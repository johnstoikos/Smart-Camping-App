using System;
using System.Collections.Generic;
using System.Drawing;

namespace SmartCamping.Models
{
    public enum WeatherCondition { Clear, Cloudy, Windy, Rain, Storm }

    public sealed class WeatherSnapshot
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public double TempC { get; set; }
        public int HumidityPct { get; set; }
        public double WindKmh { get; set; }
        public double WindDirDeg { get; set; }  // 0=N, 90=E, ...
        public WeatherCondition Condition { get; set; }

        public WeatherSnapshot Clone() => (WeatherSnapshot)MemberwiseClone();
    }

    public sealed class WeatherState
    {
        public WeatherSnapshot Now { get; set; } = new WeatherSnapshot();
        public Queue<WeatherSnapshot> History { get; } = new Queue<WeatherSnapshot>();

        public WeatherState Clone()
        {
            var s = new WeatherState { Now = Now.Clone() };
            foreach (var h in History) s.History.Enqueue(h.Clone());
            return s;
        }
    }

    public static class WeatherTheme
    {
        public static readonly Color Teal = ColorTranslator.FromHtml("#107A65");
        public static readonly Color TealDark = ColorTranslator.FromHtml("#0F5B4B");
        public static readonly Color Olive = ColorTranslator.FromHtml("#849A6A");
        public static readonly Color GreyGreen = ColorTranslator.FromHtml("#6E8278");
        public static readonly Color Cream = ColorTranslator.FromHtml("#FFF3E0");
        public static readonly Color Ink = ColorTranslator.FromHtml("#1C1F22");
        public static readonly Color RainBlue = ColorTranslator.FromHtml("#4FA3D1");
        public static readonly Color StormRed = ColorTranslator.FromHtml("#D66C5C");
    }
}
