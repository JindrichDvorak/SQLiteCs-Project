using System.Data;
using System.Data.SQLite;


namespace SQLiteCs
{
    public struct QueryResult(string[] columnNames, Type[] columnDataTypes, object?[][] rows, int rowCount)
    {
        /// <summary>
        /// Pole (<c>string</c>) obsahující názvy vrácených atributů (sloupců).
        /// </summary>
        public string[] ColumnNames = columnNames;
        /// <summary>
        /// Pole (<c>Type</c>) obsahující datové typy vrácených atributů (sloupců).
        /// </summary>
        public Type[] ColumnDataTypes = columnDataTypes;
        /// <summary>
        /// Pole (<c>object?[]</c>) obsahující všechny vrácené záznamy (řádky) jako pole (<c>object?</c>).
        /// </summary>
        public object?[][] Rows = rows;
        /// <summary>
        /// Počet vrácených atributů (sloupců).
        /// </summary>
        public int ColumnCount = columnNames.Length;
        /// <summary>
        /// Počet vrácených záznamů (řádků).
        /// </summary>
        public int RowCount = rowCount;
    }

    public class Database
    {
        private string _databasePath;
        private SQLiteConnection? _connection;

        private bool _testingMode = false;
        private bool _sqlExceptions = false;

        /// <summary>
        /// Konstruktor třídy <c>Database</c>, která umožňuje interagovat s SQLite databází uvnitř C# aplikace. 
        /// Vstupním parametrem je <c>databasePath</c>, což je <c>string</c> reprezentující cestu k souboru databáze 
        /// (relativní vůči .exe souboru C# aplikace).
        /// </summary>
        /// <param name="databasePath">Relativní cesta k souboru databáze (vůči .exe souboru C# aplikace). Pokud daný soubor neexistuje, automaticky 
        /// se vytvoří nový.</param>
        public Database(string databasePath)
        {
            _databasePath = databasePath;
            ConnectDatabase(databasePath);
        }

        ~Database()
        {
            CloseDatabase();
        }

        private void ConnectDatabase(string databasePath)
        {
            _connection = new SQLiteConnection($"Data source={databasePath};foreign keys = true;");
            _connection.Open();
        }

        private void DisconnectDatabase()
        {
            if (_connection == null) return;
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// Metoda <c>CloseDatabase()</c> uzavírá spojení mezi C# programem a databází. Pokud je databáze právě v testovacím módu (který se zapíná 
        /// příkazem: <c>SetTestingMode(true);</c>), tak metoda <c>CloseDatabase()</c> po uzavření spojení i vymaže její obsah.
        /// </summary>
        public void CloseDatabase()
        {
            if (_connection != null)
            {
                DisconnectDatabase();

                if (_testingMode)
                {
                    try
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        File.Delete(_databasePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                } 
            }
        }

        private static QueryResult CreateQueryResult(DataTable dataTable)
        {
            int columnCount = dataTable.Columns.Count;
            int rowCount = dataTable.Rows.Count;
            string[] columnNames = new string[columnCount];
            Type[] columnDataTypes = new Type[columnCount];
            object?[][] rows = new object?[rowCount][];

            if (dataTable.Columns.Count == 0) return new QueryResult(columnNames, columnDataTypes, rows, rowCount);
            
            for (int i = 0; i < columnCount; i++)
            {
                DataColumn column = dataTable.Columns[i];
                columnNames[i] = column.ColumnName;
                columnDataTypes[i] = column.DataType;
            }

            for (int i = 0; i < rowCount; i++)
            {
                object?[] row = new object?[columnCount];
                for (int j = 0; j < columnCount; j++)
                {
                    row[j] = dataTable.Rows[i].ItemArray[j];
                }
                rows[i] = row;
            }

            return new QueryResult(columnNames, columnDataTypes, rows, rowCount);
        }

        /// <summary>
        /// Funkce <c>Query()</c> umožňuje C# programu pokládat dotazy připojené databázi pomocí SQL dotazů, které přijímá jako vstupní 
        /// <c>string</c> parametr a následně vrací výsledek dotazu jako objekt typu <c>QueryResult</c>. 
        /// </summary>
        /// <param name="sql">SQL dotaz.</param>
        /// <returns>Všechny výsledky položeného SQL dotazu uložené v objektu typu <c>QueryResult</c>.</returns>
        public QueryResult Query(string sql)
        {
            DataTable dataTable = new DataTable();
            
            try
            {
                SQLiteCommand command = new SQLiteCommand(sql, _connection);
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);

                adapter.Fill(dataTable);
            }
            catch (Exception ex)
            {
                if (!_sqlExceptions) Console.WriteLine($"SQL chyba: {ex.Message}\nQuery:\n{sql}\n");
                else throw;
            }

            return CreateQueryResult(dataTable);
        }

        /// <summary>
        /// Funkce <c>Scalar()</c> umožňuje C# programu pokládat dotazy připojené databázi pomocí SQL dotazů, které přijímá jako vstupní 
        /// <c>string</c> parametr a následně vrací výsledek dotazu jako obecný objekt typu <c>object?</c>. Pokud má SQL dotaz vrátit 
        /// tabulku, tak tato funkc vrátí pouze prvek dopovídající prvnímu řádku prvního sloupce této tabulky.
        /// </summary>
        /// <param name="sql">SQL dotaz.</param>
        /// <returns>Jediný výsledek položeného SQL dotazu jako <c>object?</c>.</returns>
        public object? Scalar(string sql)
        {
            try
            {
                var command = new SQLiteCommand(sql, _connection);

                return command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                if (!_sqlExceptions) Console.WriteLine($"SQL chyba: {ex.Message}\nScalar:\n{sql}\n");
                else throw;

                return null;
            }
        }

        /// <summary>
        /// Funkce <c>NonQuery()</c> umožňuje C# programu provádět na databázi SQL příkazy (modifikace databáze), které přijímá jako 
        /// vstupní <c>string</c> parametr a následně vrací informaci (jako pravdivostní hodnotu) o tom, zdali byl daný příkaz úspěšný.
        /// </summary>
        /// <param name="sql">SQL příkaz.</param>
        /// <returns>Informaci o úspěchu provedení zadaného SQL příkazu.</returns>
        public bool NonQuery(string sql)
        {
            try
            {
                SQLiteCommand command = new SQLiteCommand(sql, _connection);
                command.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                if (!_sqlExceptions) Console.WriteLine($"SQL chyba: {ex.Message}\nNonQuery:\n{sql}\n");
                else throw;

                return false;
            }
        }

        /// <summary>
        /// Funkce <c>CopyDatabase()</c> umožňuje vytvořit kopii již existující databáze pod jiným jménem. Funkce přijímá dva vstupní parametry, 
        /// které reprezentují cestu k souboru databáze, kterou chceme zkopírovat a cestu k novému (nebo již exisutjícímu) souboru, který má 
        /// obsahovat kopii původní databáze. Obdobně jako při vytváření nové databáze, pokud zadaná cesta k nové databázi ukazuje na prozatím neexistující soubor, 
        /// tato funkce tento soubor automaticky vytvoří (stejné chování ovšem neplatí pro cestu k databázi, kterou chceme zkopírovat).
        /// </summary>
        /// <param name="databasePath">Cesta k souboru obsahujícím databázi, kterou chceme zkopírovat (tento soubor musí existovat).</param>
        /// <param name="databaseCopyPath">Cesta k souboru nové databáze, která má být kopií původní databáze (nemusí před vytvořením kopie existovat).</param>
        /// <returns>Referenci na nově vzniklou databázi.</returns>
        public static Database? CopyDatabase(string databasePath, string databaseCopyPath)
        {
            try
            {
                File.Copy(databasePath, databaseCopyPath, true);

                return new Database(databaseCopyPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{databasePath}: Při kopírování došlo k chybě:\n{ex.Message}\n");

                return null;
            }
        }

        /// <summary>
        /// Funkce <c>CopyDatabase()</c> umožňuje vytvořit kopii této databáze pod jiným jménem. Funkce přijímá vstupní parametr reprezentující cestu k nové databázi, 
        /// která má být kopií této databáze. Obdobně jako při vytváření nové databáze, pokud zadaná cesta k nové databázi ukazuje na prozatím neexistující soubor, 
        /// tato funkce tento soubor automaticky vytvoří.
        /// </summary>
        /// <param name="databaseCopyPath">Cesta k souboru nové databáze, která má být kopií této databáze (nemusí před vytvořením kopie existovat).</param>
        /// <returns>Referenci na nově vzniklou databázi.</returns>
        public Database? CopyDatabase(string databaseCopyPath)
        {
            try
            {
                File.Copy(_databasePath, databaseCopyPath, true);

                return new Database(databaseCopyPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_databasePath}: Při kopírování došlo k chybě:\n{ex.Message}\n");

                return null;
            }
        }

        /// <summary>
        /// Funkce <c>ClearDatabase()</c> vymaže obsah této databáze, ale zachová otevřené spojení mezi C# programem a nově promazanou databází.
        /// </summary>
        /// <returns>Informaci o tom, zdali bylo promazání obsahu databáze úspěšné.</returns>
        public bool ClearDatabase()
        {
            if (!_testingMode)
            {
                Console.WriteLine($"{_databasePath}: Vymazat obsah je možné pouze u databází, které jsou v testovacím módu.");
                Console.WriteLine("Testovací mód je pro danou databázi možné zapnout member metodou: SetTestingMode(true);\n");

                return false;
            }

            try
            {
                CloseDatabase();
                ConnectDatabase(_databasePath);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_databasePath}: Při mazání obsahu došlo k chybě:\n{ex.Message}\n");

                return false;
            }
        }

        /// <summary>
        /// Metoda <c>SetTestingMode()</c> umožňuje nastavit testovací mód této databáze. Pokud je testovací mód zapnutý (<c>true</c>), pak je možné vymazat obsah databáze pomocí funkce 
        /// <c>ClearDatabase()</c>, nebo metody <c>CloseDatabase()</c>, kterou bychom měli volat pro každou databázi na konci našeho programu.
        /// </summary>
        /// <param name="enable">Pravdivostní hodnota určující, zdali má být testovací mód pro tuto databázi zapnutý, nebo vypnutý.</param>
        public void SetTestingMode(bool enable)
        { 
            _testingMode = enable;
        }

        /// <summary>
        /// Metoda <c>SetSQLExceptions()</c> umožňuje změnit reakci programu na chyby v SQL dotazech a příkazech. Pokud jako vstupní parametr této funkce zadáme hodnotu <c>true</c>, 
        /// případné SQL chyby zastaví chod programu vyvoláním výjimky. Defaultní reakcí na SQL chyby je vypsání chybové zprávy do konzole bez zastavení chodu programu.
        /// </summary>
        /// <param name="enable">Pravdivostní hodnota určující, zdali mají chyby v SQL dotazech a příkazech vyvolávat výjimky, nebo jestli má program dále pokračovat a 
        /// chyby vypsat do konzole.</param>
        public void SetSQLExceptions(bool enable)
        { 
            _sqlExceptions = enable;
        }
    }
}