using UnityEngine;
using SDG.Unturned;
using HighlightingSystem;
using System.Collections.Generic;

public class ChamsBehaviour : MonoBehaviour
{
    // Separate toggles
    public static bool PlayerChamsEnabled;
    public static bool ZombieChamsEnabled;
    public static bool SelfChamsEnabled;
    public static bool OutlineEnabled;
    public static int Pattern;

    // Chams colors
    public static float VisR = 0.2f, VisG = 0.6f, VisB = 1f, VisA = 0.8f;
    public static float NonVisR = 1f, NonVisG = 0.2f, NonVisB = 0.2f, NonVisA = 0.5f;
    public static float WireVisR = 1f, WireVisG = 1f, WireVisB = 1f, WireVisA = 1f;
    public static float WireNonVisR = 1f, WireNonVisG = 1f, WireNonVisB = 1f, WireNonVisA = 0.7f;

    // Self chams color
    public static float SelfR = 0f, SelfG = 1f, SelfB = 0f, SelfA = 0.5f;

    // Outline color
    public static float OutR = 1f, OutG = 0f, OutB = 1f, OutA = 1f;

    public static bool Bootstrapped;
    public static string BundlePath;

    // Materials
    static Material _chamsMat, _wireMat, _wfFillMat, _selfMat;
    static bool _matsCreated, _triedBundle;

    // Chams restore
    struct SavedEntry { public Renderer rend; public Material[] orig; }
    static List<SavedEntry> _saved = new List<SavedEntry>();

    // Outline tracking
    static List<Highlighter> _outlineAdded = new List<Highlighter>();
    static bool _wasActive;

    void LateUpdate()
    {
        bool active = PlayerChamsEnabled || ZombieChamsEnabled || SelfChamsEnabled || OutlineEnabled;
        if (!active) {
            if (_wasActive) { RestoreAll(); RemoveOutlines(); }
            _wasActive = false;
            return;
        }
        _wasActive = true;

        if (!_matsCreated) CreateMaterials();
        if (!_matsCreated && !OutlineEnabled) return;

        RestoreAll();
        RemoveOutlines();
        UpdateColors();

        // Pick chams material
        Material chamsMat = _chamsMat;
        if (Pattern == 4 && _wireMat != null) chamsMat = _wireMat;
        if (Pattern == 5 && _wfFillMat != null) chamsMat = _wfFillMat;

        Color outColor = new Color(OutR, OutG, OutB, OutA);

        // Players
        try {
            if (Provider.clients != null) {
                foreach (SteamPlayer sp in Provider.clients) {
                    if (sp == null || sp.player == null) continue;
                    if (sp.player == Player.player) continue;
                    if (sp.player.gameObject == null) continue;

                    if (PlayerChamsEnabled && chamsMat != null)
                        ApplyChams(sp.player.gameObject, true, chamsMat);

                    if (OutlineEnabled)
                        ApplyOutline(sp.player.gameObject, outColor);
                }
            }
        } catch {}

        // Zombies
        try {
            if (ZombieManager.regions != null) {
                foreach (ZombieRegion region in ZombieManager.regions) {
                    if (region == null || region.zombies == null) continue;
                    foreach (Zombie z in region.zombies) {
                        if (z == null || z.isDead) continue;
                        if (z.gameObject == null) continue;

                        if (ZombieChamsEnabled && chamsMat != null)
                            ApplyChams(z.gameObject, false, chamsMat);

                        if (OutlineEnabled)
                            ApplyOutline(z.gameObject, outColor);
                    }
                }
            }
        } catch {}

        // Self chams
        if (SelfChamsEnabled && _selfMat != null) {
            try {
                Player lp = Player.player;
                if (lp != null) {
                    Renderer[] rends = lp.GetComponentsInChildren<Renderer>(true);
                    if (rends != null) {
                        foreach (Renderer r in rends) {
                            if (r == null) continue;
                            string n = r.gameObject.name;
                            if (n != "Model_0" && n != "Model_1") continue;
                            _saved.Add(new SavedEntry { rend = r, orig = r.sharedMaterials });
                            r.material = _selfMat;
                        }
                    }
                }
            } catch {}
        }
    }

    static void ApplyChams(GameObject go, bool playerFilter, Material mat)
    {
        if (go == null || mat == null) return;
        Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends == null) return;
        foreach (Renderer r in rends) {
            if (r == null) continue;
            if (playerFilter) {
                string n = r.gameObject.name;
                if (n != "Model_0" && n != "Model_1") continue;
            }
            _saved.Add(new SavedEntry { rend = r, orig = r.sharedMaterials });
            r.material = mat;
        }
    }

    static void ApplyOutline(GameObject go, Color color)
    {
        if (go == null) return;
        try {
            Highlighter h = go.GetComponent<Highlighter>();
            if (h == null) h = go.AddComponent<Highlighter>();
            h.overlay = true;
            h.ConstantOn(color, 0f);
            _outlineAdded.Add(h);
        } catch {}
    }

    static void RemoveOutlines()
    {
        foreach (Highlighter h in _outlineAdded) {
            if (h != null) {
                try { h.ConstantOff(0f); } catch {}
            }
        }
        _outlineAdded.Clear();
    }

    static void RestoreAll()
    {
        foreach (SavedEntry e in _saved) {
            if (e.rend != null && e.orig != null)
                try { e.rend.materials = e.orig; } catch {}
        }
        _saved.Clear();
    }

    static void CreateMaterials()
    {
        if (_triedBundle) return;
        if (BundlePath == null || BundlePath.Length == 0) return;
        _triedBundle = true;
        try {
            AssetBundle bundle = AssetBundle.LoadFromFile(BundlePath);
            if (bundle == null) return;
            Shader[] allShaders = bundle.LoadAllAssets<Shader>();
            if (allShaders != null) {
                for (int i = 0; i < allShaders.Length; i++) {
                    Shader s = allShaders[i];
                    if (s == null || s.name == null) continue;
                    if (s.name == "Custom/Chams") {
                        _chamsMat = new Material(s);
                        _chamsMat.hideFlags = HideFlags.HideAndDontSave;
                        _selfMat = new Material(s);
                        _selfMat.hideFlags = HideFlags.HideAndDontSave;
                    } else if (s.name == "Custom/WireframeFill") {
                        _wfFillMat = new Material(s);
                        _wfFillMat.hideFlags = HideFlags.HideAndDontSave;
                        _wireMat = new Material(s);
                        _wireMat.hideFlags = HideFlags.HideAndDontSave;
                    }
                }
            }
            bundle.Unload(false);
            _matsCreated = (_chamsMat != null);
        } catch {}
    }

    static void UpdateColors()
    {
        Color vis = new Color(VisR, VisG, VisB, VisA);
        Color nonvis = new Color(NonVisR, NonVisG, NonVisB, NonVisA);
        if (_chamsMat != null) {
            _chamsMat.SetColor("_Color", vis);
            _chamsMat.SetColor("_ColorBehind", nonvis);
            _chamsMat.SetInt("_Pattern", Pattern);
            _chamsMat.SetFloat("_Emission", 0.6f);
        }
        if (_selfMat != null) {
            Color sc = new Color(SelfR, SelfG, SelfB, SelfA);
            _selfMat.SetColor("_Color", sc);
            _selfMat.SetColor("_ColorBehind", sc);
            _selfMat.SetInt("_Pattern", 1); // flat for self
            _selfMat.SetFloat("_Emission", 0.4f);
        }
        if (_wireMat != null) {
            _wireMat.SetColor("_Color", new Color(0, 0, 0, 0));
            _wireMat.SetColor("_ColorBehind", new Color(0, 0, 0, 0));
            _wireMat.SetColor("_WireColor", vis);
            _wireMat.SetColor("_WireColorBehind", nonvis);
        }
        if (_wfFillMat != null) {
            _wfFillMat.SetColor("_Color", vis);
            _wfFillMat.SetColor("_ColorBehind", nonvis);
            _wfFillMat.SetColor("_WireColor", new Color(WireVisR, WireVisG, WireVisB, WireVisA));
            _wfFillMat.SetColor("_WireColorBehind", new Color(WireNonVisR, WireNonVisG, WireNonVisB, WireNonVisA));
        }
    }

    public static void Bootstrap()
    {
        GameObject go = new GameObject("__chams_helper__");
        go.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(go);
        go.AddComponent<ChamsBehaviour>();
        Bootstrapped = true;
    }
}
