using SQLiteCs;


namespace ProjectApp;

internal class Program
{
    static void Main(string[] args)
    {
        Database db = new Database("test.db");
        db.SetTestingMode(true);

        db.NonQuery(@"
            CREATE TABLE IF NOT EXISTS user (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            );
        ");

        db.NonQuery(@"
            INSERT INTO user (name)
            VALUES ('Jan Dolezal'),
                ('Jindrich Dvorak'),
                ('Jakub Dvorak');
        ");

        PrintQueryResult(db, "SELECT * FROM user;");

        db.CloseDatabase();
    }

    public static void PrintQueryResult(Database database, string sqlQuery)
    {
        QueryResult data = database.Query(sqlQuery);

        foreach (string colName in data.ColumnNames)
        {
            Console.Write($"{colName,-30} ");
        }
        Console.WriteLine();
        Console.WriteLine(new string('-', data.ColumnCount * 32));

        for(int i = 0; i < data.RowCount; i++)
        {
            for (int j = 0; j < data.ColumnCount; j++)
            {
                Console.Write($"{data.Rows[i][j], -30} ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}
