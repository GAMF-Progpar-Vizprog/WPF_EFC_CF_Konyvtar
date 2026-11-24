using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace KonyvtarWpf
{
    /// <summary>
    /// Könyv felvitele és módosítása. A főablak XAML-jében egyetlen példány van belőle.
    /// Minden, ami az űrlappal kapcsolatos (megjelenítés, betöltés, mentés, visszatérés
    /// a listához), itt történik. A szerző kiválasztása legördülő listából történik (1 : N).
    /// </summary>
    public partial class ucKönyv : UserControl
    {
        private int? könyvId;

        public ucKönyv()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Az űrlap megjelenítése: null esetén üres űrlap (új könyv),
        /// egyébként a megadott azonosítójú könyv adatai (módosítás).
        /// A szerzők listáját minden alkalommal frissen tölti be.
        /// </summary>
        public void Megjelenítés(int? könyvId)
        {
            var wndSzülő = (wndFőablak)Window.GetWindow(this);

            using (var db = new cnKönyvtár())
            {
                var szerzők = new ObservableCollection<Szerző>(db.Szerzők.OrderBy(s => s.Név));
                if (szerzők.Count == 0)
                {
                    MessageBox.Show("Könyv felviteléhez előbb legalább egy szerzőt kell rögzíteni.",
                        "Könyv", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                this.könyvId = könyvId;

                tbCím.Text = string.Empty;
                cbSzerző.ItemsSource = szerzők;
                cbSzerző.SelectedIndex = -1;
                tbKiadásÉve.Text = string.Empty;
                tbOldalszám.Text = string.Empty;
                tblHiba.Text = string.Empty;
                tblHiba.Visibility = Visibility.Collapsed;

                if (könyvId != null)
                {
                    var könyv = db.Könyvek.Find(könyvId);
                    if (könyv != null)
                    {
                        tbCím.Text = könyv.Cím;
                        cbSzerző.SelectedValue = könyv.SzerzőId;
                        tbKiadásÉve.Text = könyv.KiadásÉve.ToString();
                        tbOldalszám.Text = könyv.Oldalszám.ToString();
                    }
                }
            }

            wndSzülő.MindentElrejt();
            Visibility = Visibility.Visible;

            wndSzülő.tblFejléc.Text = könyvId == null ? "Új könyv" : "Könyv módosítása";
            wndSzülő.tblÁllapot.Text = "Adatbevitel...";

            // A fókusz beállítása azután, hogy az űrlap láthatóvá vált és elrendeződött
            Dispatcher.InvokeAsync(() => tbCím.Focus(), DispatcherPriority.Input);
        }

        private void btMentés_Click(object sender, RoutedEventArgs e)
        {
            // --- Ellenőrzés ---
            string cím = tbCím.Text.Trim();
            if (cím.Length == 0)
            {
                HibaMutatása("A cím megadása kötelező.", tbCím);
                return;
            }

            if (cbSzerző.SelectedValue is not int szerzőId)
            {
                HibaMutatása("Válasszon szerzőt.", cbSzerző);
                return;
            }

            if (!int.TryParse(tbKiadásÉve.Text.Trim(), out int kiadásÉve)
                || kiadásÉve < 1450 || kiadásÉve > DateTime.Now.Year)
            {
                HibaMutatása($"A kiadás éve 1450 és {DateTime.Now.Year} közötti egész szám legyen.", tbKiadásÉve);
                return;
            }

            if (!int.TryParse(tbOldalszám.Text.Trim(), out int oldalszám) || oldalszám < 1)
            {
                HibaMutatása("Az oldalszám pozitív egész szám legyen.", tbOldalszám);
                return;
            }

            // --- Mentés ---
            int mentettId;
            try
            {
                using var db = new cnKönyvtár();
                Könyv? könyv;

                if (könyvId == null)
                {
                    könyv = new Könyv();
                    db.Könyvek.Add(könyv);               // Create
                }
                else
                {
                    könyv = db.Könyvek.Find(könyvId);   // Update
                    if (könyv == null)
                    {
                        HibaMutatása("A könyvet időközben törölték.", tbCím);
                        return;
                    }
                }

                könyv.Cím = cím;
                könyv.SzerzőId = szerzőId;
                könyv.KiadásÉve = kiadásÉve;
                könyv.Oldalszám = oldalszám;
                db.SaveChanges();

                mentettId = könyv.Id;
            }
            catch (Exception ex)
            {
                HibaMutatása("A mentés nem sikerült: " + (ex.InnerException?.Message ?? ex.Message), tbCím);
                return;
            }

            // --- Vissza a listához, a mentett könyv kijelölésével ---
            var wndSzülő = (wndFőablak)Window.GetWindow(this);
            wndSzülő.miKönyvLista.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            wndSzülő.dgKönyvek.SelectedItem =
                ((ObservableCollection<Könyv>)wndSzülő.dgKönyvek.ItemsSource).FirstOrDefault(k => k.Id == mentettId);
        }

        private void btMégse_Click(object sender, RoutedEventArgs e)
        {
            // Vissza a listához (a Könyvek / Listázás menüpont kattintásának kiváltásával)
            var wndSzülő = (wndFőablak)Window.GetWindow(this);
            wndSzülő.miKönyvLista.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        private void HibaMutatása(string üzenet, Control fókusz)
        {
            tblHiba.Text = üzenet;
            tblHiba.Visibility = Visibility.Visible;
            fókusz.Focus();
        }
    }
}
