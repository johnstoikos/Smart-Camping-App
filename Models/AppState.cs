using System;
using System.Drawing;

namespace SmartCamping.Models
{
    public sealed class AppState
    {
        private static readonly Lazy<AppState> _i = new(() => new AppState());
        public static AppState I => _i.Value;

        // Δεδομένα για στήσιμο σκηνής
        public PointF? SelectedSite { get; set; }
        public float GroundStability { get; set; } // 0..1
        public float Humidity { get; set; }        // 0..1
        public float SunExposure { get; set; }     // 0..1

        // Όποτε αλλάζει κάτι, ειδοποιούμε το UI
        public event Action? StateChanged;
        public void Notify() => StateChanged?.Invoke();
    }
}
