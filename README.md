# KonyvtarWpf – EF Core code-first, WPF CRUD (2 entitás, 1 : N), migráció nélkül

.NET 10, EF Core 10 – Visual Studio 2026 (18.x) szükséges hozzá. Megnyitás: `KonyvtarWpf.slnx`.

## Fájlok (a solution és a projekt egy mappában)

| Fájl | Típus | Tartalom |
|---|---|---|
| `Szerzo.cs` | `Szerző` | „Egy” oldal: `Id`, `Név`, `SzületésiÉv`, `Nemzetiség`, navigáció: `Könyvek` |
| `Konyv.cs` | `Könyv` | „Több” oldal: `Id`, `Cím`, `KiadásÉve`, `Oldalszám`, idegen kulcs `SzerzőId`, navigáció: `Szerző` |
| `cnKonyvtar.cs` | `cnKönyvtár` | DbContext; teljes leképezés Fluent API-val; kapcsolati sztring az `appsettings.json`-ból; leválasztás |
| `appsettings.json` | – | A `csKonyvtar` kapcsolati sztring (`|DataDirectory|\KonyvtarDB.mdf`) |
| `wndFoablak.xaml` | `wndFőablak` | Menü, listák, `MindentElrejt()`; a konstruktorban táblák létrehozása + `KezdőAdatok()`, bezáráskor leválasztás |
| `ucSzerzo.xaml` | `ucSzerző` | UserControl szerző felviteléhez / módosításához; a főablakban egy példány (`ucSzerzőŰrlap`), `Megjelenítés(int?)` jeleníti meg; mentés és Mégse után maga tér vissza a listához |
| `ucKonyv.xaml` | `ucKönyv` | UserControl könyv felviteléhez / módosításához; a főablakban egy példány (`ucKönyvŰrlap`), `Megjelenítés(int?)` jeleníti meg; mentés és Mégse után maga tér vissza a listához |
| `App.xaml` | `App` | Csak `StartupUri` és stílusok, kód nélkül |

### Elnevezési előtagok

`wnd` Window · `uc` UserControl · `cn` DbContext · `mi` MenuItem · `dg` DataGrid ·
`tb` TextBox · `tbl` TextBlock · `lb` Label · `cb` ComboBox · `bt` Button · `db` adatbázis

A listák `ObservableCollection<T>` típusúak, így egy esetleges adatkötés automatikusan követi a változásokat.

## Az üres adatbázisfájl létrehozása (egyszer)

A Visual Studio **Add → New Item → Service-based Database** sablonja .NET 5+ (SDK-stílusú)
projektben nem működik („Microsoft.Data.SqlClient is not supported on this platform”), ezért:

1. Futtasd a solution mappájában lévő **`UresAdatbazis.cmd`** fájlt (dupla kattintás).
   Ez a LocalDB-vel létrehozza a projekt mappájában a `KonyvtarDB.mdf` és `KonyvtarDB_log.ldf` fájlt,
   majd rögtön leválasztja, így nem marad regisztrálva és zárolva.
2. A fájl magától megjelenik a Solution Explorerben, és a `.csproj` miatt a
   *Copy to Output Directory* tulajdonsága már **Copy if newer**.

Kézi alternatíva Visual Studióban: **View → SQL Server Object Explorer** → `(localdb)\MSSQLLocalDB`
→ jobb klikk a *Databases*-en → **New Query**, és futtasd (az elérési utat írd át a sajátodra):

```sql
CREATE DATABASE KonyvtarDB_Ures
ON (NAME = N'KonyvtarDB', FILENAME = N'C:\...\KonyvtarWpf\KonyvtarDB.mdf')
LOG ON (NAME = N'KonyvtarDB_log', FILENAME = N'C:\...\KonyvtarWpf\KonyvtarDB_log.ldf');
GO
EXEC sp_detach_db @dbname = N'KonyvtarDB_Ures';
```

## Futtatás

**F5**. Első indításkor a program az üres adatbázisban az entitások alapján létrehozza a
`Szerzők` és `Könyvek` táblát (`EnsureCreated`, migráció nélkül), majd a `wndFőablak.KezdőAdatok()`
feltölti a kezdő adatokat, mert az adatbázis üres. Az ablak bezárásakor (`Closed`) a program leválasztja (detach)
az adatbázist a LocalDB-ről.

A „copy if newer” miatt a bin-beli adatbázis megmarad buildről buildre (a projektbeli üres fájl
régebbi, ezért nem írja felül). Ha újra üres adatbázissal akarsz indulni: **Build → Clean Solution**.

## Miért nincs szükség `IDesignTimeDbContextFactory`-ra?

A factory csak az EF tervezési idejű eszközeinek (`Add-Migration`, `Update-Database`) kellett.
Migráció nélkül ezeket nem használjuk: a táblákat a program maga hozza létre futáskor, és a
`cnKönyvtár` paraméter nélküli konstruktora a kapcsolati sztringet az `appsettings.json`-ból olvassa.
