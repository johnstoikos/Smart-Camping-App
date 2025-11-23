namespace SmartCamping.Models
{
    public class PegPlacementConfig
    {
        public int Angle { get; set; }
        public int Pressure { get; set; }
        public int Tension { get; set; }
        public string PegType { get; set; } = "";
        public int Score { get; set; } 
    }
}
