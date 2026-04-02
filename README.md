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

- 1: Otevření databáze.
- 2: Interakce s databází.
- 3: Uzavření databáze.

### 1: Otevření databáze

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

Knihovna `SQLiteCs` umožňuje otevřít databázi i v "libovolném" adresáři. Za tímto účelem bychom použili takzvanou **absolutní cestu**. Například, pokud se databáze `csfd.db` nachází přímo na disku `C`, otevřeme ji následujícím příkazem (protože symbolem `\` začínají takzvané *escape sekvence*, musíme místo `\` psát `\\`):
```cs
Database db = new Database("C:\\csfd.db");
```
Tímto způsobem je možné otevřít (nebo vytvořit) "libovolné" množství **různých** databází. 

#### Kopírování databází a testovací mód

`SQL` příkazy modifikují trvalou paměť (přepisují data v souboru na disku), a proto je nemůžeme "jen tak" opakovat (jako u "klasických" programů, které pracují čistě s operační pamětí), čemuž je velmi těžké se vyhnout, když se teprve učíme s `SQL` příkazy pracovat. `SQLiteCs` proto umožňuje kopírovat databáze a nastavit u nich takzvaný *testovací mód*, který dále umožňuje databázi smazat. 

Chceme-li si například vyzkoušet nějaké `SQL` příkazy na databázi `csfd.db` bez rizika "poničení" databáze, provedeme následující kroky:

- a: Vytvoříme kopii databáze `csfd.db` pod názvem (například) `test.db`.
- b: U databáze `test.db` nastavíme testovací mód.
- c: Na databázi `test.db` provedeme určité `SQL` příkazy a dotazy.

Tyto kroky pomocí knihovny `SQLiteCs` realizujeme následujícím způsobem:
```cs
Database test = Database.CopyDatabase("csfd.db", "test.db"); // a

test.SetTestingMode(true);                                   // b

test.NonQuery("my NonQuery");                                // c
test.Query("my Query");                                      // c
```
Testovací mód umožní smazat obsah databáze příkazem: `test.ClearDatabase();`, který nám umožní sekvenčně testovat více `SQL` příkazů (které nemusí být navzájem kompatibilní) při jediném spuštění aplikace:
```cs
test.NonQuery("my NonQuery 1");
test.Query("my Query 1");

test.ClearDatabase();

test.NonQuery("my NonQuery 2");
test.Query("my Query 2");
```

### 2: Interakce s databází

S relačními databázemi obecně interagujeme pomocí takzvaných dotazů a příkazů, což jsou funkce, respektive metody, které přijímají jako vstupní parametr validní `SQL` kód jako `string`. Pokud zadaný `SQL` kód není validní, tak `SQLiteCs` defaultně vypíše chybovou hlášku do konzole bez zastavení programu. Chceme-li, aby `SQL` chyba zastavila chod programu vyvoláním výjimky (chybová hláška se zobrazí u relevantního řádku kódu), provedeme na konkrétní databázi (například `db`) příkaz: `db.SetSQLExceptions(true);`.

- Dotazy:
  - Dotazujeme se na **tabulku** -- `Query()`.
  - Dotazujeme se na **jedinou hodnotu** -- `Scalar()`.
- Příkazy -- `NonQuery()`.

#### `Query()`

Zadáváme `SQL` **dotaz** a získáváme objekt typu `QueryResult`. Výsledek `SQL` dotazu na databázi `db` získáme následujícím způsobem:
```cs
QueryResult res = db.Query("my Query");
```
Pak objekt `res` typu `QueryResult` reprezentuje tabulku pomocí svých proměnných:

- `ColumnNames` -- `string` pole obsahující názvy získaných sloupců (atributů).
- `Rows` -- pole obsahující `object` pole, které reprezentují jednotlivé řádky (záznamy).

| `res.ColumnNames[0]` | `res.ColumnNames[1]` | `res.ColumnNames[2]` |
| :-: | :-: | :-: |
| `res.Rows[0][0]` | `res.Rows[0][1]` | `res.Rows[0][2]` |
| `res.Rows[1][0]` | `res.Rows[1][1]` | `res.Rows[1][2]` |
| `res.Rows[2][0]` | `res.Rows[2][1]` | `res.Rows[2][2]` |

Potřebujeme-li dále pracovat s výsledky `Query()`, musíme nejprve provést `cast` na odpovídající datový typ. Pokud je například první atribut (sloupec) typu `int` (tato informace je uložena v proměnné `ColumnDataTypes` objektů typu `QueryResult`) a potřebujeme dále počítat s 1. hodnotou 1. řádku, pak použijeme následující příkaz:
```cs
int data = (int)res.Rows[0][0];
```

#### `Scalar()`

Zadáváme `SQL` **dotaz** a získáváme **jediný objekt** typu `object`. Výsledek `SQL` dotazu na databázi `db` získáme následujícím způsobem:
```cs
object res = db.Scalar("my Query");
```
Potřebujeme-li dále pracovat s výsledkem `Scalar()`, musíme nejprve provést `cast` na odpovídající datový typ. Pokud se například jedná o atribut typu `float`, pak použijeme následující příkaz:
```cs
float data = (float)res;
```

#### `NonQuery()`

Zadáváme `SQL` **příkaz**, který modifikuje databázi a získáváme informaci o tom, zdali tato modifikace byla úspěšná (jako `bool`). Databázi `db` modifikujeme následujícím způsobem:
```cs
db.NonQuery("my NonQuery");
```

### 3: Uzavření databáze

Jakmile dokončíme svou práci s danou databází, tak bychom ji měli takzvaně uzavřít, tedy přerušit spojení mezi samotnou databází a `C#` programem, čehož pro databázi `db` dosáhneme příkazem:
```cs
db.CloseDatabase();
```
Pokud jsme dříve u této databáze nastavili testovací mód (pomocí příkazu: `db.SetTestingMode(true);`), tak se po uzavření databáze **rovnou vymaže i soubor, který ji obsahuje**.