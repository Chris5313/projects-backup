using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
    public static class AimAssist
    {
        public static bool Enabled => State.AimAssistOn;

        private static float _currentYaw;
        private static float _currentPitch;
        private static bool _assisting;
        private static float _lastTargetTime;

        private const float FRONT_FOV = 90f;
        private const float MAX_DIST = 200f;

        private static float SmoothSpeed => Mathf.Lerp(0.05f, 0.3f, State.AimAssistSmoothness);

        // Cached reflection fields for PlayerLook
        private static FieldInfo _lookYawField;
        private static FieldInfo _lookPitchField;
        private static bool _lookFieldsCached;

        private static void CacheLookFields()
        {
            if (_lookFieldsCached) return;
            _lookFieldsCached = true;

            BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (string name in new[] { "_yaw", "_angleYaw", "m_Yaw", "_lookYaw", "_y", "_horizontal" })
            {
                _lookYawField = typeof(PlayerLook).GetField(name, bf);
                if (_lookYawField != null && (_lookYawField.FieldType == typeof(float) || _lookYawField.FieldType == typeof(byte))) break;
                _lookYawField = null;
            }

            foreach (string name in new[] { "_pitch", "_anglePitch", "m_Pitch", "_lookPitch", "_x", "_vertical" })
            {
                _lookPitchField = typeof(PlayerLook).GetField(name, bf);
                if (_lookPitchField != null && (_lookPitchField.FieldType == typeof(float) || _lookPitchField.FieldType == typeof(byte))) break;
                _lookPitchField = null;
            }
        }

        public static void Update()
        {
            if (!Enabled || State.IsSpying || State.Open)
            {
                _assisting = false;
                return;
            }

            Player lp = Player.player;
            if (lp == null || lp.equipment == null) return;
            if (!(lp.equipment.useable is UseableGun)) return;
            if (!Input.GetMouseButton(1) && State.AimAssistRequireADS) return;

            // Use SilentAim's locked target if available
            object target = SilentAim.LockedTarget;
            if (target == null)
            {
                if (Time.time - _lastTargetTime > 0.3f)
                    _assisting = false;
                return;
            }

            Vector3 targetPos = SilentAim.LockedTargetPos;
            Vector3 aimPos = lp.look.aim.position;
            Vector3 aimForward = lp.look.aim.forward;
            Vector3 toTarget = targetPos - aimPos;
            float dist = toTarget.magnitude;

            if (dist > MAX_DIST || dist < 2f)
            {
                _assisting = false;
                return;
            }

            toTarget.Normalize();
            float angle = Vector3.Angle(aimForward, toTarget);
            if (angle > FRONT_FOV * 0.5f)
            {
                _assisting = false;
                return;
            }

            if (State.AimAssistVisCheck)
            {
                int mask = RayMasks.DAMAGE_CLIENT & ~1025 & ~67108865;
                if (Physics.Linecast(aimPos, targetPos, mask, QueryTriggerInteraction.Collide))
                {
                    _assisting = false;
                    return;
                }
            }

            _lastTargetTime = Time.time;
            _assisting = true;

            // Calculate desired angles
            Vector3 dir = (targetPos - aimPos).normalized;
            float desiredYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float desiredPitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg + 90f;

            while (desiredYaw < 0f) desiredYaw += 360f;
            while (desiredYaw >= 360f) desiredYaw -= 360f;

            // Smooth toward target
            float speed = SmoothSpeed * Time.deltaTime * 60f;
            _currentYaw = SmoothAngle(lp.look.yaw, desiredYaw, speed * 1.2f);
            _currentPitch = Mathf.Lerp(lp.look.pitch, desiredPitch, speed * 0.8f);
            _currentPitch = Mathf.Clamp(_currentPitch, 0f, 180f);

            // Apply
            CacheLookFields();
            if (lp.look != null)
            {
                if (_lookYawField != null)
                    _lookYawField.SetValue(lp.look, _currentYaw);
                if (_lookPitchField != null)
                    _lookPitchField.SetValue(lp.look, _currentPitch);

                // Call updateLook to refresh camera
                try { lp.look.updateLook(); } catch { }
            }
        }

        private static float SmoothAngle(float current, float target, float t)
        {
            float diff = target - current;
            while (diff > 180f) diff -= 360f;
            while (diff < -180f) diff += 360f;
            return current + diff * Mathf.Clamp01(t);
        }

        public static void DrawOverlay()
        {
            if (!Enabled || !_assisting) return;
            if (!State.AimAssistIndicator) return;

            Vector3 screenPos = MainCamera.instance.WorldToScreenPoint(SilentAim.LockedTargetPos);
            if (screenPos.z <= 0f) return;

            float x = screenPos.x;
            float y = Screen.height - screenPos.y;

            Color c = new Color(1f, 1f, 1f, 0.5f);
            GUI.color = c;
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, 4f, 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}