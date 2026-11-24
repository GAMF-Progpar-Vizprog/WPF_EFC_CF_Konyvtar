using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KonyvtarWpf
{
    public class cnKönyvtár : DbContext
    {
        /// <summary>
        /// A kapcsolati sztring az appsettings.json fájlból (a program könyvtárából).
        /// </summary>
        public static string KapcsolatiSztring { get; }

        static cnKönyvtár()
        {
            // A |DataDirectory| a program (bin\...) könyvtárára mutasson,
            // ahová a "copy if newer" az adatbázisfájlt másolja.
            AppDomain.CurrentDomain.SetData("DataDirectory", AppContext.BaseDirectory);

            IConfiguration beállítások = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            KapcsolatiSztring = beállítások.GetConnectionString("csKonyvtar")
                ?? throw new InvalidOperationException(
                    "Az appsettings.json nem tartalmazza a 'csKonyvtar' kapcsolati sztringet.");
        }

        // A tulajdonság csak olvasható, de az objektum, amit visszaad, módosítható
        // (Add, Remove, Find stb. használható rajta; a változásokat a SaveChanges írja az adatbázisba).
        public DbSet<Szerző> Szerzők => Set<Szerző>();
        public DbSet<Könyv> Könyvek => Set<Könyv>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(KapcsolatiSztring);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- Szerző ---
            modelBuilder.Entity<Szerző>(entitás =>
            {
                entitás.ToTable("Szerzők");
                entitás.HasKey(s => s.Id);
                entitás.Property(s => s.Id).ValueGeneratedOnAdd();
                entitás.Property(s => s.Név).IsRequired().HasMaxLength(100);
                entitás.Property(s => s.SzületésiÉv).IsRequired();
                entitás.Property(s => s.Nemzetiség).HasMaxLength(50);
            });

            // --- Könyv ---
            modelBuilder.Entity<Könyv>(entitás =>
            {
                entitás.ToTable("Könyvek");
                entitás.HasKey(k => k.Id);
                entitás.Property(k => k.Id).ValueGeneratedOnAdd();
                entitás.Property(k => k.Cím).IsRequired().HasMaxLength(200);
                entitás.Property(k => k.KiadásÉve).IsRequired();
                entitás.Property(k => k.Oldalszám).IsRequired();

                // Egy a többhöz kapcsolat: Szerző (1) -> Könyv (N), kaszkádolt törléssel
                entitás.HasOne(k => k.Szerző)
                       .WithMany(s => s.Könyvek)
                       .HasForeignKey(k => k.SzerzőId)
                       .IsRequired()
                       .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Leválasztja (detach) az adatbázisfájlt a LocalDB-ről, így az nem marad
        /// regisztrálva, és a fájl sem marad zárolva.
        /// </summary>
        public static void AdatbázisLeválasztása()
        {
            try
            {
                // 1. Az adatbázis LocalDB-beli nevének lekérdezése
                string adatbázisNév;
                using (var kapcsolat = new SqlConnection(KapcsolatiSztring))
                {
                    kapcsolat.Open();
                    adatbázisNév = kapcsolat.Database;
                }

                // 2. A kapcsolatkészletben (pool) maradt kapcsolatok lezárása
                SqlConnection.ClearAllPools();

                // 3. Leválasztás a master adatbázison keresztül
                var mesterSztring = new SqlConnectionStringBuilder(KapcsolatiSztring)
                {
                    AttachDBFilename = string.Empty,
                    InitialCatalog = "master"
                };

                using var mester = new SqlConnection(mesterSztring.ConnectionString);
                mester.Open();
                using var parancs = mester.CreateCommand();
                parancs.CommandText = """
                    IF DB_ID(@nev) IS NOT NULL
                    BEGIN
                        DECLARE @sql nvarchar(max);
                        SET @sql = N'ALTER DATABASE ' + QUOTENAME(@nev) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE';
                        EXEC (@sql);
                        SET @sql = N'ALTER DATABASE ' + QUOTENAME(@nev) + N' SET MULTI_USER';
                        EXEC (@sql);
                        EXEC sp_detach_db @dbname = @nev;
                    END
                    """;
                parancs.Parameters.AddWithValue("@nev", adatbázisNév);
                parancs.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Kilépéskor nem zavarjuk a felhasználót; legrosszabb esetben a LocalDB
                // néhány perc tétlenség után magától leáll és elengedi a fájlt.
                Debug.WriteLine("Leválasztási hiba: " + ex.Message);
            }
        }
    }
}
