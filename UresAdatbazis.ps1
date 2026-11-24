# Üres LocalDB adatbázisfájl (KonyvtarDB.mdf) létrehozása a projekt könyvtárában.
# A táblákat a program első futáskor hozza létre (EnsureCreated).

$projekt = $PSScriptRoot
$mdf = Join-Path $projekt 'KonyvtarDB.mdf'
$ldf = Join-Path $projekt 'KonyvtarDB_log.ldf'

if (Test-Path $mdf) {
    Write-Host "Az adatbázisfájl már létezik: $mdf"
    exit 0
}

# A LocalDB példány elindítása (ha még nem fut)
sqllocaldb start MSSQLLocalDB | Out-Null

# Ideiglenes, egyedi név a LocalDB-ben; a fájl létrehozása után rögtön leválasztjuk
$nev = 'KonyvtarDB_' + [guid]::NewGuid().ToString('N')
$mdfSql = $mdf.Replace("'", "''")
$ldfSql = $ldf.Replace("'", "''")

$kapcsolat = New-Object System.Data.SqlClient.SqlConnection 'Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True'
try {
    $kapcsolat.Open()
    $parancs = $kapcsolat.CreateCommand()

    $parancs.CommandText = "CREATE DATABASE [$nev] ON (NAME = N'KonyvtarDB', FILENAME = N'$mdfSql') LOG ON (NAME = N'KonyvtarDB_log', FILENAME = N'$ldfSql');"
    [void]$parancs.ExecuteNonQuery()

    $parancs.CommandText = "EXEC sp_detach_db @dbname = N'$nev';"
    [void]$parancs.ExecuteNonQuery()

    Write-Host "Elkészült az üres adatbázis: $mdf"
}
catch {
    Write-Host "Hiba: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    $kapcsolat.Close()
}
