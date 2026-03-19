using System.Data;
using System.Data.SQLite;


namespace SQLiteCs
{
    public struct QueryResult(string[] columnNames, Type[] columnDataTypes, object?[][] rows, int rowCount)
    {
        public string[] ColumnNames = columnNames;
        public Type[] ColumnDataTypes = columnDataTypes;
        public object?[][] Rows = rows;
        public int ColumnCount = columnNames.Length;
        public int RowCount = rowCount;
    }

    public class Database
    {
        private string _databasePath;
        private SQLiteConnection? _connection;

        private bool _testingMode = false;
        private bool _sqlExceptions = false;

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

        public void SetTestingMode(bool enable)
        { 
            _testingMode = enable;
        }

        public void SetSQLExceptions(bool enable)
        { 
            _sqlExceptions = enable;
        }
    }
}