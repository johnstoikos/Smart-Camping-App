using System;
using System.Collections.Generic;
using System.Linq;
using SmartCamping.Models;

namespace SmartCamping.Services
{
    public sealed class EventsService
    {
        private readonly List<CampEvent> _events = new();
        public IReadOnlyList<CampEvent> Events => _events;

        public event EventHandler<IReadOnlyList<CampEvent>>? EventsChanged;
        private void RaiseChanged() => EventsChanged?.Invoke(this, _events);

        public void Clear()
        {
            _events.Clear();
            RaiseChanged();
        }

        public void Add(CampEvent ev)
        {
            if (ev.Id == Guid.Empty) ev.Id = Guid.NewGuid();
            _events.Add(ev);
            RaiseChanged();
        }

        public void Join(Guid id)
        {
            var ev = _events.FirstOrDefault(x => x.Id == id);
            if (ev == null) return;
            if (ev.Capacity is int cap && ev.Registered >= cap) return;
            ev.Registered++;
            RaiseChanged();
        }

        // Γέμισμα με 6 εκδηλώσεις (με εικόνες από το Assets)
        public void SeedIfEmpty(int count = 6)
        {
            if (_events.Count > 0) return;

            var d0 = DateTime.Today;

            void AddSeed(string title, int dStart, int hStart, int dEnd, int hEnd, string loc, int cap, string desc, string img)
            {
                Add(new CampEvent
                {
                    Title = title,
                    Start = d0.AddDays(dStart).AddHours(hStart),
                    End = d0.AddDays(dEnd).AddHours(hEnd),
                    Location = loc,
                    Capacity = cap,
                    Description = desc,
                    Registered = 0,
                    ImageFile = img
                });
            }

            AddSeed(
                "Sunset Yoga", 1, 18, 1, 19, "Ακτή A1", 25,
                "Χαλαρωτική απογευματινή πρακτική με θέα το ηλιοβασίλεμα. " +
                "Κατάλληλη για όλα τα επίπεδα, με ήπιες διατάσεις και αναπνοές. " +
                "Φέρε στρωματάκι, νερό και ελαφρύ ρουχισμό. Θα υπάρχει μουσική υπόκρουση χαμηλής έντασης.",
                "sunset_yoga.jpg"
            );

            AddSeed(
                "BBQ Night", 2, 20, 2, 23, "Κεντρική Πλατεία", 80,
                "Βραδιά ψησίματος για όλο το κάμπινγκ! Κλασικές και vegan επιλογές, " +
                "μπύρες/αναψυκτικά και ζωντανή μουσική στο τέλος. " +
                "Δήλωσε αν χρειάζεσαι ειδικές διατροφικές επιλογές.",
                "bbq.jpg"
            );

            AddSeed(
                "Πεζοπορία στο φαράγγι", 3, 9, 3, 13, "Αφετηρία: Reception", 30,
                "Μονοπάτι μέτριας δυσκολίας με σκιά και εντυπωσιακή θέα. " +
                "Στάση για ξεκούραση και μπάνιο στο ρέμα, συνοδεία έμπειρου οδηγού. " +
                "Απαιτούνται κλειστά παπούτσια, καπέλο και νερό.",
                "canyon.jpg"
            );

            AddSeed(
                "Movie Night", 1, 21, 1, 23, "Υπαίθριο Σινεμά", 60,
                "Προβολή οικογενειακής ταινίας στον προτζέκτορα. " +
                "Παρέχονται καρέκλες, αλλά προτείνεται κουβέρτα/μαξιλάρι. " +
                "Ποπ-κορν & λεμονάδα διαθέσιμα στο κιόσκι.",
                "movie_night.jpg"
            );

            AddSeed(
                "Beach Volleyball 4x4", 2, 16, 2, 19, "Παραλία B", 32,
                "Φιλικό τουρνουά 4x4 με σύστημα ομίλων. " +
                "Δήλωσε ομάδα ή έλα μόνος/η και θα σε βάλουμε σε μία. " +
                "Παρέχονται νερά και βασικός εξοπλισμός.",
                "beach_volley.jpg"
            );

            AddSeed(
                "Stargazing – Αστρονομία", 4, 22, 4, 23, "Λόφος Πεύκων", 40,
                "Παρατήρηση νυχτερινού ουρανού με τηλεσκόπια και σύντομη εισαγωγή από ερασιτέχνη αστρονόμο. " +
                "Μαθαίνουμε να εντοπίζουμε αστερισμούς και πλανήτες. " +
                "Καλό να έχετε ένα ελαφρύ μπουφάν.",
                "stargazing.jpg"
            );
        }
    }
}
