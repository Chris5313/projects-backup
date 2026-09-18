#include "gui.h"
#include "style.h"
#include "sound.h"
#include "game.h"
#include "inject.h"
#include "discord_log.h"

#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

#include <cstdio>
#include <cmath>
#include <cstring>
#include <string>
#include <ctime>
#include <cstdlib>
#include <thread>
#include <atomic>


#include <windows.h>
#include <shlobj.h>

// Font Awesome 6 icon codepoints
#define ICON_FA_LOCK       "\xef\x80\xa3"
#define ICON_FA_EYE        "\xef\x81\xae"
#define ICON_FA_EYE_SLASH  "\xef\x81\xb0"
#define ICON_FA_XMARK      "\xef\x80\x8d"
#define ICON_FA_PLAY       "\xef\x81\x8b"
#define ICON_FA_USERS      "\xef\x83\x80"
#define ICON_FA_GAMEPAD    "\xef\x84\x9b"
#define ICON_FA_GEAR       "\xef\x80\x93"
#define ICON_FA_CALENDAR   "\xef\x84\xb3"
#define ICON_FA_CROSSHAIRS "\xef\x81\x9b"
#define ICON_FA_ARROW_LEFT "\xef\x81\xa0"
#define ICON_FA_SHIELD     "\xef\x84\xb2"
#define ICON_FA_GLOBE      "\xef\x82\xac"
#define ICON_FA_ROCKET     "\xef\x84\xb5"
#define ICON_FA_CHECK      "\xef\x80\x8c"
#define ICON_FA_TERMINAL   "\xef\x84\xa0"

namespace GUI {
    ImFont* FontText  = nullptr;
    ImFont* FontTitle = nullptr;
    ImFont* FontSmall = nullptr;
    ImFont* FontIcon  = nullptr;

    ID3D11ShaderResourceView* GameTexture = nullptr;
    int GameWidth  = 0;
    int GameHeight = 0;

    ID3D11ShaderResourceView* LogoTexture = nullptr;
    int LogoWidth  = 0;
    int LogoHeight = 0;

    ID3D11ShaderResourceView* BannerTexture = nullptr;
    int BannerWidth  = 0;
    int BannerHeight = 0;

    ID3D11ShaderResourceView* FrontTexture = nullptr;
    int FrontWidth  = 0;
    int FrontHeight = 0;

    ID3D11ShaderResourceView* Gw2Texture = nullptr;
    int Gw2Width  = 0;
    int Gw2Height = 0;
    ID3D11ShaderResourceView* RbxTexture = nullptr;
    int RbxWidth  = 0;
    int RbxHeight = 0;

    bool WantsClose = false;
    bool LoggedIn   = false;  // Show login screen
    bool NotAdmin   = false;
    bool HoveringInteractive = false;
    bool GhostMode = false;
    // ── Server auth state (login button) ──
    static std::thread s_AuthThread;
    static std::atomic<bool> s_AuthThreadDone{false};
    static std::atomic<bool> s_AuthBusy{false};
    static std::atomic<bool> s_AuthOk{false};


    // Transition
    static float s_TransitionT     = 0.0f;  // Start at login screen
    static bool  s_Transitioning   = false;
    static float s_TransitionDelay = 0.0f;

    // Login state
    static char  s_KeyBuffer[256] = {};
    static bool  s_RememberKey = false;
    static bool  s_ShowKey     = false;
    static float s_AnimTime    = 0.0f;
    static float s_BtnHoverAnim  = 0.0f;
    static float s_BtnClickAnim  = 0.0f;
    static char  s_AuthMsg[128]  = {};

    // Dashboard state
    static float s_PlayBtnHover   = 0.0f;
    static float s_GearHover      = 0.0f;
    static bool  s_DetailOpen     = false;
    static float s_DetailAnim     = 0.0f;
    static float s_BackBtnHover   = 0.0f;
    static float s_LaunchBtnHover = 0.0f;

    static bool  s_AutoStartGame  = true;
    static int   s_SelectedGame   = 0;   // 0 = Unturned, 1 = PvZ GW2
    static bool  s_SettingsOpen   = false;
    static float s_SettingsAnim   = 0.0f;
    static bool  s_AlwaysOnTop    = true;
    static bool  s_GhostMode      = false;
    // Debug log window state
    static bool  s_LogOpen         = false;
    static float s_LogRefreshTimer = 0.0f;
    static char  s_LogBuf[32768]   = {};
    static bool  s_LogCopied       = false;
    static float s_LogCopiedTimer  = 0.0f;
    static bool  s_NeedApplyTopmost = false;

    // Launch animation state
    static bool  s_Launching      = false;
    static float s_LaunchTime     = 0.0f;
    static int   s_LaunchPhase    = 0;
    static bool  s_LaunchSuccess  = false;
    static char  s_LaunchMsg[256] = {};
    static float s_CloseTimer     = 0.0f;

    // ── Settings persistence ──
    static std::string GetAppDataDir() {
        char buf[MAX_PATH];
        if (SUCCEEDED(SHGetFolderPathA(nullptr, CSIDL_APPDATA, nullptr, 0, buf))) {
            std::string dir = std::string(buf) + "\\hamasclient";
            CreateDirectoryA(dir.c_str(), nullptr);
            return dir;
        }
        return ".";
    }

    static void SaveSettings();

    static void LoadSettings() {
        std::string path = GetAppDataDir() + "\\autologin.cfg";
        FILE* f = fopen(path.c_str(), "r");
        if (!f) return;

        char line[512];
        while (fgets(line, sizeof(line), f)) {
            for (int i = 0; line[i]; i++)
                if (line[i] == '\n' || line[i] == '\r') { line[i] = 0; break; }
            if (!line[0]) continue;

            char* eq = strchr(line, '=');
            if (!eq) continue;
            *eq = 0;
            const char* key = line;
            const char* val = eq + 1;

            if (strcmp(key, "key") == 0) {
                if (val[0]) {
                    strncpy(s_KeyBuffer, val, sizeof(s_KeyBuffer) - 1);
                    s_RememberKey = true;
                }
            } else if (strcmp(key, "topmost") == 0) {
                s_AlwaysOnTop = atoi(val) != 0;
            } else if (strcmp(key, "autostart") == 0) {
                s_AutoStartGame = atoi(val) != 0;
            } else if (strcmp(key, "ghost") == 0) {
                s_GhostMode = atoi(val) != 0;
            }
        }
        fclose(f);
    }

    static void SaveSettings() {
        std::string path = GetAppDataDir() + "\\autologin.cfg";
        FILE* f = fopen(path.c_str(), "w");
        if (!f) return;
        if (s_RememberKey && s_KeyBuffer[0])
            fprintf(f, "key=%s\n", s_KeyBuffer);
        fprintf(f, "topmost=%d\n", s_AlwaysOnTop ? 1 : 0);
        fprintf(f, "autostart=%d\n", s_AutoStartGame ? 1 : 0);
        fprintf(f, "ghost=%d\n", s_GhostMode ? 1 : 0);
        fclose(f);
    }

    // ── Texture loading from memory ──
    static bool LoadTextureFromMemory(const unsigned char* buf, int bufLen,
                                      ID3D11Device* device,
                                      ID3D11ShaderResourceView** outSRV, int* outW, int* outH)
    {
        int w, h, channels;
        unsigned char* data = stbi_load_from_memory(buf, bufLen, &w, &h, &channels, 4);
        if (!data) return false;

        D3D11_TEXTURE2D_DESC desc = {};
        desc.Width            = w;
        desc.Height           = h;
        desc.MipLevels        = 1;
        desc.ArraySize        = 1;
        desc.Format           = DXGI_FORMAT_R8G8B8A8_UNORM;
        desc.SampleDesc.Count = 1;
        desc.Usage            = D3D11_USAGE_DEFAULT;
        desc.BindFlags        = D3D11_BIND_SHADER_RESOURCE;

        D3D11_SUBRESOURCE_DATA subRes = {};
        subRes.pSysMem     = data;
        subRes.SysMemPitch = w * 4;

        ID3D11Texture2D* tex = nullptr;
        HRESULT hr = device->CreateTexture2D(&desc, &subRes, &tex);
        stbi_image_free(data);
        if (FAILED(hr)) return false;

        hr = device->CreateShaderResourceView(tex, nullptr, outSRV);
        tex->Release();
        if (FAILED(hr)) return false;

        *outW = w;
        *outH = h;
        return true;
    }

    static void LoadResourceTexture(int resId, ID3D11Device* device,
                                     ID3D11ShaderResourceView** outSRV, int* outW, int* outH) {
        HRSRC hr = FindResource(nullptr, MAKEINTRESOURCE(resId), RT_RCDATA);
        if (!hr) return;
        HGLOBAL hg = LoadResource(nullptr, hr);
        if (!hg) return;
        DWORD sz = SizeofResource(nullptr, hr);
        const unsigned char* p = (const unsigned char*)LockResource(hg);
        if (p) LoadTextureFromMemory(p, (int)sz, device, outSRV, outW, outH);
    }

    void Init(ID3D11Device* device) {
        LoadResourceTexture(203, device, &GameTexture, &GameWidth, &GameHeight);
        LoadResourceTexture(204, device, &LogoTexture, &LogoWidth, &LogoHeight);
        LoadResourceTexture(205, device, &BannerTexture, &BannerWidth, &BannerHeight);
        LoadResourceTexture(206, device, &FrontTexture, &FrontWidth, &FrontHeight);
        LoadResourceTexture(212, device, &Gw2Texture, &Gw2Width, &Gw2Height);
        LoadResourceTexture(213, device, &RbxTexture, &RbxWidth, &RbxHeight);
        LoadSettings();
        GhostMode = s_GhostMode;
        if (!s_AlwaysOnTop) s_NeedApplyTopmost = true;
    }

    void Shutdown() {
        // Join the in-flight auth worker first — destroying a joinable
        // std::thread calls std::terminate() and crashes on exit.
        if (s_AuthThread.joinable())
            s_AuthThread.join();

        if (GameTexture)   { GameTexture->Release();   GameTexture = nullptr; }
        if (LogoTexture)   { LogoTexture->Release();   LogoTexture = nullptr; }
        if (RbxTexture)    { RbxTexture->Release();    RbxTexture = nullptr; }
        if (FrontTexture)  { FrontTexture->Release();  FrontTexture = nullptr; }
        if (Gw2Texture)    { Gw2Texture->Release();    Gw2Texture = nullptr; }
    }

    // ── Background with blue corner glows ──
    static void DrawBackground(ImDrawList* dl, float w, float h) {
        dl->AddRectFilled(ImVec2(0, 0), ImVec2(w, h), Style::COL_BG);
        float t = s_AnimTime;
        float pulse = 0.8f + 0.2f * sinf(t * 0.4f);
        int glow = (int)(45.0f * pulse);
        ImU32 clear = IM_COL32(0, 0, 0, 0);

        // Top-right glow
        dl->AddRectFilledMultiColor(
            ImVec2(w * 0.35f, 0), ImVec2(w, h * 0.55f),
            clear, IM_COL32(0, 100, 40, glow),
            IM_COL32(0, 70, 25, glow / 3), clear);
        dl->AddRectFilledMultiColor(
            ImVec2(w * 0.6f, 0), ImVec2(w, h * 0.3f),
            clear, IM_COL32(0, 120, 50, glow * 2 / 3),
            IM_COL32(0, 80, 30, glow / 4), clear);

        // Bottom-left glow
        dl->AddRectFilledMultiColor(
            ImVec2(0, h * 0.45f), ImVec2(w * 0.65f, h),
            clear, clear,
            IM_COL32(0, 70, 25, glow / 3), IM_COL32(0, 100, 40, glow));
        dl->AddRectFilledMultiColor(
            ImVec2(0, h * 0.7f), ImVec2(w * 0.4f, h),
            clear, clear,
            IM_COL32(0, 80, 30, glow / 4), IM_COL32(0, 120, 50, glow * 2 / 3));
    }

    // ── Card background ──
    static void DrawCard(ImDrawList* dl, ImVec2 pos, ImVec2 size) {
        ImVec2 shadowOff(4, 4);
        dl->AddRectFilled(
            ImVec2(pos.x + shadowOff.x, pos.y + shadowOff.y),
            ImVec2(pos.x + size.x + shadowOff.x, pos.y + size.y + shadowOff.y),
            IM_COL32(0, 0, 0, 80), 12.0f);
        dl->AddRectFilled(pos, ImVec2(pos.x + size.x, pos.y + size.y), Style::COL_CARD, 12.0f);
        dl->AddRect(pos, ImVec2(pos.x + size.x, pos.y + size.y),
            IM_COL32(40, 40, 48, 120), 12.0f, 0, 1.0f);
    }

    // ══════════════════════════════════════════════════════════════
    //  RenderLogin — LEFT-ALIGNED card + right panel
    // ══════════════════════════════════════════════════════════════
    void RenderLogin(float width, float height, float offsetX) {
        s_AnimTime += ImGui::GetIO().DeltaTime;
        static bool s_anyHover = false;
        s_anyHover = false;
        ImDrawList* bgDL = ImGui::GetBackgroundDrawList();
        if (offsetX == 0.0f) DrawBackground(bgDL, width, height);

        const float cardW = 380.0f;
        const float cardH = 480.0f;
        const float cardX = 30.0f + offsetX;
        const float cardY = (height - cardH) * 0.5f;

        DrawCard(bgDL, ImVec2(cardX, cardY), ImVec2(cardW, cardH));

        // ── Close button (top-right of WINDOW) ──
        {
            const float btnSize = 30.0f;
            const float margin  = 8.0f;
            ImGui::SetNextWindowPos(ImVec2(width - btnSize - margin + offsetX, margin));
            ImGui::SetNextWindowSize(ImVec2(btnSize, btnSize));
            ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(0, 0));
            ImGui::PushStyleVar(ImGuiStyleVar_ItemSpacing, ImVec2(0, 0));
            ImGui::Begin("##close", nullptr,
                ImGuiWindowFlags_NoDecoration | ImGuiWindowFlags_NoMove |
                ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoBackground |
                ImGuiWindowFlags_AlwaysAutoResize);
            ImVec2 iconSz = ImGui::CalcTextSize(ICON_FA_XMARK);
            float padX = (btnSize - iconSz.x) * 0.5f;
            float padY = (btnSize - iconSz.y) * 0.5f;
            ImGui::PushStyleVar(ImGuiStyleVar_FramePadding, ImVec2(padX, padY));
            ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 6.0f);
            ImGui::PushStyleColor(ImGuiCol_Button, ImVec4(0, 0, 0, 0));
            ImGui::PushStyleColor(ImGuiCol_ButtonHovered, ImVec4(0.86f, 0.2f, 0.2f, 0.8f));
            ImGui::PushStyleColor(ImGuiCol_ButtonActive, ImVec4(0.7f, 0.1f, 0.1f, 1.0f));
            ImGui::PushStyleColor(ImGuiCol_Text, ImVec4(0.6f, 0.6f, 0.6f, 1.0f));
            if (ImGui::Button(ICON_FA_XMARK)) { Sound::PlayClick(); WantsClose = true; }
            if (ImGui::IsItemHovered()) s_anyHover = true;
            ImGui::PopStyleColor(4);
            ImGui::PopStyleVar(2);
            ImGui::End();
            ImGui::PopStyleVar(2);
        }

        // ── Card content (ImGui window) ──
        ImGui::SetNextWindowPos(ImVec2(cardX, cardY));
        ImGui::SetNextWindowSize(ImVec2(cardW, cardH));
        ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(32.0f, 28.0f));
        ImGui::Begin("##login", nullptr,
            ImGuiWindowFlags_NoDecoration | ImGuiWindowFlags_NoMove |
            ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoScrollWithMouse |
            ImGuiWindowFlags_NoBackground);

        float contentW = cardW - 64.0f;

        // Logo
        if (LogoTexture) {
            float logoDispH = 64.0f;
            float aspect = (float)LogoWidth / (float)LogoHeight;
            float logoDispW = logoDispH * aspect;
            ImGui::SetCursorPosX(ImGui::GetCursorPosX() + (contentW - logoDispW) * 0.5f);
            ImGui::Image((ImTextureID)LogoTexture, ImVec2(logoDispW, logoDispH));
        } else {
            ImGui::Dummy(ImVec2(0, 64));
        }
        ImGui::Dummy(ImVec2(0, 10.0f));

        // Title
        if (FontTitle) ImGui::PushFont(FontTitle);
        {
            const char* t = "Hamas Client";
            ImVec2 ts = ImGui::CalcTextSize(t);
            ImGui::SetCursorPosX(ImGui::GetCursorPosX() + (contentW - ts.x) * 0.5f);
            ImGui::TextUnformatted(t);
        }
        if (FontTitle) ImGui::PopFont();

        // Slogan — "Min al-nahr ila al-bahr" (From the river to the sea) in Arabic
        // Pre-shaped presentation forms in visual LTR order for ImGui rendering
        if (FontSmall) ImGui::PushFont(FontSmall);
        {
            const char* s = "\xef\xba\xae\xef\xba\xa4\xef\xba\x92\xef\xbb\x9f\xef\xba\x8d"
                            " \xef\xbb\xb0\xef\xbb\x9f\xef\xba\x87"
                            " \xef\xba\xae\xef\xbb\xac\xef\xbb\xa8\xef\xbb\x9f\xef\xba\x8d"
                            " \xef\xbb\xa6\xef\xbb\xa3";
            ImVec2 ss = ImGui::CalcTextSize(s);
            ImGui::SetCursorPosX(ImGui::GetCursorPosX() + (contentW - ss.x) * 0.5f);
            ImGui::PushStyleColor(ImGuiCol_Text, Style::ToVec4(Style::COL_TEXT_SUB));
            ImGui::TextUnformatted(s);
            ImGui::PopStyleColor();
        }
        if (FontSmall) ImGui::PopFont();

        ImGui::Dummy(ImVec2(0, 20.0f));

        // License Key label
        if (FontSmall) ImGui::PushFont(FontSmall);
        ImGui::PushStyleColor(ImGuiCol_Text, Style::ToVec4(Style::COL_TEXT_SUB));
        ImGui::TextUnformatted("License Key");
        ImGui::PopStyleColor();
        if (FontSmall) ImGui::PopFont();
        ImGui::Dummy(ImVec2(0, 4.0f));

        // Key input
        float inputH = 40.0f;
        ImVec2 inputPos = ImGui::GetCursorScreenPos();
        ImGui::PushStyleVar(ImGuiStyleVar_FramePadding, ImVec2(36.0f, 11.0f));
        ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 8.0f);
        ImGui::PushStyleVar(ImGuiStyleVar_FrameBorderSize, 1.0f);
        ImGui::PushStyleColor(ImGuiCol_Border, ImVec4(0.15f, 0.15f, 0.18f, 1.0f));
        ImGui::PushItemWidth(contentW);
        ImGuiInputTextFlags flags = ImGuiInputTextFlags_None;
        if (!s_ShowKey) flags |= ImGuiInputTextFlags_Password;
        if (ImGui::InputTextWithHint("##key", "Enter license key", s_KeyBuffer, sizeof(s_KeyBuffer), flags)) {
            if (s_RememberKey) SaveSettings();
        }
        ImGui::PopItemWidth();
        ImGui::PopStyleColor();
        ImGui::PopStyleVar(3);

        ImDrawList* dl = ImGui::GetWindowDrawList();
        dl->AddText(FontText, 14.0f, ImVec2(inputPos.x + 12.0f, inputPos.y + (inputH - 14.0f) * 0.5f),
            Style::COL_TEXT_DIM, ICON_FA_LOCK);

        // Eye toggle
        const char* eyeIcon = s_ShowKey ? ICON_FA_EYE : ICON_FA_EYE_SLASH;
        ImVec2 eyeSz = ImGui::CalcTextSize(eyeIcon);
        float eyeX = inputPos.x + contentW - eyeSz.x - 12.0f;
        float eyeY = inputPos.y + (inputH - eyeSz.y) * 0.5f;
        ImVec2 eyeMin(eyeX - 6.0f, inputPos.y);
        ImVec2 eyeMax(eyeX + eyeSz.x + 6.0f, inputPos.y + inputH);
        ImVec2 mouse = ImGui::GetMousePos();
        bool eyeHover = mouse.x >= eyeMin.x && mouse.x <= eyeMax.x &&
                        mouse.y >= eyeMin.y && mouse.y <= eyeMax.y;
        if (eyeHover && ImGui::GetIO().MouseClicked[0]) s_ShowKey = !s_ShowKey;
        if (eyeHover) HoveringInteractive = true;
        ImU32 eyeCol = eyeHover ? Style::COL_TEXT : Style::COL_TEXT_DIM;
        dl->AddText(FontText, 14.0f, ImVec2(eyeX, eyeY), eyeCol, eyeIcon);

        ImGui::Dummy(ImVec2(0, 8.0f));

        // Remember Key checkbox (custom drawn)
        {
            float boxSz = 20.0f;
            float gap = 10.0f;
            ImVec2 pos = ImGui::GetCursorScreenPos();
            ImVec2 boxMin = pos;
            ImVec2 boxMax(pos.x + boxSz, pos.y + boxSz);
            const char* cbLabel = "Remember Key";
            ImVec2 labelSz = ImGui::CalcTextSize(cbLabel);
            ImGui::InvisibleButton("##remkey", ImVec2(boxSz + gap + labelSz.x, boxSz));
            bool cbHover = ImGui::IsItemHovered();
            if (ImGui::IsItemClicked()) {
                s_RememberKey = !s_RememberKey;
                Sound::PlayClick();
                SaveSettings();
            }
            if (cbHover) s_anyHover = true;
            ImU32 bgCol = cbHover ? IM_COL32(25, 25, 30, 255) : IM_COL32(15, 15, 18, 255);
            dl->AddRectFilled(boxMin, boxMax, bgCol, 4.0f);
            dl->AddRect(boxMin, boxMax, IM_COL32(50, 50, 60, 200), 4.0f, 0, 1.0f);
            if (s_RememberKey) {
                float pad = 4.0f;
                float x = boxMin.x + pad, y = boxMin.y + pad, sz = boxSz - pad * 2;
                ImVec2 p1(x, y + sz * 0.5f);
                ImVec2 p2(x + sz * 0.35f, y + sz * 0.85f);
                ImVec2 p3(x + sz, y + sz * 0.1f);
                ImU32 tickCol = IM_COL32(0, 120, 220, 255);
                dl->AddLine(p1, p2, tickCol, 2.5f);
                dl->AddLine(p2, p3, tickCol, 2.5f);
            }
            float textY = pos.y + (boxSz - labelSz.y) * 0.5f;
            dl->AddText(ImVec2(pos.x + boxSz + gap, textY), IM_COL32(220, 220, 225, 255), cbLabel);
        }

        ImGui::Dummy(ImVec2(0, 14.0f));

        // Activate button (custom drawn, premium feel)
        {
            float dt = ImGui::GetIO().DeltaTime;
            float btnH = 44.0f;
            float btnRound = 10.0f;
            ImVec2 btnPos = ImGui::GetCursorScreenPos();
            ImVec2 btnMin = btnPos;
            ImVec2 btnMax(btnPos.x + contentW, btnPos.y + btnH);
            ImGui::InvisibleButton("##activate", ImVec2(contentW, btnH));
            bool hovered = ImGui::IsItemHovered();
            bool clicked = ImGui::IsItemClicked();
            if (hovered) s_anyHover = true;

            float hoverTarget = hovered ? 1.0f : 0.0f;
            s_BtnHoverAnim += (hoverTarget - s_BtnHoverAnim) * dt * 10.0f;
            if (s_BtnHoverAnim < 0.005f) s_BtnHoverAnim = 0.0f;
            if (s_BtnHoverAnim > 0.995f) s_BtnHoverAnim = 1.0f;
            float h = s_BtnHoverAnim;

            if (clicked && !s_AuthBusy) {
                s_BtnClickAnim = 1.0f;
                Sound::PlayActivate();
                if (s_KeyBuffer[0] == '\0') {
                    snprintf(s_AuthMsg, sizeof(s_AuthMsg), "Enter a license key");
                } else {
                    // Server-side auth on a worker thread — never block the UI
                    s_AuthBusy = true;
                    s_AuthThreadDone = false;
                    s_AuthOk = false;
                    snprintf(s_AuthMsg, sizeof(s_AuthMsg), "Verifying...");
                    char keyCopy[256];
                    strncpy(keyCopy, s_KeyBuffer, sizeof(keyCopy) - 1);
                    keyCopy[sizeof(keyCopy) - 1] = '\0';
                    if (s_AuthThread.joinable()) s_AuthThread.join();
                    s_AuthThread = std::thread([keyCopy]() {
                        s_AuthOk = DiscordLog::Authenticate(keyCopy);
                        s_AuthThreadDone = true;
                    });
                }
            }

            // Poll the auth worker for completion
            if (s_AuthBusy && s_AuthThreadDone) {
                s_AuthBusy = false;
                if (s_AuthOk) {
                    snprintf(s_AuthMsg, sizeof(s_AuthMsg), "Access granted!");
                    LoggedIn = true;
                    SaveSettings();
                    Sound::PlayHamasSong();
                    DiscordLog::PostEvent(DiscordLog::EVENT_LOGIN_SUCCESS);
                } else {
                    const char* why = DiscordLog::GetAuthError();
                    if (!why || !why[0]) why = "Invalid license key.";
                    snprintf(s_AuthMsg, sizeof(s_AuthMsg), "%s", why);
                    DiscordLog::PostEvent(DiscordLog::EVENT_LOGIN_FAIL, why);
                }
            }
            s_BtnClickAnim *= 0.92f;
            if (s_BtnClickAnim < 0.01f) s_BtnClickAnim = 0.0f;

            // Outer glow
            if (h > 0.01f) {
                for (int g = 3; g >= 1; g--) {
                    float expand = (float)g * 3.0f * h;
                    int alpha = (int)(18.0f * h * (1.0f - g * 0.25f));
                    dl->AddRectFilled(ImVec2(btnMin.x - expand, btnMin.y - expand),
                        ImVec2(btnMax.x + expand, btnMax.y + expand),
                        IM_COL32(0, 100, 210, alpha), btnRound + expand * 0.5f);
                }
            }
            // Gradient body
            int tg = (int)(91 + 30 * h), tb = (int)(187 + 40 * h);
            int bg2 = (int)(68 + 20 * h), bb = (int)(153 + 30 * h);
            ImU32 topCol = IM_COL32(0, tg, tb, 255);
            ImU32 botCol = IM_COL32(0, bg2, bb, 255);
            dl->AddRectFilledMultiColor(btnMin, btnMax, topCol, topCol, botCol, botCol);
            dl->AddRect(btnMin, btnMax, IM_COL32(0, 120, 220, (int)(40 + 60 * h)), btnRound, 0, 1.5f);
            if (s_BtnClickAnim > 0.01f)
                dl->AddRectFilled(btnMin, btnMax, IM_COL32(255, 255, 255, (int)(80 * s_BtnClickAnim)), btnRound);
            if (h > 0.01f) {
                float shineW = contentW * 0.6f;
                float shineX = btnMin.x + (contentW - shineW) * 0.5f;
                dl->AddLine(ImVec2(shineX, btnMin.y + 1), ImVec2(shineX + shineW, btnMin.y + 1),
                    IM_COL32(255, 255, 255, (int)(50 * h)), 1.0f);
            }
            const char* label = "Login";
            ImVec2 textSz = ImGui::CalcTextSize(label);
            float textX = btnMin.x + (contentW - textSz.x) * 0.5f;
            float textY = btnMin.y + (btnH - textSz.y) * 0.5f - h * 1.5f;
            dl->AddText(ImVec2(textX, textY), IM_COL32(255, 255, 255, 255), label);
        }

        // Auth status message
        if (s_AuthMsg[0] != '\0') {
            ImGui::Dummy(ImVec2(0, 6.0f));
            if (FontSmall) ImGui::PushFont(FontSmall);
            ImVec4 msgCol = LoggedIn ? ImVec4(0, 0.78f, 0.31f, 1) : ImVec4(0.86f, 0.24f, 0.24f, 1);
            ImGui::PushStyleColor(ImGuiCol_Text, msgCol);
            ImGui::PushTextWrapPos(ImGui::GetCursorPosX() + contentW);
            ImVec2 msgSz = ImGui::CalcTextSize(s_AuthMsg, nullptr, false, contentW);
            if (msgSz.x < contentW) ImGui::SetCursorPosX(ImGui::GetCursorPosX() + (contentW - msgSz.x) * 0.5f);
            ImGui::TextWrapped("%s", s_AuthMsg);
            ImGui::PopTextWrapPos();
            ImGui::PopStyleColor();
            if (FontSmall) ImGui::PopFont();
        }

        ImGui::Dummy(ImVec2(0, 10.0f));

        // Footer
        if (FontSmall) ImGui::PushFont(FontSmall);
        ImGui::PushStyleColor(ImGuiCol_Text, Style::ToVec4(Style::COL_TEXT_DIM));
        ImGui::TextUnformatted("v1.0 | Free Edition");
        ImGui::PopStyleColor();
        if (FontSmall) ImGui::PopFont();

        ImGui::End();
        ImGui::PopStyleVar(); // WindowPadding

        // ── RIGHT PANEL — Hamas Front Image ──
        {
            float margin = 30.0f;
            float cardEnd = cardX + cardW;
            float panelX = cardEnd + 30.0f;
            float panelTop = margin + 30.0f;
            float panelR = width - margin + offsetX;
            float panelW = panelR - panelX;
            float imgH = height - margin - panelTop - 20.0f;

            ImDrawList* fgDL = ImGui::GetForegroundDrawList();

            ID3D11ShaderResourceView* img = FrontTexture ? FrontTexture : GameTexture;
            int imgW_src = FrontTexture ? FrontWidth : GameWidth;
            int imgH_src = FrontTexture ? FrontHeight : GameHeight;

            if (img) {
                float srcAspect = (float)imgW_src / (float)imgH_src;
                float dstAspect = panelW / imgH;
                float u0 = 0, v0 = 0, u1 = 1, v1 = 1;
                if (srcAspect > dstAspect) {
                    float visibleFrac = dstAspect / srcAspect;
                    u0 = (1.0f - visibleFrac) * 0.5f; u1 = u0 + visibleFrac;
                } else {
                    float visibleFrac = srcAspect / dstAspect;
                    v0 = (1.0f - visibleFrac) * 0.5f; v1 = v0 + visibleFrac;
                }
                fgDL->AddImageRounded((ImTextureID)img,
                    ImVec2(panelX, panelTop), ImVec2(panelR, panelTop + imgH),
                    ImVec2(u0, v0), ImVec2(u1, v1), IM_COL32(255, 255, 255, 255), 12.0f);
            }
        }

        Sound::UpdateHover(s_anyHover);
    }


    // ══════════════════════════════════════════════════════════════
    //  RenderDashboard
    // ══════════════════════════════════════════════════════════════
    void RenderDashboard(float width, float height, float offsetX) {
        s_AnimTime += ImGui::GetIO().DeltaTime;
        float dt = ImGui::GetIO().DeltaTime;
        ImDrawList* dl = ImGui::GetBackgroundDrawList();
        if (offsetX == 0.0f) DrawBackground(dl, width, height);

        const float margin = 30.0f + offsetX;

        // ═══ Top Bar ═══
        {
            float barX = margin; float barY = 15.0f;
            float barW = width - margin * 2 + offsetX;
            float barH = 40.0f;
            float settingsBoxW = 36.0f;
            float mainBarW = barW - settingsBoxW * 2 - 20.0f;

            dl->AddRectFilled(ImVec2(barX, barY), ImVec2(barX + mainBarW, barY + barH),
                IM_COL32(18, 18, 24, 200), 8.0f);
            float logX = barX + mainBarW + 10.0f;
            dl->AddRectFilled(ImVec2(logX, barY), ImVec2(logX + settingsBoxW, barY + barH),
                IM_COL32(18, 18, 24, 200), 8.0f);
            float setX = logX + settingsBoxW + 10.0f;
            dl->AddRectFilled(ImVec2(setX, barY), ImVec2(setX + settingsBoxW, barY + barH),
                IM_COL32(18, 18, 24, 200), 8.0f);

            // Logo icon + title text in bar
            float textStartX = barX + 12.0f;
            if (LogoTexture) {
                float iconH = 24.0f;
                float iconW = iconH * ((float)LogoWidth / (float)LogoHeight);
                dl->AddImage((ImTextureID)LogoTexture,
                    ImVec2(barX + 8.0f, barY + (barH - iconH) * 0.5f),
                    ImVec2(barX + 8.0f + iconW, barY + (barH + iconH) * 0.5f));
                textStartX = barX + 12.0f + iconW + 4.0f;
            }
            if (FontSmall)
                dl->AddText(FontSmall, 13.0f,
                    ImVec2(textStartX, barY + 13.0f),
                    IM_COL32(210, 210, 220, 255), "Hamas Client v1.0");

            // Date
            char dateBuf[32];
            { time_t now = time(nullptr); struct tm t; localtime_s(&t, &now);
              snprintf(dateBuf, sizeof(dateBuf), "%02d.%02d.%04d", t.tm_mday, t.tm_mon+1, t.tm_year+1900); }
            if (FontSmall) {
                char dateStr[48];
                snprintf(dateStr, sizeof(dateStr), ICON_FA_CALENDAR "  %s", dateBuf);
                ImVec2 dateSize = FontSmall->CalcTextSizeA(13.0f, FLT_MAX, 0.0f, dateStr);
                dl->AddText(FontSmall, 13.0f,
                    ImVec2(barX + mainBarW - 14.0f - dateSize.x, barY + 13.0f),
                    IM_COL32(100, 105, 120, 255), dateStr);
            }

            // Terminal / debug log icon
            if (FontSmall) {
                const char* logIcon = ICON_FA_TERMINAL;
                ImVec2 logSize = FontSmall->CalcTextSizeA(14.0f, FLT_MAX, 0.0f, logIcon);
                float logIconX = logX + (settingsBoxW - logSize.x) * 0.5f;
                float logIconY = barY + (barH - logSize.y) * 0.5f;
                ImVec2 mousePos = ImGui::GetIO().MousePos;
                bool logHovered = (mousePos.x >= logX && mousePos.x <= logX + settingsBoxW &&
                                   mousePos.y >= barY && mousePos.y <= barY + barH);
                if (logHovered) HoveringInteractive = true;
                if (logHovered && ImGui::GetIO().MouseClicked[0]) {
                    Sound::PlayClick();
                    s_LogOpen = !s_LogOpen;
                }
                ImU32 logCol = s_LogOpen
                    ? Style::COL_ACCENT
                    : IM_COL32(140, 140, 155, 255);
                dl->AddText(FontSmall, 14.0f, ImVec2(logIconX, logIconY), logCol, logIcon);
            }

            // Gear icon
            if (FontSmall) {
                const char* gearIcon = ICON_FA_GEAR;
                ImVec2 gearSize = FontSmall->CalcTextSizeA(14.0f, FLT_MAX, 0.0f, gearIcon);
                float gearX = setX + (settingsBoxW - gearSize.x) * 0.5f;
                float gearY = barY + (barH - gearSize.y) * 0.5f;
                ImVec2 mousePos = ImGui::GetIO().MousePos;
                bool gearHovered = (mousePos.x >= setX && mousePos.x <= setX + settingsBoxW &&
                                    mousePos.y >= barY && mousePos.y <= barY + barH);
                if (gearHovered) HoveringInteractive = true;
                if (gearHovered && ImGui::GetIO().MouseClicked[0]) {
                    Sound::PlayClick();
                    s_SettingsOpen = !s_SettingsOpen;
                    if (s_SettingsOpen) s_DetailOpen = false;
                }
                s_GearHover += ((gearHovered ? 1.0f : 0.0f) - s_GearHover) * dt * 12.0f;
                ImU32 gearCol = s_SettingsOpen
                    ? Style::COL_ACCENT
                    : IM_COL32((int)(100+155*s_GearHover),(int)(105+150*s_GearHover),(int)(120+135*s_GearHover),255);
                dl->AddText(FontSmall, 14.0f, ImVec2(gearX, gearY), gearCol, gearIcon);
            }
        }

        // ═══ GAMES Category ═══
        float catY = 70.0f;
        if (FontSmall) {
            char survBuf[64]; snprintf(survBuf, sizeof(survBuf), ICON_FA_GAMEPAD "  GAMES");
            dl->AddText(FontSmall, 13.0f, ImVec2(margin, catY),
                IM_COL32(100, 105, 120, 255), survBuf);
        }

        // ═══ Game Card — Unturned ═══
        {
            float cardX2 = margin;
            float cardY2 = catY + 28.0f;
            float cardW2 = 280.0f;
            float cardH2 = 220.0f;
            float rounding = 10.0f;
            float imgH2 = 155.0f;
            float textPad = 12.0f;

            dl->AddRectFilled(ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + cardH2),
                IM_COL32(18, 18, 24, 220), rounding);
            dl->AddRect(ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + cardH2),
                IM_COL32(40, 40, 50, 120), rounding, 0, 1.0f);

            if (GameTexture) {
                float srcAspect = (float)GameWidth / (float)GameHeight;
                float dstAspect = cardW2 / imgH2;
                float u0 = 0, v0 = 0, u1 = 1, v1 = 1;
                if (srcAspect > dstAspect) {
                    float vis = dstAspect / srcAspect;
                    u0 = (1.0f - vis) * 0.5f; u1 = u0 + vis;
                } else {
                    float vis = srcAspect / dstAspect;
                    v0 = (1.0f - vis) * 0.5f; v1 = v0 + vis;
                }
                dl->AddImageRounded((ImTextureID)GameTexture,
                    ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + imgH2),
                    ImVec2(u0, v0), ImVec2(u1, v1),
                    IM_COL32(255, 255, 255, 255), rounding, ImDrawFlags_RoundCornersTop);
            }

            float textY2 = cardY2 + imgH2 + textPad;
            if (FontText)
                dl->AddText(FontText, 18.0f, ImVec2(cardX2 + textPad, textY2), IM_COL32(255,255,255,255), "Unturned");
            float subY = textY2 + 22.0f;
            if (FontSmall)
                dl->AddText(FontSmall, 13.0f, ImVec2(cardX2 + textPad, subY), IM_COL32(100,105,120,255), "Free cheat");

            // Play button — rounded square
            {
                float btnSz = 36.0f;
                float btnRound2 = 8.0f;
                float btnX = cardX2 + cardW2 - textPad - btnSz;
                float btnY = textY2 + 4.0f;
                ImVec2 bMin(btnX, btnY); ImVec2 bMax(btnX + btnSz, btnY + btnSz);
                ImVec2 mp = ImGui::GetIO().MousePos;
                bool btnHov = (mp.x >= bMin.x && mp.x <= bMax.x && mp.y >= bMin.y && mp.y <= bMax.y);
                if (btnHov) HoveringInteractive = true;
                s_PlayBtnHover += ((btnHov ? 1.0f : 0.0f) - s_PlayBtnHover) * dt * 12.0f;
                float bh = s_PlayBtnHover;
                if (bh > 0.01f) {
                    float exp = 3.0f * bh;
                    dl->AddRectFilled(ImVec2(bMin.x-exp,bMin.y-exp),ImVec2(bMax.x+exp,bMax.y+exp),
                        IM_COL32(0,91,187,(int)(30*bh)), btnRound2+exp);
                }
                dl->AddRectFilled(bMin, bMax, IM_COL32(0,(int)(91+30*bh),(int)(187+40*bh),255), btnRound2);
                if (FontText) {
                    const char* playIcon = ICON_FA_PLAY;
                    ImVec2 iSz = FontText->CalcTextSizeA(16.0f, FLT_MAX, 0.0f, playIcon);
                    dl->AddText(FontText, 16.0f,
                        ImVec2(btnX + (btnSz - iSz.x)*0.5f + 1, btnY + (btnSz - iSz.y)*0.5f),
                        IM_COL32(255,255,255,255), playIcon);
                }
                if (btnHov && ImGui::GetIO().MouseClicked[0] && !s_SettingsOpen) {
                    Sound::PlayClick();
                    s_SelectedGame = 0;
                    s_DetailOpen = true;
                }
            }

            // Also make entire card clickable
            {
                ImVec2 mp2 = ImGui::GetIO().MousePos;
                bool cardHov = (mp2.x >= cardX2 && mp2.x <= cardX2 + cardW2 &&
                                mp2.y >= cardY2 && mp2.y <= cardY2 + cardH2);
                if (cardHov) HoveringInteractive = true;
                if (cardHov && ImGui::GetIO().MouseClicked[0] && !s_DetailOpen && !s_SettingsOpen) {
                    Sound::PlayClick();
                    s_SelectedGame = 0;
                    s_DetailOpen = true;
                }
            }
        }

        // ═══ Game Card — PvZ GW2 ═══
        {
            float cardX2 = margin + 280.0f + 20.0f;
            float cardY2 = catY + 28.0f;
            float cardW2 = 280.0f;
            float cardH2 = 220.0f;
            float rounding = 10.0f;
            float imgH2 = 155.0f;
            float textPad = 12.0f;

            dl->AddRectFilled(ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + cardH2),
                IM_COL32(18, 18, 24, 220), rounding);
            dl->AddRect(ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + cardH2),
                IM_COL32(40, 40, 50, 120), rounding, 0, 1.0f);

            if (Gw2Texture) {
                float srcAspect = (float)Gw2Width / (float)Gw2Height;
                float dstAspect = cardW2 / imgH2;
                float u0 = 0, v0 = 0, u1 = 1, v1 = 1;
                if (srcAspect > dstAspect) {
                    float vis = dstAspect / srcAspect;
                    u0 = (1.0f - vis) * 0.5f; u1 = u0 + vis;
                } else {
                    float vis = srcAspect / dstAspect;
                    v0 = (1.0f - vis) * 0.5f; v1 = v0 + vis;
                }
                dl->AddImageRounded((ImTextureID)Gw2Texture,
                    ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + imgH2),
                    ImVec2(u0, v0), ImVec2(u1, v1),
                    IM_COL32(255, 255, 255, 255), rounding, ImDrawFlags_RoundCornersTop);
            }

            float textY2 = cardY2 + imgH2 + textPad;
            if (FontText)
                dl->AddText(FontText, 18.0f, ImVec2(cardX2 + textPad, textY2), IM_COL32(255,255,255,255), "PvZ GW2");
            float subY = textY2 + 22.0f;
            if (FontSmall)
                dl->AddText(FontSmall, 13.0f, ImVec2(cardX2 + textPad, subY), IM_COL32(100,105,120,255), "Free cheat");

            // Play button
            {
                float btnSz = 36.0f;
                float btnRound2 = 8.0f;
                float btnX = cardX2 + cardW2 - textPad - btnSz;
                float btnY = textY2 + 4.0f;
                ImVec2 bMin(btnX, btnY); ImVec2 bMax(btnX + btnSz, btnY + btnSz);
                ImVec2 mp = ImGui::GetIO().MousePos;
                bool btnHov = (mp.x >= bMin.x && mp.x <= bMax.x && mp.y >= bMin.y && mp.y <= bMax.y);
                if (btnHov) HoveringInteractive = true;
                s_PlayBtnHover += ((btnHov ? 1.0f : 0.0f) - s_PlayBtnHover) * dt * 12.0f;
                float bh = s_PlayBtnHover;
                if (bh > 0.01f) {
                    float exp = 3.0f * bh;
                    dl->AddRectFilled(ImVec2(bMin.x-exp,bMin.y-exp),ImVec2(bMax.x+exp,bMax.y+exp),
                        IM_COL32(0,91,187,(int)(30*bh)), btnRound2+exp);
                }
                dl->AddRectFilled(bMin, bMax, IM_COL32(0,(int)(91+30*bh),(int)(187+40*bh),255), btnRound2);
                if (FontText) {
                    const char* playIcon = ICON_FA_PLAY;
                    ImVec2 iSz = FontText->CalcTextSizeA(16.0f, FLT_MAX, 0.0f, playIcon);
                    dl->AddText(FontText, 16.0f,
                        ImVec2(btnX + (btnSz - iSz.x)*0.5f + 1, btnY + (btnSz - iSz.y)*0.5f),
                        IM_COL32(255,255,255,255), playIcon);
                }
                if (btnHov && ImGui::GetIO().MouseClicked[0] && !s_SettingsOpen) {
                    Sound::PlayClick();
                    s_SelectedGame = 1;
                    s_DetailOpen = true;
                }
            }

            // Entire card clickable
            {
                ImVec2 mp2 = ImGui::GetIO().MousePos;
                bool cardHov = (mp2.x >= cardX2 && mp2.x <= cardX2 + cardW2 &&
                                mp2.y >= cardY2 && mp2.y <= cardY2 + cardH2);
                if (cardHov) HoveringInteractive = true;
                if (cardHov && ImGui::GetIO().MouseClicked[0] && !s_DetailOpen && !s_SettingsOpen) {
                    Sound::PlayClick();
                    s_SelectedGame = 1;
                    s_DetailOpen = true;
                }
            }
        }

        // ═══ Game Card — Roblox ═══
        {
            float cardX2 = margin + 2.0f * (280.0f + 20.0f);
            float cardY2 = catY + 28.0f;
            float cardW2 = 280.0f;
            float cardH2 = 220.0f;
            float rounding = 10.0f;
            float imgH2 = 155.0f;
            float textPad = 12.0f;

            dl->AddRectFilled(ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + cardH2),
                IM_COL32(18, 18, 24, 220), rounding);
            dl->AddRect(ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + cardH2),
                IM_COL32(40, 40, 50, 120), rounding, 0, 1.0f);

            if (RbxTexture) {
                float srcAspect = (float)RbxWidth / (float)RbxHeight;
                float dstAspect = cardW2 / imgH2;
                float u0 = 0, v0 = 0, u1 = 1, v1 = 1;
                if (srcAspect > dstAspect) {
                    float vis = dstAspect / srcAspect;
                    u0 = (1.0f - vis) * 0.5f; u1 = u0 + vis;
                } else {
                    float vis = srcAspect / dstAspect;
                    v0 = (1.0f - vis) * 0.5f; v1 = v0 + vis;
                }
                dl->AddImageRounded((ImTextureID)RbxTexture,
                    ImVec2(cardX2, cardY2), ImVec2(cardX2 + cardW2, cardY2 + imgH2),
                    ImVec2(u0, v0), ImVec2(u1, v1),
                    IM_COL32(255, 255, 255, 255), rounding, ImDrawFlags_RoundCornersTop);
            }

            float textY2 = cardY2 + imgH2 + textPad;
            if (FontText)
                dl->AddText(FontText, 18.0f, ImVec2(cardX2 + textPad, textY2), IM_COL32(255,255,255,255), "Roblox");
            float subY = textY2 + 22.0f;
            if (FontSmall)
                dl->AddText(FontSmall, 13.0f, ImVec2(cardX2 + textPad, subY), IM_COL32(100,105,120,255), "Free cheat");

            // Play button
            {
                float btnSz = 36.0f;
                float btnRound2 = 8.0f;
                float btnX = cardX2 + cardW2 - textPad - btnSz;
                float btnY = textY2 + 4.0f;
                ImVec2 bMin(btnX, btnY); ImVec2 bMax(btnX + btnSz, btnY + btnSz);
                ImVec2 mp = ImGui::GetIO().MousePos;
                bool btnHov = (mp.x >= bMin.x && mp.x <= bMax.x && mp.y >= bMin.y && mp.y <= bMax.y);
                if (btnHov) HoveringInteractive = true;
                s_PlayBtnHover += ((btnHov ? 1.0f : 0.0f) - s_PlayBtnHover) * dt * 12.0f;
                float bh = s_PlayBtnHover;
                if (bh > 0.01f) {
                    float exp = 3.0f * bh;
                    dl->AddRectFilled(ImVec2(bMin.x-exp,bMin.y-exp),ImVec2(bMax.x+exp,bMax.y+exp),
                        IM_COL32(0,91,187,(int)(30*bh)), btnRound2+exp);
                }
                dl->AddRectFilled(bMin, bMax, IM_COL32(0,(int)(91+30*bh),(int)(187+40*bh),255), btnRound2);
                if (FontText) {
                    const char* playIcon = ICON_FA_PLAY;
                    ImVec2 iSz = FontText->CalcTextSizeA(16.0f, FLT_MAX, 0.0f, playIcon);
                    dl->AddText(FontText, 16.0f,
                        ImVec2(btnX + (btnSz - iSz.x)*0.5f + 1, btnY + (btnSz - iSz.y)*0.5f),
                        IM_COL32(255,255,255,255), playIcon);
                }
                if (btnHov && ImGui::GetIO().MouseClicked[0] && !s_SettingsOpen) {
                    Sound::PlayClick();
                    s_SelectedGame = 2;
                    s_DetailOpen = true;
                }
            }

            // Entire card clickable
            {
                ImVec2 mp2 = ImGui::GetIO().MousePos;
                bool cardHov = (mp2.x >= cardX2 && mp2.x <= cardX2 + cardW2 &&
                                mp2.y >= cardY2 && mp2.y <= cardY2 + cardH2);
                if (cardHov) HoveringInteractive = true;
                if (cardHov && ImGui::GetIO().MouseClicked[0] && !s_DetailOpen && !s_SettingsOpen) {
                    Sound::PlayClick();
                    s_SelectedGame = 2;
                    s_DetailOpen = true;
                }
            }
        }

        // ═══ GAME DETAIL PANEL — slides up ═══
        {
            float dt2 = ImGui::GetIO().DeltaTime;
            float target = s_DetailOpen ? 1.0f : 0.0f;
            s_DetailAnim += (target - s_DetailAnim) * dt2 * 8.0f;
            if (s_DetailAnim < 0.005f) s_DetailAnim = 0.0f;
            if (s_DetailAnim > 0.995f) s_DetailAnim = 1.0f;

            if (s_DetailAnim > 0.01f) {
                ImDrawList* fg = ImGui::GetForegroundDrawList();
                float panelMargin = 30.0f + offsetX;
                float topBarBottom = 60.0f;
                float panelW = width - panelMargin * 2 + offsetX;
                float panelH = height - topBarBottom - 15.0f;

                float eased = 1.0f - (1.0f - s_DetailAnim) * (1.0f - s_DetailAnim);
                float panelY = topBarBottom + panelH * (1.0f - eased);

                // Panel background
                fg->AddRectFilled(
                    ImVec2(panelMargin, panelY),
                    ImVec2(panelMargin + panelW, panelY + panelH),
                    IM_COL32(14, 14, 18, 250), 12.0f);
                fg->AddRect(
                    ImVec2(panelMargin, panelY),
                    ImVec2(panelMargin + panelW, panelY + panelH),
                    IM_COL32(40, 40, 50, 100), 12.0f, 0, 1.0f);

                float px = panelMargin + 20.0f;
                float py = panelY + 16.0f;
                float rightColX = panelMargin + panelW * 0.48f;
                float rightColW = panelW * 0.52f - 20.0f;

                // ── BACK button ──
                if (FontSmall) {
                    char backBuf[64]; snprintf(backBuf, sizeof(backBuf), ICON_FA_ARROW_LEFT "  BACK");
                    const char* backText = backBuf;
                    ImVec2 backSz = FontSmall->CalcTextSizeA(13.0f, FLT_MAX, 0.0f, backText);

                    ImVec2 mousePos = ImGui::GetIO().MousePos;
                    bool backHov = (mousePos.x >= px && mousePos.x <= px + backSz.x + 8 &&
                                    mousePos.y >= py && mousePos.y <= py + backSz.y + 4);
                    if (backHov) HoveringInteractive = true;
                    s_BackBtnHover += ((backHov ? 1.0f : 0.0f) - s_BackBtnHover) * dt2 * 12.0f;

                    ImU32 backCol = IM_COL32(
                        (int)(120 + 135 * s_BackBtnHover),
                        (int)(120 + 135 * s_BackBtnHover),
                        (int)(130 + 125 * s_BackBtnHover), 255);
                    fg->AddText(FontSmall, 13.0f, ImVec2(px, py), backCol, backText);

                    if (backHov && ImGui::GetIO().MouseClicked[0]) {
                        s_DetailOpen = false;
                        Sound::PlayClick();
                    }
                }

                py += 30.0f;

                const char* gameName = (s_SelectedGame == 1) ? "PvZ GW2"
                                     : (s_SelectedGame == 2) ? "Roblox" : "Unturned";
                const char* gameDesc = (s_SelectedGame == 1)
                    ? "Free cheat for PvZ GW2.\nESP, aimbot, and more."
                    : (s_SelectedGame == 2)
                        ? "External cheat for Roblox.\nESP, aimbot, and more."
                        : "Free cheat for Unturned.\nESP, aimbot, and more.";

                // ── Game Title ──
                if (FontTitle) {
                    char titleBuf[64];
                    snprintf(titleBuf, sizeof(titleBuf), ICON_FA_CROSSHAIRS "  %s", gameName);
                    fg->AddText(FontTitle, 26.0f, ImVec2(px, py),
                        IM_COL32(255, 255, 255, 255), titleBuf);
                }
                py += 36.0f;

                // ── Description ──
                if (FontSmall) {
                    fg->AddText(FontSmall, 13.0f, ImVec2(px, py),
                        IM_COL32(140, 140, 155, 255), gameDesc);
                }
                py += 55.0f;

                // ── Auto-start checkbox ──
                {
                    float boxSz = 20.0f;
                    float gap = 10.0f;
                    const char* aLabel = "Auto-start game";
                    ImVec2 aLabelSz = (FontSmall) ?
                        FontSmall->CalcTextSizeA(13.0f, FLT_MAX, 0.0f, aLabel) : ImVec2(130, 14);

                    ImVec2 aBoxMin(px, py);
                    ImVec2 aBoxMax(px + boxSz, py + boxSz);

                    ImVec2 mousePos = ImGui::GetIO().MousePos;
                    bool aHov = (mousePos.x >= px && mousePos.x <= px + boxSz + gap + aLabelSz.x &&
                                 mousePos.y >= py && mousePos.y <= py + boxSz);
                    if (aHov) HoveringInteractive = true;
                    if (aHov && ImGui::GetIO().MouseClicked[0]) {
                        s_AutoStartGame = !s_AutoStartGame;
                        Sound::PlayClick();
                        SaveSettings();
                    }

                    ImU32 aBg = aHov ? IM_COL32(25, 25, 30, 255) : IM_COL32(15, 15, 18, 255);
                    fg->AddRectFilled(aBoxMin, aBoxMax, aBg, 4.0f);
                    fg->AddRect(aBoxMin, aBoxMax, IM_COL32(50, 50, 60, 200), 4.0f, 0, 1.0f);
                    if (s_AutoStartGame) {
                        float pad = 4.0f;
                        float ax = aBoxMin.x + pad, ay = aBoxMin.y + pad, asz = boxSz - pad * 2;
                        ImVec2 tp1(ax, ay + asz * 0.5f);
                        ImVec2 tp2(ax + asz * 0.35f, ay + asz * 0.85f);
                        ImVec2 tp3(ax + asz, ay + asz * 0.1f);
                        fg->AddLine(tp1, tp2, IM_COL32(0, 120, 220, 255), 2.5f);
                        fg->AddLine(tp2, tp3, IM_COL32(0, 120, 220, 255), 2.5f);
                    }
                    if (FontSmall)
                        fg->AddText(FontSmall, 13.0f,
                            ImVec2(px + boxSz + gap, py + (boxSz - aLabelSz.y) * 0.5f),
                            IM_COL32(220, 220, 225, 255), aLabel);
                }
                py += 40.0f;

                // ── Right Column: Image + Launch Button ──
                {
                    float imgX = rightColX;
                    float imgY2 = panelY + 16.0f;
                    float imgW2 = rightColW;
                    float imgH2 = panelH * 0.55f;
                    ID3D11ShaderResourceView* img = (s_SelectedGame == 1) ? Gw2Texture
                                                : (s_SelectedGame == 2) ? RbxTexture : GameTexture;
                    int imgWsrc = (s_SelectedGame == 1) ? Gw2Width
                                : (s_SelectedGame == 2) ? RbxWidth : GameWidth;
                    int imgHsrc = (s_SelectedGame == 1) ? Gw2Height
                                : (s_SelectedGame == 2) ? RbxHeight : GameHeight;
                    if (img) {
                        float srcAspect = (float)imgWsrc / (float)imgHsrc;
                        float dstAspect = imgW2 / imgH2;
                        float u0 = 0, v0 = 0, u1 = 1, v1 = 1;
                        if (srcAspect > dstAspect) {
                            float vis = dstAspect / srcAspect;
                            u0 = (1.0f - vis) * 0.5f; u1 = u0 + vis;
                        } else {
                            float vis = srcAspect / dstAspect;
                            v0 = (1.0f - vis) * 0.5f; v1 = v0 + vis;
                        }
                        fg->AddImageRounded(
                            (ImTextureID)img,
                            ImVec2(imgX, imgY2),
                            ImVec2(imgX + imgW2, imgY2 + imgH2),
                            ImVec2(u0, v0), ImVec2(u1, v1),
                            IM_COL32(255, 255, 255, 255), 10.0f);
                    }

                    // ── Launch/Inject Button ──
                    float launchY = imgY2 + imgH2 + 16.0f;
                    float launchH = 44.0f;
                    float launchRound = 10.0f;
                    ImVec2 lMin(imgX, launchY);
                    ImVec2 lMax(imgX + imgW2, launchY + launchH);

                    ImVec2 mousePos = ImGui::GetIO().MousePos;
                    bool launchHov = (mousePos.x >= lMin.x && mousePos.x <= lMax.x &&
                                     mousePos.y >= lMin.y && mousePos.y <= lMax.y);
                    if (launchHov) HoveringInteractive = true;
                    s_LaunchBtnHover += ((launchHov ? 1.0f : 0.0f) - s_LaunchBtnHover) * dt2 * 10.0f;
                    float lh = s_LaunchBtnHover;

                    // Glow
                    if (lh > 0.01f) {
                        float exp = 4.0f * lh;
                        fg->AddRectFilled(
                            ImVec2(lMin.x - exp, lMin.y - exp),
                            ImVec2(lMax.x + exp, lMax.y + exp),
                            IM_COL32(0, 91, 187, (int)(25 * lh)), launchRound + exp);
                    }

                    // Button gradient
                    ImU32 ltop = IM_COL32(0, (int)(91 + 30 * lh), (int)(187 + 40 * lh), 255);
                    ImU32 lbot = IM_COL32(0, (int)(68 + 20 * lh), (int)(153 + 30 * lh), 255);
                    fg->AddRectFilledMultiColor(lMin, lMax, ltop, ltop, lbot, lbot);
                    fg->AddRect(lMin, lMax, IM_COL32(0, 120, 220, (int)(40 + 60 * lh)),
                        launchRound, 0, 1.5f);

                    // Shine on hover
                    if (lh > 0.01f) {
                        float sw = imgW2 * 0.5f;
                        float sx = imgX + (imgW2 - sw) * 0.5f;
                        fg->AddLine(ImVec2(sx, lMin.y + 1), ImVec2(sx + sw, lMin.y + 1),
                            IM_COL32(255, 255, 255, (int)(40 * lh)), 1.0f);
                    }

                    // Label centered
                    if (FontText) {
                        char launchBuf[64]; snprintf(launchBuf, sizeof(launchBuf),
                            ICON_FA_PLAY "   %s", (s_SelectedGame == 2) ? "Launch" : "Inject");
                        const char* launchLabel = launchBuf;
                        ImVec2 lsz = FontText->CalcTextSizeA(18.0f, FLT_MAX, 0.0f, launchLabel);
                        float tx = imgX + (imgW2 - lsz.x) * 0.5f;
                        float ty = launchY + (launchH - lsz.y) * 0.5f - lh * 1.5f;
                        fg->AddText(FontText, 18.0f, ImVec2(tx, ty),
                            IM_COL32(255, 255, 255, 255), launchLabel);
                    }

                    if (launchHov && ImGui::GetIO().MouseClicked[0] && !s_Launching) {
                        Sound::PlayActivate();
                        s_Launching = true;
                        s_LaunchTime = 0.0f;
                        s_LaunchPhase = 1;
                        s_LaunchSuccess = false;
                        s_LaunchMsg[0] = '\0';
                    }
                }
            }
        }

        // ═══ SETTINGS PANEL — slides up ═══
        {
            float sdt = ImGui::GetIO().DeltaTime;
            float sTarget = s_SettingsOpen ? 1.0f : 0.0f;
            s_SettingsAnim += (sTarget - s_SettingsAnim) * sdt * 8.0f;
            if (s_SettingsAnim < 0.005f) s_SettingsAnim = 0.0f;
            if (s_SettingsAnim > 0.995f) s_SettingsAnim = 1.0f;

            if (s_SettingsAnim > 0.01f) {
                ImDrawList* fg = ImGui::GetForegroundDrawList();
                float sPanelMargin = 30.0f + offsetX;
                float sTopBarBot = 60.0f;
                float sPanelW = width - sPanelMargin * 2 + offsetX;
                float sPanelH = height - sTopBarBot - 15.0f;
                float sEased = 1.0f - (1.0f - s_SettingsAnim) * (1.0f - s_SettingsAnim);
                float sPanelY = sTopBarBot + sPanelH * (1.0f - sEased);

                fg->AddRectFilled(
                    ImVec2(sPanelMargin, sPanelY),
                    ImVec2(sPanelMargin + sPanelW, sPanelY + sPanelH),
                    IM_COL32(14, 14, 18, 250), 12.0f);
                fg->AddRect(
                    ImVec2(sPanelMargin, sPanelY),
                    ImVec2(sPanelMargin + sPanelW, sPanelY + sPanelH),
                    IM_COL32(40, 40, 50, 100), 12.0f, 0, 1.0f);

                float spx = sPanelMargin + 20.0f;
                float spy = sPanelY + 20.0f;
                float sContentW = sPanelW - 40.0f;

                // ── General Section ──
                if (FontText) {
                    fg->AddText(FontText, 18.0f, ImVec2(spx, spy),
                        Style::COL_ACCENT, ICON_FA_GEAR);
                    fg->AddText(FontText, 18.0f, ImVec2(spx + 28.0f, spy),
                        Style::COL_TEXT, "General");
                }
                spy += 34.0f;

                // General Card — Always on Top + Auto Start
                {
                    float gcH = 124.0f;
                    fg->AddRectFilled(ImVec2(spx, spy), ImVec2(spx + sContentW, spy + gcH),
                        Style::COL_CARD, 10.0f);
                    fg->AddRect(ImVec2(spx, spy), ImVec2(spx + sContentW, spy + gcH),
                        IM_COL32(40, 40, 48, 120), 10.0f, 0, 1.0f);

                    float iy = spy + 16.0f;
                    float ix = spx + 16.0f;

                    // Always on Top label
                    if (FontSmall)
                        fg->AddText(FontSmall, 14.0f, ImVec2(ix, iy + 3.0f),
                            Style::COL_TEXT, "Always on Top");

                    // Toggle pill (40x22)
                    float tW = 40.0f, tH = 22.0f;
                    float tX = spx + sContentW - 16.0f - tW;
                    float tY = iy;
                    ImU32 pillCol = s_AlwaysOnTop ? Style::COL_ACCENT : IM_COL32(60, 60, 68, 255);
                    fg->AddRectFilled(ImVec2(tX, tY), ImVec2(tX + tW, tY + tH),
                        pillCol, tH * 0.5f);
                    float knobR = 8.0f;
                    float knobX = s_AlwaysOnTop ? tX + tW - knobR - 3.0f : tX + knobR + 3.0f;
                    fg->AddCircleFilled(ImVec2(knobX, tY + tH * 0.5f), knobR,
                        IM_COL32(255, 255, 255, 255), 24);

                    ImVec2 tMp = ImGui::GetIO().MousePos;
                    bool topToggleHov = (tMp.x >= tX && tMp.x <= tX + tW && tMp.y >= tY && tMp.y <= tY + tH);
                    if (topToggleHov) HoveringInteractive = true;
                    if (topToggleHov && ImGui::GetIO().MouseClicked[0]) {
                        s_AlwaysOnTop = !s_AlwaysOnTop;
                        Sound::PlayClick();
                        SaveSettings();
                        HWND hwnd = FindWindowW(nullptr, L"Hamas Client");
                        if (hwnd)
                            SetWindowPos(hwnd,
                                s_AlwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST,
                                0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    }

                    // Auto Start Game toggle
                    iy += 34.0f;
                    if (FontSmall)
                        fg->AddText(FontSmall, 14.0f, ImVec2(ix, iy + 3.0f),
                            Style::COL_TEXT, "Auto Start Game");

                    float asW = 40.0f, asH = 22.0f;
                    float asX = spx + sContentW - 16.0f - asW;
                    float asY = iy;
                    ImU32 asPill = s_AutoStartGame ? Style::COL_ACCENT : IM_COL32(60, 60, 68, 255);
                    fg->AddRectFilled(ImVec2(asX, asY), ImVec2(asX + asW, asY + asH), asPill, asH * 0.5f);
                    float asKnobR = 8.0f;
                    float asKnobX = s_AutoStartGame ? asX + asW - asKnobR - 3.0f : asX + asKnobR + 3.0f;
                    fg->AddCircleFilled(ImVec2(asKnobX, asY + asH * 0.5f), asKnobR,
                        IM_COL32(255, 255, 255, 255), 24);

                    ImVec2 asMp = ImGui::GetIO().MousePos;
                    bool autoToggleHov = (asMp.x >= asX && asMp.x <= asX + asW && asMp.y >= asY && asMp.y <= asY + asH);
                    if (autoToggleHov) HoveringInteractive = true;
                    if (autoToggleHov && ImGui::GetIO().MouseClicked[0]) {
                        s_AutoStartGame = !s_AutoStartGame;
                        Sound::PlayClick();
                        SaveSettings();
                    }

                    // Ghost Mode toggle
                    iy += 34.0f;
                    if (FontSmall)
                        fg->AddText(FontSmall, 14.0f, ImVec2(ix, iy + 3.0f),
                            Style::COL_TEXT, "Ghost Mode");

                    float gmW = 40.0f, gmH = 22.0f;
                    float gmX = spx + sContentW - 16.0f - gmW;
                    float gmY = iy;
                    ImU32 gmPill = s_GhostMode ? Style::COL_ACCENT : IM_COL32(60, 60, 68, 255);
                    fg->AddRectFilled(ImVec2(gmX, gmY), ImVec2(gmX + gmW, gmY + gmH), gmPill, gmH * 0.5f);
                    float gmKnobR = 8.0f;
                    float gmKnobX = s_GhostMode ? gmX + gmW - gmKnobR - 3.0f : gmX + gmKnobR + 3.0f;
                    fg->AddCircleFilled(ImVec2(gmKnobX, gmY + gmH * 0.5f), gmKnobR,
                        IM_COL32(255, 255, 255, 255), 24);

                    ImVec2 gmMp = ImGui::GetIO().MousePos;
                    bool ghostToggleHov = (gmMp.x >= gmX && gmMp.x <= gmX + gmW && gmMp.y >= gmY && gmMp.y <= gmY + gmH);
                    if (ghostToggleHov) HoveringInteractive = true;
                    if (ghostToggleHov && ImGui::GetIO().MouseClicked[0]) {
                        s_GhostMode = !s_GhostMode;
                        GhostMode = s_GhostMode;
                        Sound::PlayClick();
                        SaveSettings();
                    }
                }
            }
        }

        // ═══ LAUNCH OVERLAY — loading animation + status ═══
        if (s_Launching) {
            float dt3 = ImGui::GetIO().DeltaTime;
            s_LaunchTime += dt3;
            ImDrawList* fg = ImGui::GetForegroundDrawList();

            // Phase 1: Loading animation
            if (s_LaunchPhase == 1) {
                static bool s_injectStarted = false;
                if (!s_injectStarted) {
                    Sound::StopHamasSong();
                    Inject::StartAsync(s_SelectedGame, s_AutoStartGame);
                    s_injectStarted = true;
                }

                float tintAlpha = (s_LaunchTime < 0.3f) ? s_LaunchTime / 0.3f : 1.0f;
                fg->AddRectFilled(ImVec2(0, 0), ImVec2(width, height),
                    IM_COL32(255, 255, 255, (int)(12 * tintAlpha)));

                float cx = width * 0.5f;
                float cy = height * 0.5f;
                float t = s_LaunchTime;
                float spread = (t < 0.5f) ? 0.0f : (t - 0.5f) * 80.0f;
                if (spread > 40.0f) spread = 40.0f;
                float baseR = 12.0f;
                float pulse = 1.0f + 0.15f * sinf(t * 6.0f);
                float r = baseR * pulse;
                float angle = t * 3.0f;
                int numCircles = (t < 0.5f) ? 1 : 4;
                for (int i = 0; i < numCircles; i++) {
                    float a = angle + i * (3.14159f * 0.5f);
                    float px = cx + cosf(a) * spread;
                    float py = cy + sinf(a) * spread;
                    fg->AddCircleFilled(ImVec2(px, py), r + 6.0f, IM_COL32(0, 91, 187, 30), 32);
                    fg->AddCircleFilled(ImVec2(px, py), r,
                        IM_COL32(180, 190, 220, (int)(200 + 55 * sinf(t * 4.0f + i))), 32);
                }

                if (FontSmall) {
                    const char* st = Inject::GetStatus();
                    ImVec2 stSz = FontSmall->CalcTextSizeA(13.0f, FLT_MAX, 0.0f, st);
                    fg->AddText(FontSmall, 13.0f,
                        ImVec2(cx - stSz.x * 0.5f, cy + 50.0f),
                        IM_COL32(160, 160, 175, 255), st);
                }

                if (Inject::IsDone()) {
                    s_injectStarted = false;
                    s_LaunchPhase = 2;
                    s_LaunchTime = 0.0f;

                    if (Inject::Succeeded()) {
                        s_LaunchSuccess = true;
                        snprintf(s_LaunchMsg, sizeof(s_LaunchMsg), "Injected successfully!");
                        s_CloseTimer = 5.0f;
                    } else {
                        s_LaunchSuccess = false;
                        snprintf(s_LaunchMsg, sizeof(s_LaunchMsg), "%s", Inject::GetStatus());
                        s_CloseTimer = 99999.0f;
                    }
                }
            }

            // Phase 2: Status bar
            if (s_LaunchPhase == 2) {
                s_CloseTimer -= dt3;

                // Subtle overlay stays
                fg->AddRectFilled(ImVec2(0, 0), ImVec2(width, height),
                    IM_COL32(0, 0, 0, 80));

                // Status bar at bottom
                float barH = 60.0f;
                float barMargin = 30.0f + offsetX;
                float barY = height - barH - 15.0f;
                float barW = width - barMargin * 2;

                ImU32 barBg = s_LaunchSuccess
                    ? IM_COL32(18, 18, 24, 240)
                    : IM_COL32(40, 12, 12, 240);
                ImU32 barBorder = s_LaunchSuccess
                    ? IM_COL32(0, 91, 187, 100)
                    : IM_COL32(200, 40, 40, 150);
                ImU32 accentCol = s_LaunchSuccess
                    ? IM_COL32(0, 91, 187, 255)
                    : IM_COL32(220, 50, 50, 255);

                fg->AddRectFilled(ImVec2(barMargin, barY),
                    ImVec2(barMargin + barW, barY + barH),
                    barBg, 10.0f);
                fg->AddRect(ImVec2(barMargin, barY),
                    ImVec2(barMargin + barW, barY + barH),
                    barBorder, 10.0f, 0, 1.0f);

                // Accent line on left edge
                fg->AddRectFilled(
                    ImVec2(barMargin, barY + 8),
                    ImVec2(barMargin + 3, barY + barH - 8),
                    accentCol, 2.0f);

                // Status text
                if (FontText) {
                    const char* title = s_LaunchSuccess
                        ? "Injected successfully!"
                        : "Injection failed";
                    fg->AddText(FontText, 16.0f,
                        ImVec2(barMargin + 18.0f, barY + 10.0f),
                        IM_COL32(255, 255, 255, 255), title);
                }
                if (FontSmall) {
                    // Countdown or error message
                    char subMsg[128];
                    if (s_LaunchSuccess) {
                        int secs = (int)ceilf(s_CloseTimer);
                        if (secs < 0) secs = 0;
                        snprintf(subMsg, sizeof(subMsg), "Closing in %d seconds...", secs);
                    } else {
                        snprintf(subMsg, sizeof(subMsg), "%s", s_LaunchMsg);
                    }
                    fg->AddText(FontSmall, 13.0f,
                        ImVec2(barMargin + 18.0f, barY + 34.0f),
                        IM_COL32(140, 140, 155, 255), subMsg);

                    // Close window button on right
                    const char* closeLabel = "Close window";
                    ImVec2 clSz = FontSmall->CalcTextSizeA(13.0f, FLT_MAX, 0.0f, closeLabel);
                    float clBtnW = clSz.x + 24.0f;
                    float clBtnH = 32.0f;
                    float clBtnX = barMargin + barW - clBtnW - 14.0f;
                    float clBtnY = barY + (barH - clBtnH) * 0.5f;

                    fg->AddRectFilled(ImVec2(clBtnX, clBtnY),
                        ImVec2(clBtnX + clBtnW, clBtnY + clBtnH),
                        IM_COL32(40, 40, 50, 200), 6.0f);
                    fg->AddRect(ImVec2(clBtnX, clBtnY),
                        ImVec2(clBtnX + clBtnW, clBtnY + clBtnH),
                        IM_COL32(70, 70, 80, 200), 6.0f, 0, 1.0f);
                    fg->AddText(FontSmall, 13.0f,
                        ImVec2(clBtnX + 12.0f, clBtnY + (clBtnH - clSz.y) * 0.5f),
                        IM_COL32(200, 200, 210, 255), closeLabel);

                    // Click close button
                    ImVec2 mp = ImGui::GetIO().MousePos;
                    bool clBtnHov = (mp.x >= clBtnX && mp.x <= clBtnX + clBtnW &&
                        mp.y >= clBtnY && mp.y <= clBtnY + clBtnH);
                    if (clBtnHov) HoveringInteractive = true;
                    if (clBtnHov && ImGui::GetIO().MouseClicked[0]) {
                        WantsClose = true;
                    }
                }

                // Auto-close on timer
                if (s_LaunchSuccess && s_CloseTimer <= 0.0f) {
                    WantsClose = true;
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  Debug Log window — tails kernel driver + loader + kdmapper logs
    // ══════════════════════════════════════════════════════════════
    static void AppendFile(const char* path, const char* label) {
        if (strlen(s_LogBuf) > 30000) return; // safety cap
        HANDLE h = CreateFileA(path, GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL, nullptr);
        if (h == INVALID_HANDLE_VALUE) return;
        DWORD sz = GetFileSize(h, nullptr);
        if (sz > 12000) sz = 12000; // show last ~12KB of each log
        DWORD bytesRead = 0;
        SetFilePointer(h, -(LONG)sz, nullptr, FILE_END);
        char* tmp = (char*)alloca(sz + 1);
        ReadFile(h, tmp, sz, &bytesRead, nullptr);
        CloseHandle(h);
        tmp[bytesRead] = '\0';

        size_t cur = strlen(s_LogBuf);
        snprintf(s_LogBuf + cur, sizeof(s_LogBuf) - cur,
            "=== %s ===\n%s\n", label, tmp);
    }
    static void RefreshLogs() {
        s_LogBuf[0] = '\0';
        AppendFile("C:\\Users\\Public\\kmap_status.txt",    "KERNEL DRIVER (kmap_status.txt)");
        AppendFile("C:\\Users\\Public\\kdmapper_output.txt", "KDMAPPER (kdmapper_output.txt)");
        AppendFile("C:\\Users\\Public\\hamas_debug.txt",    "LOADER (hamas_debug.txt)");
    }

    static void CopyLogToClipboard() {
        if (!s_LogBuf[0]) return;
        if (!OpenClipboard(nullptr)) return;
        EmptyClipboard();
        size_t len = strlen(s_LogBuf);
        HGLOBAL h = GlobalAlloc(GMEM_MOVEABLE, len + 1);
        if (h) {
            void* p = GlobalLock(h);
            if (p) { memcpy(p, s_LogBuf, len + 1); GlobalUnlock(h); }
            SetClipboardData(CF_TEXT, h);
        }
        CloseClipboard();
        s_LogCopied = true;
        s_LogCopiedTimer = 1.5f;
    }

    static void RenderLogWindow() {
        if (!s_LogOpen) return;

        // Auto-refresh every 0.75s
        s_LogRefreshTimer -= ImGui::GetIO().DeltaTime;
        if (s_LogRefreshTimer <= 0.0f) {
            RefreshLogs();
            s_LogRefreshTimer = 0.75f;
        }

        ImVec2 disp = ImGui::GetIO().DisplaySize;
        ImGui::SetNextWindowPos(
            ImVec2(disp.x * 0.5f - 340.0f, disp.y * 0.5f - 240.0f),
            ImGuiCond_FirstUseEver);
        ImGui::SetNextWindowSize(ImVec2(680.0f, 480.0f), ImGuiCond_FirstUseEver);

        // Opaque background — the global style uses a transparent WindowBg,
        // so we override it here or the window would be invisible.
        ImGui::PushStyleColor(ImGuiCol_WindowBg,      ImVec4(0.055f, 0.055f, 0.07f, 0.99f));
        ImGui::PushStyleColor(ImGuiCol_Border,        ImVec4(0.0f, 0.35f, 0.2f, 1.0f));
        ImGui::PushStyleColor(ImGuiCol_TitleBg,       ImVec4(0.07f, 0.07f, 0.09f, 1.0f));
        ImGui::PushStyleColor(ImGuiCol_TitleBgActive, ImVec4(0.09f, 0.09f, 0.12f, 1.0f));
        ImGui::PushStyleColor(ImGuiCol_ChildBg,       ImVec4(0.01f, 0.01f, 0.02f, 1.0f));
        ImGui::PushStyleColor(ImGuiCol_Text,          ImVec4(0.75f, 0.85f, 0.78f, 1.0f));
        ImGui::PushStyleVar(ImGuiStyleVar_WindowRounding, 8.0f);
        ImGui::PushStyleVar(ImGuiStyleVar_WindowBorderSize, 1.0f);
        ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(10.0f, 10.0f));

        if (!ImGui::Begin("Debug Log", &s_LogOpen)) {
            ImGui::End();
            ImGui::PopStyleVar(3);
            ImGui::PopStyleColor(6);
            return;
        }

        // Toolbar
        if (ImGui::Button("Refresh")) { RefreshLogs(); }
        ImGui::SameLine();
        if (ImGui::Button("Copy")) { CopyLogToClipboard(); }
        ImGui::SameLine();
        if (ImGui::Button("Clear")) {
            DeleteFileA("C:\\Users\\Public\\kmap_status.txt");
            DeleteFileA("C:\\Users\\Public\\kdmapper_output.txt");
            DeleteFileA("C:\\Users\\Public\\hamas_debug.txt");
            RefreshLogs();
        }

        // "Copied!" feedback
        if (s_LogCopied) {
            s_LogCopiedTimer -= ImGui::GetIO().DeltaTime;
            if (s_LogCopiedTimer <= 0.0f) s_LogCopied = false;
            ImGui::SameLine();
            ImGui::PushStyleColor(ImGuiCol_Text, ImVec4(0.3f, 0.9f, 0.5f, 1.0f));
            ImGui::Text(ICON_FA_CHECK " Copied");
            ImGui::PopStyleColor();
        }
        ImGui::Separator();

        ImGui::BeginChild("##logscroll", ImVec2(0, 0), true,
            ImGuiWindowFlags_HorizontalScrollbar);
        ImGui::TextUnformatted(s_LogBuf[0] ? s_LogBuf : "(no logs yet — run an injection)");
        // Stick to bottom when new content arrives
        if (ImGui::GetScrollY() >= ImGui::GetScrollMaxY() - 20.0f) {
            ImGui::SetScrollHereY(1.0f);
        }
        ImGui::EndChild();

        ImGui::End();
        ImGui::PopStyleVar(3);
        ImGui::PopStyleColor(6);
    }


    void RenderFrame(float width, float height) {
        float dt = ImGui::GetIO().DeltaTime;
        HoveringInteractive = false;

        // Apply deferred topmost setting
        if (s_NeedApplyTopmost) {
            s_NeedApplyTopmost = false;
            HWND hwnd = FindWindowW(nullptr, L"Hamas Client");
            if (hwnd)
                SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        // ── NOT ADMIN — show error overlay and block everything ──
        if (NotAdmin) {
            s_AnimTime += dt;
            ImDrawList* bgDL = ImGui::GetBackgroundDrawList();
            DrawBackground(bgDL, width, height);

            // Dark blur overlay
            bgDL->AddRectFilled(ImVec2(0, 0), ImVec2(width, height),
                IM_COL32(0, 0, 0, 180));

            // Centered popup card
            float popW = 340.0f;
            float popH = 160.0f;
            float popX = (width - popW) * 0.5f;
            float popY = (height - popH) * 0.5f;

            // Card background
            bgDL->AddRectFilled(ImVec2(popX, popY), ImVec2(popX + popW, popY + popH),
                Style::COL_CARD, 12.0f);
            bgDL->AddRect(ImVec2(popX, popY), ImVec2(popX + popW, popY + popH),
                IM_COL32(220, 50, 50, 120), 12.0f, 0, 1.5f);

            // Red accent line on top
            bgDL->AddRectFilled(
                ImVec2(popX + 40, popY),
                ImVec2(popX + popW - 40, popY + 2),
                IM_COL32(220, 50, 50, 200), 1.0f);

            float cy = popY + 28.0f;

            // Warning icon + title
            if (FontTitle) {
                const char* errTitle = ICON_FA_SHIELD "  Run as Administrator";
                ImVec2 tsz = FontTitle->CalcTextSizeA(22.0f, FLT_MAX, 0.0f, errTitle);
                bgDL->AddText(FontTitle, 22.0f,
                    ImVec2(popX + (popW - tsz.x) * 0.5f, cy),
                    IM_COL32(220, 60, 60, 255), errTitle);
                cy += 34.0f;
            }

            // Subtitle
            if (FontSmall) {
                const char* sub = "This application requires administrator";
                ImVec2 ssz = FontSmall->CalcTextSizeA(13.0f, FLT_MAX, 0.0f, sub);
                bgDL->AddText(FontSmall, 13.0f,
                    ImVec2(popX + (popW - ssz.x) * 0.5f, cy),
                    Style::COL_TEXT_SUB, sub);
                cy += 18.0f;

                const char* sub2 = "privileges to run properly.";
                ImVec2 s2sz = FontSmall->CalcTextSizeA(13.0f, FLT_MAX, 0.0f, sub2);
                bgDL->AddText(FontSmall, 13.0f,
                    ImVec2(popX + (popW - s2sz.x) * 0.5f, cy),
                    Style::COL_TEXT_SUB, sub2);
                cy += 28.0f;
            }

            // Close button
            {
                float btnW = 120.0f, btnH = 32.0f;
                float btnX = popX + (popW - btnW) * 0.5f;
                float btnY = cy;
                ImVec2 bMin(btnX, btnY);
                ImVec2 bMax(btnX + btnW, btnY + btnH);

                ImVec2 mp = ImGui::GetIO().MousePos;
                bool hov = (mp.x >= bMin.x && mp.x <= bMax.x &&
                            mp.y >= bMin.y && mp.y <= bMax.y);
                if (hov) HoveringInteractive = true;

                ImU32 btnCol = hov ? IM_COL32(200, 40, 40, 255) : IM_COL32(160, 35, 35, 255);
                bgDL->AddRectFilled(bMin, bMax, btnCol, 8.0f);

                if (FontSmall) {
                    const char* cl = "Close";
                    ImVec2 csz = FontSmall->CalcTextSizeA(14.0f, FLT_MAX, 0.0f, cl);
                    bgDL->AddText(FontSmall, 14.0f,
                        ImVec2(btnX + (btnW - csz.x) * 0.5f, btnY + (btnH - csz.y) * 0.5f),
                        IM_COL32(255, 255, 255, 255), cl);
                }

                if (hov && ImGui::GetIO().MouseClicked[0])
                    WantsClose = true;
            }

            return; // block all other rendering
        }

        // Start transition when LoggedIn flips to true
        if (LoggedIn && s_TransitionT < 1.0f && !s_Transitioning) {
            s_Transitioning = true;
            s_TransitionDelay = 0.3f;
        }

        // Tick delay
        if (s_Transitioning && s_TransitionDelay > 0.0f) {
            s_TransitionDelay -= dt;
        }

        // Animate transition (ease-out cubic)
        if (s_Transitioning && s_TransitionDelay <= 0.0f) {
            s_TransitionT += dt * 2.2f;
            if (s_TransitionT >= 1.0f) {
                s_TransitionT = 1.0f;
                s_Transitioning = false;
            }
        }

        // Ease-out cubic: fast start, smooth deceleration
        float t = s_TransitionT;
        float eased = 1.0f - (1.0f - t) * (1.0f - t) * (1.0f - t);

        ImDrawList* bgDL = ImGui::GetBackgroundDrawList();
        DrawBackground(bgDL, width, height);

        // Render the debug log window FIRST so it captures mouse input before
        // the custom-drawn UI below. If the log window is under the cursor,
        // swallow the click so the loader's buttons underneath don't fire.
        RenderLogWindow();
        if (s_LogOpen && ImGui::GetIO().WantCaptureMouse) {
            ImGui::GetIO().MouseClicked[0] = false;
            ImGui::GetIO().MouseClicked[1] = false;
        }

        if (t <= 0.0f) {
            // Pure login
            RenderLogin(width, height, 0.0f);
        } else if (t >= 1.0f) {
            // Pure dashboard
            RenderDashboard(width, height, 0.0f);
        } else {
            // Both visible — login slides left, dashboard slides in from right
            float loginOffset  = -width * eased;
            float dashOffset   =  width * (1.0f - eased);

            // Fade overlay during transition
            int fadeAlpha = (int)(60.0f * sinf(t * 3.14159f));
            bgDL->AddRectFilled(ImVec2(0, 0), ImVec2(width, height),
                IM_COL32(0, 0, 0, fadeAlpha));

            RenderLogin(width, height, loginOffset);
            RenderDashboard(width, height, dashOffset);
        }
    }
}
