# SQLiteCs-Project

> Řešení praktických úloh v `C#`, které vyžadují interakci s `SQLite` databázemi.

## Základní architektura

Tento repozitář obsahuje `Visual Studio` řešení `SQLiteCs-Project`, které dále obsahuje další dva projekty:

- `SQLiteCs` - Statická knihovna, která definuje jednoduchou abstrakci složitějších procesů zajišťujících interakci s `SQLite` databázemi pomocí `C#` příkazů. Tato knihovna má dvě hlavní závislosti (které se **automaticky nainstalují** po spuštění první kompilace pomocí package manageru `NuGet`):
  - `System.Data.SQLite` - Propojení `SQLite` databáze a `C#`.
  - `SQLitePCLRaw.bundle_e_sqlite3` - Implementace `SQLite` databáze.
- `ProjectApp` - Konzolová aplikace, která se odkazuje na statickou knihovnu `SQLiteCs` a obsahuje následující dva soubory:
  - `Program.cs` - Obsahuje `Main` metodu, jedná se tedy o vstupní bod celé aplikace. 
  - `csfd.db` - Příkladová databáze obsahující jedinou tabulku `movies`, ve které jsou uloženy záznamy o 100 nejlépe hodnocených filmech podle ČSFD. Tato tabulka obsahuje následující atributy:
    - `id` - ID záznamu.
    - `title` - Název filmu.
    - `year` - Rok vydání filmu.
    - `country` - Země původu.
    - `genre` - Žánr / žánry.
    - `director` - Režisér / režiséři.
    - `actors` - První dva uvedení herci.
    - `rating` - Hodnocení.
    - `num_of_ratings` - Počet hodnocení.

## Základní použití

Pro úspěšné řešení všech připravených úloh není potřeba nijak zasahovat do zdrojového kódu knihovny `SQLiteCs`. Veškerá logika naší aplikace bude obsažena ve zdrojových souborech konzolové aplikace `ProjectApp`.

Při práci s databázemi obecně sledujeme následující kroky:

- Otevření databáze.
- Interakce s databází.
- Uzavření databáze.

### Otevření databáze

Knihovna `SQLiteCs` pracuje s databázemi, které jsou uloženy lokálně na disku. Proto je potřeba specifikovat takzvanou cestu (`path`) k umístění databáze, aby mohlo vzniknout spojení mezi danou databází a naším `C#` programem. Například, pro otevření databáze `csfd.db` použijeme příkaz:
```cs
Database db = new Database("csfd.db");
```
Skrze nově vytvořený objekt `db` je následně možné s databází `csfd.db` interagovat (čemuž se věnuje následující sekce). 

Pokud bychom chtěli vytvořit kompletně novou databázi, použijeme stejný příkaz, ale specifikujeme jinou cestu, která zároveň udává i název souboru, který bude novou databázi obsahovat (v tomto případě `newDatabase.db`):
```cs
Database db = new Database("newDatabase.db");
```
Tento příkaz automaticky vytvoří nový soubor s názvem `newDatabase.db` ve stejné složce, ze které se spouští náš program -- používali jsme tedy takzvané **relativní cesty**.

Knihovna `SQLiteCs` umožňuje otevřít databázi i v "libovolném" adresáři. Za tímto účelem bychom použili takzvanou **absolutní cestu**. Například, pokud se databáze `csfd.db` nachází přímo na disku `C`, otevřeme ji následujícím příkazem (protože symbolem `\` začínají takzvané escape sekvence, musíme místo `\` psát `\\`):
```cs
Database db = new Database("C:\\csfd.db");
```
Tímto způsobem je možné otevřít (nebo vytvořit) "libovolné" množství **různých** databází. 

`SQL` příkazy modifikují trvalou paměť (přepisují data v souboru na disku), a proto při jejich používání vzniká riziko, že nějaký jejich nezamýšlený vedlejší efekt nenávratně přepíše databázi, což 