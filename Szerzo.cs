using System.Collections.ObjectModel;

namespace KonyvtarWpf
{
    /// <summary>
    /// Az "egy" oldal: egy szerzőnek több könyve lehet.
    /// A leképezést a cnKönyvtár.OnModelCreating adja meg Fluent API-val.
    /// </summary>
    public class Szerző
    {
        public int Id { get; set; }

        public string Név { get; set; } = string.Empty;

        public int SzületésiÉv { get; set; }

        public string? Nemzetiség { get; set; }

        // Navigációs tulajdonság (1 : N)
        public ObservableCollection<Könyv> Könyvek { get; set; } = new ObservableCollection<Könyv>();
    }
}
