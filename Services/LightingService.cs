using System;
using System.Drawing;
using SmartCamping.Models;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace SmartCamping.Services
{
    public sealed class LightingService : IDisposable
    {
        public LightingState State { get; private set; } = new LightingState();
        public event EventHandler<LightingState>? StateChanged;

        private readonly WinFormsTimer _tick;   
        private double _phase;                  
        private double _hue;                   

        public LightingService()
        {
            _tick = new WinFormsTimer { Interval = 60 }; 
            _tick.Tick += Tick;
            _tick.Start();
        }

        public void Apply(LightingState newState)
        {
            if (newState == null) throw new ArgumentNullException(nameof(newState));
            State = newState.Clone();

            if (State.AutoNight)
                ApplyNightSuggestionIfNeeded();

            OnChanged();
        }

        public void Toggle(bool on)
        {
            if (State.IsOn == on) return;
            State.IsOn = on;
            OnChanged();
        }

        private void Tick(object? sender, EventArgs e)
        {
            if (!State.IsOn) return;

            bool changed = false;

            switch (State.Effect)
            {
                case LightingEffect.Pulse:
                    _phase += 0.12;
                    if (_phase > Math.PI * 2) _phase = 0;
                    var mid = State.Brightness;
                    var amp = Math.Max(5, (int)(mid * 0.35));
                    var pulsed = mid + (int)(Math.Sin(_phase) * amp);
                    pulsed = Math.Max(5, Math.Min(100, pulsed));
                    if (pulsed != State.Brightness)
                    {
                        State.Brightness = pulsed;
                        changed = true;
                    }
                    break;

                case LightingEffect.ColorCycle:
                    _hue += 1.0;
                    if (_hue >= 360) _hue -= 360;
                    var c = FromHsv(_hue, 0.55, 1.0);
                    if (c.ToArgb() != State.Color.ToArgb())
                    {
                        State.Color = c;
                        changed = true;
                    }
                    break;

                default:
                    break;
            }

            if (changed) OnChanged();
        }

        private void ApplyNightSuggestionIfNeeded()
        {
            // Νύχτα = 21:00–06:00
            var now = DateTime.Now.TimeOfDay;
            bool isNight = (now >= new TimeSpan(21, 0, 0)) || (now < new TimeSpan(6, 0, 0));

            if (isNight &&
               (State.Effect == LightingEffect.Static || State.Effect == LightingEffect.Reading))
            {
                State.Effect = LightingEffect.NightLight;
                State.Color = Color.FromArgb(255, 255, 196, 140); 
                State.Brightness = Math.Min(State.Brightness, 35);
            }
        }

        private static Color FromHsv(double hue, double saturation, double value)
        {
            if (hue < 0) hue = 0; else if (hue >= 360) hue %= 360;
            saturation = Math.Max(0, Math.Min(1, saturation));
            value = Math.Max(0, Math.Min(1, value));

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            double v = value * 255;
            int vi = Convert.ToInt32(v);
            int p = Convert.ToInt32(v * (1 - saturation));
            int q = Convert.ToInt32(v * (1 - f * saturation));
            int t = Convert.ToInt32(v * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(255, vi, t, p),
                1 => Color.FromArgb(255, q, vi, p),
                2 => Color.FromArgb(255, p, vi, t),
                3 => Color.FromArgb(255, p, q, vi),
                4 => Color.FromArgb(255, t, p, vi),
                _ => Color.FromArgb(255, vi, p, q),
            };
        }

        private void OnChanged() => StateChanged?.Invoke(this, State.Clone());

        public void Dispose()
        {
            _tick.Stop();
            _tick.Tick -= Tick;
            _tick.Dispose();
        }
    }
}
