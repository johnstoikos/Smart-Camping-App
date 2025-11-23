using System;
using SmartCamping.Models;
using WinTimer = System.Windows.Forms.Timer; // αποφυγή σύγκρουσης με Threading.Timer

namespace SmartCamping.Services
{
    public sealed class WeatherService : IDisposable
    {
        public WeatherState State { get; private set; } = new WeatherState();
        public event EventHandler<WeatherState>? StateChanged;

        private readonly Random _rng = new Random();
        private readonly WinTimer _tick;

        private double _targetTemp = 22;
        private int _targetHum = 60;
        private double _targetWind = 8;
        private double _targetDir = 220; // ΝΔ

        public WeatherService()
        {
            State.Now.TempC = 21.5;
            State.Now.HumidityPct = 65;
            State.Now.WindKmh = 7.5;
            State.Now.WindDirDeg = 220;
            State.Now.Condition = WeatherCondition.Clear;

            _tick = new WinTimer { Interval = 1200 }; // ~1.2s
            _tick.Tick += (s, e) => Step();
            _tick.Start();
        }

        // Για TarpPlacementForm
        public float WindStrength01 => (float)Math.Max(0, Math.Min(1, State.Now.WindKmh / 40.0));
        public float WindDirDeg => (float)State.Now.WindDirDeg;

        public void Dispose()
        {
            _tick.Stop();
            _tick.Dispose();
        }

        private void Step()
        {
            if (_rng.NextDouble() < 0.08)
            {
                _targetTemp = Clamp(_targetTemp + _rng.NextDouble() * 2 - 1, 12, 34);
                _targetHum = (int)Clamp(_targetHum + _rng.Next(-8, 9), 30, 95);
                _targetWind = Clamp(_targetWind + _rng.NextDouble() * 4 - 2, 0, 40);
                _targetDir = (_targetDir + _rng.Next(-20, 21) + 360) % 360;

                if (_rng.NextDouble() < 0.06)
                {
                    _targetWind = Math.Max(_targetWind, 28);
                    _targetHum = Math.Max(_targetHum, 80);
                    State.Now.Condition = WeatherCondition.Storm;
                }
                else if (_targetWind > 18) State.Now.Condition = WeatherCondition.Windy;
                else if (_targetHum > 75) State.Now.Condition = WeatherCondition.Rain;
                else if (_targetHum > 60) State.Now.Condition = WeatherCondition.Cloudy;
                else State.Now.Condition = WeatherCondition.Clear;
            }

            // κίνηση προς τους στόχους
            State.Now.TempC = Lerp(State.Now.TempC, _targetTemp, 0.08);
            State.Now.HumidityPct = (int)Lerp(State.Now.HumidityPct, _targetHum, 0.10);
            State.Now.WindKmh = Lerp(State.Now.WindKmh, _targetWind, 0.10);
            State.Now.WindDirDeg = (State.Now.WindDirDeg * 0.9 + _targetDir * 0.1 + 360) % 360;
            State.Now.Time = DateTime.Now;

            State.History.Enqueue(State.Now.Clone());
            while (State.History.Count > 120) State.History.Dequeue();

            StateChanged?.Invoke(this, State.Clone());
        }

        public string? CurrentAdvice()
        {
            var w = State.Now.WindKmh; var c = State.Now.Condition;
            if (c == WeatherCondition.Storm || w >= 35) return "Ισχυρός άνεμος/καταιγίδα: ανάπτυξη προστατευτικών πανιών.";
            if (c == WeatherCondition.Rain || w >= 22) return "Βροχή/μεταβλητός άνεμος: προτείνεται πανί στην υπήνεμη πλευρά.";
            if (w >= 15) return "Μέτριος άνεμος: σκέψου μερική ανάπτυξη πανιών.";
            return null;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
        private static double Clamp(double v, double mn, double mx) => Math.Min(mx, Math.Max(mn, v));
    }
}
