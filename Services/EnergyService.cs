using System;
using System.Linq;
using SmartCamping.Models;
using SmartCamping.Services;
using WinFormsTimer = System.Windows.Forms.Timer;
using System.Drawing;

namespace SmartCamping.Services
{
    public sealed class EnergyService : IDisposable
    {
        public event EventHandler<EnergyState>? StateChanged;
        public EnergyState State { get; private set; } = new EnergyState();

        private readonly WinFormsTimer _tick;
        private readonly double _capacityWh = 800;     
        private double _batteryWh;                     // τρέχουσα ενέργεια
        private readonly Random _rng = new Random();

        private readonly LightingService? _lighting;

        public EnergyService(LightingService? lighting = null)
        {
            _lighting = lighting;

            _batteryWh = _capacityWh * State.BatteryPercent / 100.0;

            _tick = new WinFormsTimer { Interval = 1000 };
            _tick.Tick += (s, e) => Step(1.0);
            _tick.Start();
        }

        public void Dispose()
        {
            _tick.Stop();
            _tick.Dispose();
        }

        //   ενέργειες από UI 
        public void ToggleDevice(string name, bool on)
        {
            var d = State.Devices.FirstOrDefault(x => x.Name == name);
            if (d == null) return;
            d.IsOn = on;
            Recompute(0);
        }

        public void SetAutoSave(bool enabled)
        {
            State.AutoSave = enabled;
            Recompute(0);
        }

        public void SetAc(bool on, AcMode mode, int setpointC)
        {
            State.AcOn = on;
            State.AcMode = on ? mode : AcMode.Off;
            State.AcSetpointC = setpointC;
            Recompute(0);
        }

        public void ApplySavingNow()
        {
            ApplyAutoSavingActions(force: true);
            Recompute(0);
        }

        private void Step(double seconds)
        {
            var h = DateTime.Now.TimeOfDay.TotalHours;
            double dayFactor = Math.Clamp(Math.Sin((h - 6) / 14.0 * Math.PI), 0, 1); // 0..1
            double weather = 0.75 + 0.25 * Math.Sin(h * 0.2) + (_rng.NextDouble() - 0.5) * 0.05; // μικρές διακυμάνσεις
            int pv = (int)Math.Round(220 * dayFactor * Math.Clamp(weather, 0.6, 1.0));

            // κατανάλωση συσκευών
            int load = State.Devices.Where(d => d.IsOn).Sum(d => d.PowerW);

            // A/C
            if (State.AcOn)
            {
                int acW = State.AcMode switch
                {
                    AcMode.Cool => 320,
                    AcMode.Heat => 350,
                    AcMode.Fan => 60,
                    _ => 0
                };
                double duty = State.AcMode == AcMode.Fan ? 1.0 : 0.4;
                load += (int)(acW * duty);
            }

            // NET
            int net = pv - load;

            // ενημέρωση μπαταρίας
            _batteryWh += net * (seconds / 3600.0);
            _batteryWh = Math.Clamp(_batteryWh, 0, _capacityWh);
            State.BatteryPercent = (int)Math.Round(_batteryWh / _capacityWh * 100.0);

            // εκτίμηση ωρών
            if (net < 0)
                State.EstHoursRemaining = Math.Round(_batteryWh / (-net), 1); // Wh / W = h
            else
                State.EstHoursRemaining = double.PositiveInfinity;

            State.PvPowerW = pv;
            State.LoadPowerW = load;
            State.NetPowerW = net;

            // AutoSave actions
            if (State.AutoSave && State.BatteryPercent <= 20)
                ApplyAutoSavingActions();

            OnChanged();
        }

        private void Recompute(double seconds) => Step(seconds);

        private void ApplyAutoSavingActions(bool force = false)
        {
            string actions = "";

            // 1) A/C off αν μπαταρία χαμηλή
            if (State.AcOn)
            {
                State.AcOn = false;
                State.AcMode = AcMode.Off;
                actions += "A/C απενεργοποιήθηκε για εξοικονόμηση. ";
            }

            // 2) Μείωση φωτισμού (αν υπάρχει LightingService)
            if (_lighting != null && (_lighting.State.IsOn || force))
            {
                var st = _lighting.State.Clone();
                st.IsOn = true;
                st.Brightness = Math.Min(st.Brightness, 35);
                st.Effect = LightingEffect.NightLight;
                st.Color = Color.FromArgb(255, 255, 196, 140);
                _lighting.Apply(st);
                actions += "Φωτισμός → NightLight 35%. ";
            }

            var charger = State.Devices.FirstOrDefault(d => d.Name.Contains("Φορτιστής"));
            if (charger != null && charger.IsOn)
            {
                charger.IsOn = false;
                actions += "Φορτιστής OFF. ";
            }

            if (!string.IsNullOrWhiteSpace(actions))
                State.LastAction = actions;
        }

        private void OnChanged() => StateChanged?.Invoke(this, State.Clone());
    }
}
