using System.Collections.Generic;
using System.Drawing;

namespace SmartCamping.Models
{
    public class TarpPlacement
    {
        public PointF Center { get; set; }
        public SizeF Size { get; set; }
        public float AngleDeg { get; set; }
    }

    public class TarpPlacementResult
    {
        public List<TarpPlacement> Items { get; } = new();
        public float WindDirDeg { get; set; }
        public float WindStrength01 { get; set; }   
        public string Advice { get; set; } = "";
    }
}
