using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WPF_EFC_CF_Konyvtar
{
    /// <summary>
    /// Szerző felvitele és módosítása. A főablak XAML-jében egyetlen példány van belőle.
    /// Minden, ami az űrlappal kapcsolatos (megjelenítés, betöltés, mentés, visszatérés
    /// a listához), itt történik.
    /// </summary>
    public partial class ucSzerző : UserControl
    {
        private int? szerzőId;

        public ucSzerző()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Az űrlap megjelenítése: null esetén üres űrlap (új szerző),
        /// egyébként a megadott azonosítójú szerző adatai (módosítás).
        /// </summary>
        public void Megjelenítés(int? szerzőId)
        {
            var wndSzülő = (wndFőablak)Window.GetWindow(this);

            this.szerzőId = szerzőId;

            tbNév.Text = string.Empty;
            tbSzületésiÉv.Text = string.Empty;
            tbNemzetiség.Text = string.Empty;
            tblHiba.Text = string.Empty;
            tblHiba.Visibility = Visibility.Collapsed;

            if (szerzőId != null)
            {
                using var db = new cnKönyvtár();
                var szerző = db.Szerzők.Find(szerzőId);
                if (szerző != null)
                {
                    tbNév.Text = szerző.Név;
                    tbSzületésiÉv.Text = szerző.SzületésiÉv.ToString();
                    tbNemzetiség.Text = szerző.Nemzetiség ?? string.Empty;
                }
            }

            wndSzülő.MindentElrejt();
            Visibility = Visibility.Visible;

            wndSzülő.tblFejléc.Text = szerzőId == null ? "Új szerző" : "Szerző módosítása";
            wndSzülő.tblÁllapot.Text = "Adatbevitel...";

            // A fókusz beállítása azután, hogy az űrlap láthatóvá vált és elrendeződött
            Dispatcher.InvokeAsync(() => tbNév.Focus(), DispatcherPriority.Input);
        }

        private void btMentés_Click(object sender, RoutedEventArgs e)
        {
            // --- Ellenőrzés ---
            string név = tbNév.Text.Trim();
            if (név.Length == 0)
            {
                HibaMutatása("A név megadása kötelező.", tbNév);
                return;
            }

            if (!int.TryParse(tbSzületésiÉv.Text.Trim(), out int születésiÉv)
                || születésiÉv < 1000 || születésiÉv > DateTime.Now.Year)
            {
                HibaMutatása($"A születési év 1000 és {DateTime.Now.Year} közötti egész szám legyen.", tbSzületésiÉv);
                return;
            }

            string? nemzetiség = string.IsNullOrWhiteSpace(tbNemzetiség.Text) ? null : tbNemzetiség.Text.Trim();

            // --- Mentés ---
            int mentettId;
            try
            {
                using var db = new cnKönyvtár();
                Szerző? szerző;

                if (szerzőId == null)
                {
                    szerző = new Szerző();
                    db.Szerzők.Add(szerző);               // Create
                }
                else
                {
                    szerző = db.Szerzők.Find(szerzőId);  // Update
                    if (szerző == null)
                    {
                        HibaMutatása("A szerzőt időközben törölték.", tbNév);
                        return;
                    }
                }

                szerző.Név = név;
                szerző.SzületésiÉv = születésiÉv;
                szerző.Nemzetiség = nemzetiség;
                db.SaveChanges();

                mentettId = szerző.Id;
            }
            catch (Exception ex)
            {
                HibaMutatása("A mentés nem sikerült: " + (ex.InnerException?.Message ?? ex.Message), tbNév);
                return;
            }

            // --- Vissza a listához, a mentett szerző kijelölésével ---
            var wndSzülő = (wndFőablak)Window.GetWindow(this);
            wndSzülő.miSzerzőLista.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            wndSzülő.dgSzerzők.SelectedItem =
                ((ObservableCollection<Szerző>)wndSzülő.dgSzerzők.ItemsSource).FirstOrDefault(s => s.Id == mentettId);
        }

        private void btMégse_Click(object sender, RoutedEventArgs e)
        {
            // Vissza a listához (a Szerzők / Listázás menüpont kattintásának kiváltásával)
            var wndSzülő = (wndFőablak)Window.GetWindow(this);
            wndSzülő.miSzerzőLista.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        private void HibaMutatása(string üzenet, Control fókusz)
        {
            tblHiba.Text = üzenet;
            tblHiba.Visibility = Visibility.Visible;
            fókusz.Focus();
        }
    }
}
