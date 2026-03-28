using UnityEngine;

namespace Scripts
{
    [DisallowMultipleComponent]
    public class DisplayFrameRateController : MonoBehaviour
    {
        [Header("Frame Rate")]
        [SerializeField] private bool limitToMonitorRefresh = true;
        [SerializeField] private int customTargetFrameRate = -1;

        [Header("VSync")]
        [SerializeField] private bool useVSync = true;
        [SerializeField] private int vSyncCount = 1;

        private void Awake()
        {
            ApplySettings();
        }

        private void OnValidate()
        {
            vSyncCount = Mathf.Clamp(vSyncCount, 0, 4);

            if (!Application.isPlaying)
                return;

            ApplySettings();
        }

        public void SetUseVSync(bool enabled)
        {
            useVSync = enabled;
            ApplySettings();
        }

        public void SetLimitToMonitorRefresh(bool enabled)
        {
            limitToMonitorRefresh = enabled;
            ApplySettings();
        }

        public void SetCustomTargetFrameRate(int fps)
        {
            customTargetFrameRate = fps;
            ApplySettings();
        }

        public void ApplySettings()
        {
            if (useVSync)
            {
                QualitySettings.vSyncCount = Mathf.Max(1, vSyncCount);
                Application.targetFrameRate = -1;
                return;
            }

            QualitySettings.vSyncCount = 0;

            if (limitToMonitorRefresh)
            {
                Application.targetFrameRate = GetCurrentDisplayRefreshRate();
                return;
            }

            Application.targetFrameRate = customTargetFrameRate > 0 ? customTargetFrameRate : -1;
        }

        private static int GetCurrentDisplayRefreshRate()
        {
            var refreshRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            return Mathf.Max(30, refreshRate);
        }
    }
}
