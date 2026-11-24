using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace KonyvtarWpf
{
    public partial class wndFőablak : Window
    {
        // Igaz, ha a program csatolta az adatbázist a LocalDB-hez (kilépéskor le kell választani)
        private bool dbCsatolva;

        public wndFőablak()
        {
            InitializeComponent();

            string adatbázisFájl = Path.Combine(AppContext.BaseDirectory, "KonyvtarDB.mdf");
            if (!File.Exists(adatbázisFájl))
            {
                MessageBox.Show("Nem található az adatbázisfájl:\n" + adatbázisFájl +
                                "\n\nHozd létre az üres KonyvtarDB.mdf fájlt az UresAdatbazis.cmd futtatásával.",
                    "Adatbázis hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            try
            {
                using (var db = new cnKönyvtár())
                {
                    // Az első kapcsolatnyitáskor csatolja a LocalDB az .mdf fájlt (AttachDbFilename);
                    // ha ez sikerült, kilépéskor le kell választani.
                    db.Database.OpenConnection();
                    dbCsatolva = true;

                    // Az üres adatbázisban az entitások alapján
                    // létrehozza a táblákat (ha már léteznek, nem csinál semmit).
                    db.Database.EnsureCreated();
                    db.Database.CloseConnection();
                }

                KezdőAdatok();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Az adatbázis nem érhető el:\n\n" + ex.Message,
                    "Adatbázis hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                if (dbCsatolva)
                {
                    cnKönyvtár.AdatbázisLeválasztása();
                }
                Environment.Exit(1);
            }

            miSzerzőLista_Click(miSzerzőLista, new RoutedEventArgs());
        }

        // ---------------------------------------------------------------
        //  Ablak bezárása
        // ---------------------------------------------------------------

        private void wndFőablak_Closed(object sender, EventArgs e)
        {
            // Kilépéskor az adatbázisfájl regisztrációjának megszüntetése a LocalDB-ben
            if (dbCsatolva)
            {
                cnKönyvtár.AdatbázisLeválasztása();
            }
        }

        // ---------------------------------------------------------------
        //  Láthatóság
        // ---------------------------------------------------------------

        /// <summary>
        /// Elrejti a két listát és a két űrlapot. Minden láthatóságváltás ezzel kezdődik,
        /// utána a hívó megjeleníti azt az elemet, amelyikre szükség van.
        /// A UserControl-ok is hívják, ezért internal.
        /// </summary>
        internal void MindentElrejt()
        {
            dgSzerzők.Visibility = Visibility.Collapsed;
            dgKönyvek.Visibility = Visibility.Collapsed;
            ucSzerzőŰrlap.Visibility = Visibility.Collapsed;
            ucKönyvŰrlap.Visibility = Visibility.Collapsed;
        }

        // ---------------------------------------------------------------
        //  Kezdő adatok – csak akkor, ha az adatbázis üres
        // ---------------------------------------------------------------

        private void KezdőAdatok()
        {
            using var db = new cnKönyvtár();
            if (db.Szerzők.Any() || db.Könyvek.Any())
            {
                return;
            }

            // A könyveket a szerző navigációs tulajdonságán keresztül adjuk hozzá,
            // így az idegen kulcsot (SzerzőId) az EF tölti ki.
            var jókai = new Szerző { Név = "Jókai Mór", SzületésiÉv = 1825, Nemzetiség = "magyar" };
            jókai.Könyvek.Add(new Könyv { Cím = "Az arany ember", KiadásÉve = 1872, Oldalszám = 480 });
            jókai.Könyvek.Add(new Könyv { Cím = "A kőszívű ember fiai", KiadásÉve = 1869, Oldalszám = 560 });

            var szabó = new Szerző { Név = "Szabó Magda", SzületésiÉv = 1917, Nemzetiség = "magyar" };
            szabó.Könyvek.Add(new Könyv { Cím = "Az ajtó", KiadásÉve = 1987, Oldalszám = 280 });
            szabó.Könyvek.Add(new Könyv { Cím = "Abigél", KiadásÉve = 1970, Oldalszám = 360 });

            var orwell = new Szerző { Név = "George Orwell", SzületésiÉv = 1903, Nemzetiség = "angol" };
            orwell.Könyvek.Add(new Könyv { Cím = "1984", KiadásÉve = 1949, Oldalszám = 328 });

            db.Szerzők.AddRange(jókai, szabó, orwell);
            db.SaveChanges();
        }

        // ---------------------------------------------------------------
        //  Fájl menü
        // ---------------------------------------------------------------

        private void miKilépés_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ---------------------------------------------------------------
        //  Szerzők menü – CRUD
        // ---------------------------------------------------------------

        private void miSzerzőLista_Click(object sender, RoutedEventArgs e)
        {
            using var db = new cnKönyvtár();
            var szerzők = new ObservableCollection<Szerző>(db.Szerzők
                .Include(s => s.Könyvek)
                .OrderBy(s => s.Név));

            dgSzerzők.ItemsSource = szerzők;

            MindentElrejt();
            dgSzerzők.Visibility = Visibility.Visible;

            tblFejléc.Text = "Szerzők";
            tblÁllapot.Text = $"{szerzők.Count} szerző";
        }

        private void miSzerzőÚj_Click(object sender, RoutedEventArgs e)
        {
            ucSzerzőŰrlap.Megjelenítés(null);
        }

        private void miSzerzőMódosítás_Click(object sender, RoutedEventArgs e)
        {
            if (dgSzerzők.Visibility != Visibility.Visible || dgSzerzők.SelectedItem is not Szerző kiválasztott)
            {
                miSzerzőLista_Click(miSzerzőLista, new RoutedEventArgs());
                MessageBox.Show("Előbb jelöljön ki egy szerzőt a listában, majd válassza újra a Módosítás menüpontot.",
                    "Módosítás", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ucSzerzőŰrlap.Megjelenítés(kiválasztott.Id);
        }

        private void miSzerzőTörlés_Click(object sender, RoutedEventArgs e)
        {
            if (dgSzerzők.Visibility != Visibility.Visible || dgSzerzők.SelectedItem is not Szerző kiválasztott)
            {
                miSzerzőLista_Click(miSzerzőLista, new RoutedEventArgs());
                MessageBox.Show("Előbb jelöljön ki egy szerzőt a listában, majd válassza újra a Törlés menüpontot.",
                    "Törlés", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string figyelmeztetés = kiválasztott.Könyvek.Count > 0
                ? $"\n\nFigyelem: a szerző {kiválasztott.Könyvek.Count} könyve is törlődik!"
                : string.Empty;

            if (MessageBox.Show($"Biztosan törli a(z) „{kiválasztott.Név}” szerzőt?{figyelmeztetés}",
                    "Törlés megerősítése", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using var db = new cnKönyvtár();
                var szerző = db.Szerzők.Find(kiválasztott.Id);
                if (szerző != null)
                {
                    db.Szerzők.Remove(szerző);   // a könyvek kaszkádolt törléssel törlődnek
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("A törlés nem sikerült:\n\n" + ex.Message, "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            miSzerzőLista_Click(miSzerzőLista, new RoutedEventArgs());
        }

        /// <summary>
        /// Igaz, ha a dupla kattintás egy adatsoron történt (nem az oszlopfejlécen,
        /// a görgetősávon vagy a lista üres részén).
        /// </summary>
        private static bool SorraKattintott(DataGrid dg, MouseButtonEventArgs e)
        {
            return e.OriginalSource is DependencyObject elem
                && ItemsControl.ContainerFromElement(dg, elem) is DataGridRow;
        }

        private void dgSzerzők_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SorraKattintott(dgSzerzők, e) && dgSzerzők.SelectedItem is Szerző)
            {
                miSzerzőMódosítás_Click(miSzerzőMódosítás, new RoutedEventArgs());
            }
        }

        // ---------------------------------------------------------------
        //  Könyvek menü – CRUD
        // ---------------------------------------------------------------

        private void miKönyvLista_Click(object sender, RoutedEventArgs e)
        {
            using var db = new cnKönyvtár();
            var könyvek = new ObservableCollection<Könyv>(db.Könyvek
                .Include(k => k.Szerző)
                .OrderBy(k => k.Cím));

            dgKönyvek.ItemsSource = könyvek;

            MindentElrejt();
            dgKönyvek.Visibility = Visibility.Visible;

            tblFejléc.Text = "Könyvek";
            tblÁllapot.Text = $"{könyvek.Count} könyv";
        }

        private void miKönyvÚj_Click(object sender, RoutedEventArgs e)
        {
            ucKönyvŰrlap.Megjelenítés(null);
        }

        private void miKönyvMódosítás_Click(object sender, RoutedEventArgs e)
        {
            if (dgKönyvek.Visibility != Visibility.Visible || dgKönyvek.SelectedItem is not Könyv kiválasztott)
            {
                miKönyvLista_Click(miKönyvLista, new RoutedEventArgs());
                MessageBox.Show("Előbb jelöljön ki egy könyvet a listában, majd válassza újra a Módosítás menüpontot.",
                    "Módosítás", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ucKönyvŰrlap.Megjelenítés(kiválasztott.Id);
        }

        private void miKönyvTörlés_Click(object sender, RoutedEventArgs e)
        {
            if (dgKönyvek.Visibility != Visibility.Visible || dgKönyvek.SelectedItem is not Könyv kiválasztott)
            {
                miKönyvLista_Click(miKönyvLista, new RoutedEventArgs());
                MessageBox.Show("Előbb jelöljön ki egy könyvet a listában, majd válassza újra a Törlés menüpontot.",
                    "Törlés", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Biztosan törli a(z) „{kiválasztott.Cím}” című könyvet?",
                    "Törlés megerősítése", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using var db = new cnKönyvtár();
                var könyv = db.Könyvek.Find(kiválasztott.Id);
                if (könyv != null)
                {
                    db.Könyvek.Remove(könyv);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("A törlés nem sikerült:\n\n" + ex.Message, "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            miKönyvLista_Click(miKönyvLista, new RoutedEventArgs());
        }

        private void dgKönyvek_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SorraKattintott(dgKönyvek, e) && dgKönyvek.SelectedItem is Könyv)
            {
                miKönyvMódosítás_Click(miKönyvMódosítás, new RoutedEventArgs());
            }
        }
    }
}
