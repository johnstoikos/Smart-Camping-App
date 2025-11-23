using System;
using System.Drawing;

namespace SmartCamping.Models
{
    public enum LightingEffect
    {
        Static = 0,
        NightLight = 1,  // ζεστό χαμηλό φως
        Reading = 2,     // ψυχρό πιο δυνατό
        Pulse = 3,       // παλλόμενη ένταση (party)
        ColorCycle = 4   // κυκλική εναλλαγή χρώματος
    }

    public class LightingState
    {
        public bool IsOn { get; set; } = true;
        public int Brightness { get; set; } = 60;    
        public Color Color { get; set; } = Color.FromArgb(255, 255, 244, 230);
        public LightingEffect Effect { get; set; } = LightingEffect.Static;
        public bool AutoNight { get; set; } = true;

        public LightingState Clone() => new LightingState
        {
            IsOn = IsOn,
            Brightness = Brightness,
            Color = Color,
            Effect = Effect,
            AutoNight = AutoNight
        };
    }
}
