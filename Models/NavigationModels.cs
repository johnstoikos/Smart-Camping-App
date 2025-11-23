using System;
using System.Collections.Generic;
using System.Drawing;

namespace SmartCamping.Models
{
    // Προτίμηση διαδρομής 
    public enum RoutePreference { Recommended, Balanced, Fastest }

    public record struct MapPoint(string Id, string Name, float X, float Y)
    {
        public PointF ToPixel(Size mapSize)
            => new PointF(X * mapSize.Width, Y * mapSize.Height);
    }

    public sealed class NavigationState
    {
        public MapPoint Start { get; set; }
        public MapPoint Destination { get; set; }
        public RoutePreference Preference { get; set; } = RoutePreference.Recommended;

        public bool ShowRoutes { get; set; } = true;
        public bool ShowPins { get; set; } = true;
        public bool ShowHazards { get; set; } = true;

        public List<PointF> ActivePathPixels { get; set; } = new();
        public string[] Guidance { get; set; } = Array.Empty<string>();
        public double EstimatedDistanceM { get; set; }
        public TimeSpan EstimatedTime { get; set; }

        public NavigationState Clone() => new NavigationState
        {
            Start = Start,
            Destination = Destination,
            Preference = Preference,
            ShowRoutes = ShowRoutes,
            ShowPins = ShowPins,
            ShowHazards = ShowHazards,
            ActivePathPixels = new List<PointF>(ActivePathPixels),
            Guidance = (string[])Guidance.Clone(),
            EstimatedDistanceM = EstimatedDistanceM,
            EstimatedTime = EstimatedTime
        };
    }

    //  helpers για το χρώμα της διαδρομής
    public static class NavTheme
    {
        public static readonly Color Teal = ColorTranslator.FromHtml("#107A65");
        public static readonly Color TealDark = ColorTranslator.FromHtml("#0F5B4B");
        public static readonly Color Olive = ColorTranslator.FromHtml("#849A6A");
        public static readonly Color GreyGreen = ColorTranslator.FromHtml("#6E8278");
        public static readonly Color Cream = ColorTranslator.FromHtml("#FFF3E0");
        public static readonly Color Ink = ColorTranslator.FromHtml("#1C1F22");
    }
}
