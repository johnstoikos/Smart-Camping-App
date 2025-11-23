using System;

namespace SmartCamping.Models
{
    public sealed class CampEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Location { get; set; } = "-";
        public int? Capacity { get; set; }    
        public int Registered { get; set; }
        public string? Status { get; set; }   

        // Εικόνα   μέσα στον φάκελο Assets
        public string? ImageFile { get; set; }

        public override string ToString() => Title;
    }
}
