namespace KonyvtarWpf
{
    /// <summary>
    /// A "több" oldal: minden könyv pontosan egy szerzőhöz tartozik.
    /// A leképezést a cnKönyvtár.OnModelCreating adja meg Fluent API-val.
    /// </summary>
    public class Könyv
    {
        public int Id { get; set; }

        public string Cím { get; set; } = string.Empty;

        public int KiadásÉve { get; set; }

        public int Oldalszám { get; set; }

        // Idegen kulcs
        public int SzerzőId { get; set; }

        // Navigációs tulajdonság (N : 1).
        // Nullázható, mert csak akkor kap értéket, ha az EF betölti (Include) vagy mi beállítjuk.
        public Szerző? Szerző { get; set; }
    }
}
