using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SmartCamping.Models;

namespace SmartCamping.Services
{
    
    public sealed class NavigationService
    {
        public NavigationState State { get; private set; }

        public event EventHandler<NavigationState>? StateChanged;

        // Γνωστά σημεία 
        public readonly MapPoint StartA1 = new("A1", "Basecamp A1", 0.60f, 0.66f);
        public readonly MapPoint S1 = new("S1", "Forest Ridge", 0.53f, 0.26f);
        public readonly MapPoint S2 = new("S2", "North Gate", 0.10f, 0.22f);
        public readonly MapPoint S3 = new("S3", "East Dunes", 0.72f, 0.54f); // πιο μέσα από θάλασσα

        
        private readonly Dictionary<(string dest, RoutePreference pref), List<PointF>> _routes = new();

        public NavigationService()
        {
            State = new NavigationState
            {
                Start = StartA1,
                Destination = S1,
                Preference = RoutePreference.Recommended,
                ShowRoutes = true,
                ShowPins = true,
                ShowHazards = true
            };

            // Helper
            static PointF N(float x, float y) => new(x, y);

            // Προς S1
            _routes[(S1.Id, RoutePreference.Recommended)] = new()
            {
                N(0.60f,0.66f), N(0.58f,0.62f), N(0.55f,0.58f), N(0.51f,0.52f), N(0.49f,0.47f),
                N(0.50f,0.40f), N(0.52f,0.34f), N(0.53f,0.29f), N(0.53f,0.26f)
            };
            _routes[(S1.Id, RoutePreference.Balanced)] = new()
            {
                N(0.60f,0.66f), N(0.56f,0.62f), N(0.52f,0.58f), N(0.48f,0.56f), N(0.47f,0.50f),
                N(0.48f,0.44f), N(0.50f,0.38f), N(0.52f,0.31f), N(0.53f,0.26f)
            };
            _routes[(S1.Id, RoutePreference.Fastest)] = new()
            {
                N(0.60f,0.66f), N(0.63f,0.66f), N(0.69f,0.63f), N(0.73f,0.60f), N(0.76f,0.56f),
                N(0.72f,0.52f), N(0.66f,0.46f), N(0.60f,0.40f), N(0.56f,0.33f), N(0.53f,0.26f)
            };

            // Προς S2 
            _routes[(S2.Id, RoutePreference.Recommended)] = new()
            { N(0.60f,0.66f), N(0.50f,0.60f), N(0.40f,0.52f), N(0.28f,0.40f), N(0.18f,0.30f), N(0.10f,0.22f) };
            _routes[(S2.Id, RoutePreference.Balanced)] = new()
            { N(0.60f,0.66f), N(0.54f,0.56f), N(0.46f,0.48f), N(0.34f,0.38f), N(0.22f,0.30f), N(0.10f,0.22f) };
            _routes[(S2.Id, RoutePreference.Fastest)] = new()
            { N(0.60f,0.66f), N(0.48f,0.60f), N(0.32f,0.48f), N(0.18f,0.34f), N(0.10f,0.22f) };

            // Προς S3
            _routes[(S3.Id, RoutePreference.Recommended)] = new()
            { N(0.60f,0.66f), N(0.63f,0.62f), N(0.66f,0.58f), N(0.69f,0.56f), N(0.72f,0.54f) };
            _routes[(S3.Id, RoutePreference.Balanced)] = new()
            { N(0.60f,0.66f), N(0.62f,0.60f), N(0.66f,0.56f), N(0.70f,0.54f), N(0.72f,0.54f) };
            _routes[(S3.Id, RoutePreference.Fastest)] = new()
            { N(0.60f,0.66f), N(0.66f,0.60f), N(0.70f,0.56f), N(0.72f,0.54f) };

            RecomputePath(mapSize: new Size(1600, 1067)); 
        }

        public void SetDestination(string destId, Size currentMapSize)
        {
            State.Destination = destId.ToUpperInvariant() switch
            {
                "S1" => S1,
                "S2" => S2,
                "S3" => S3,
                _ => State.Destination
            };
            RecomputePath(currentMapSize);
        }

        public void SetPreference(RoutePreference pref, Size currentMapSize)
        {
            State.Preference = pref;
            RecomputePath(currentMapSize);
        }

        public void ToggleOverlays(bool? showRoutes = null, bool? showPins = null, bool? showHazards = null)
        {
            if (showRoutes.HasValue) State.ShowRoutes = showRoutes.Value;
            if (showPins.HasValue) State.ShowPins = showPins.Value;
            if (showHazards.HasValue) State.ShowHazards = showHazards.Value;
            OnChanged();
        }

        public void RescalePath(Size newMapSize) => RecomputePath(newMapSize);

        private void RecomputePath(Size mapSize)
        {
            var key = (State.Destination.Id, State.Preference);
            if (_routes.TryGetValue(key, out var norm))
            {
                State.ActivePathPixels = norm
                    .Select(p => new PointF(p.X * mapSize.Width, p.Y * mapSize.Height))
                    .ToList();

                double len = 0;
                for (int i = 1; i < norm.Count; i++)
                {
                    var a = norm[i - 1];
                    var b = norm[i];
                    var dx = (b.X - a.X) * mapSize.Width;
                    var dy = (b.Y - a.Y) * mapSize.Height;
                    len += Math.Sqrt(dx * dx + dy * dy); // pixels
                }
                var meters = len / 2.0;
                State.EstimatedDistanceM = meters;
                var seconds = meters / 1.2; // 1.2 m/s
                State.EstimatedTime = TimeSpan.FromSeconds(seconds);

                State.Guidance = BuildGuidance(norm);
            }
            OnChanged();
        }

        private static string[] BuildGuidance(List<PointF> normPath)
        {
            var list = new List<string> { "Ξεκίνησε προς την έξοδο του κάμπινγκ." };
            for (int i = 1; i < normPath.Count - 1; i++)
            {
                var a = normPath[i - 1];
                var b = normPath[i];
                var c = normPath[i + 1];

                var ang1 = Math.Atan2(b.Y - a.Y, b.X - a.X);
                var ang2 = Math.Atan2(c.Y - b.Y, c.X - b.X);
                var d = ang2 - ang1;
                while (d > Math.PI) d -= 2 * Math.PI;
                while (d < -Math.PI) d += 2 * Math.PI;

                var turn = d switch
                {
                    > 0.35 => "Στρίψε αριστερά",
                    < -0.35 => "Στρίψε δεξιά",
                    _ => "Συνέχισε ευθεία"
                };
                list.Add($"{turn} στο επόμενο μονοπάτι.");
            }
            list.Add("Έφτασες στο καταφύγιο!");
            return list.ToArray();
        }

        private void OnChanged() => StateChanged?.Invoke(this, State.Clone());
    }
}
