using System.Data.SQLite;
using System.Data;

namespace SQLiteCs
{
    public class Database
    {
        private SQLiteConnection _connection;

        public Database(string dbPath)
        {
            _connection = new SQLiteConnection($"Data source={dbPath};foreign keys = true;");
            _connection.Open();
        }

        ~Database()
        {
            CloseDB();
        }

        public void CloseDB()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }

        public DataTable Query(string sql)
        {
            DataTable dataTable = new DataTable();

            try
            {
                SQLiteCommand command = new SQLiteCommand(sql, _connection);
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);

                adapter.Fill(dataTable);
                return dataTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nChyba: {ex.Message}\nQuery:\n{sql}\n");
            }

            return dataTable;
        }

        public object Scalar(string sql) //object -> object?
        {
            try
            {
                var command = new SQLiteCommand(sql, _connection); //using
                return command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nChyba: {ex.Message}\nScalar:\n{sql}\n");
            }
            return null;
        }

        public void NonQuery(string sql) //Možnost přidat vrácení bool
        {
            try
            {
                SQLiteCommand command = new SQLiteCommand(sql, _connection); //using
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nChyba: {ex.Message}\nNonQuery:\n{sql}\n");
            }
        }

        public void InsertUser(string jmeno, string email)
        {
            var command = new SQLiteCommand("INSERT INTO Uzivatele (jmeno, email) VALUES (@jmeno, @email);", _connection); //using
            command.Parameters.AddWithValue("@jmeno", jmeno);
            command.Parameters.AddWithValue("@email", email);
            command.ExecuteNonQuery();
        }

        /*
        data.Columns = ["id", "jmeno", "email"]
        data.Rows = [
            [1, "Anna", "anna@example.com"],
            [2, "Petr", "petr@example.com"]
        ]
        */

        public void PrintQueryResult(string sql)
        {
            var data = Query(sql);

            if (data.Columns.Count == 0) return; //Error handling

            foreach (DataColumn col in data.Columns)
            {
                Console.Write($"{col.ColumnName,-30} ");
            }
            Console.WriteLine();
            Console.WriteLine(new string('-', data.Columns.Count * 32));

            foreach (DataRow row in data.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    Console.Write($"{item,-30} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}