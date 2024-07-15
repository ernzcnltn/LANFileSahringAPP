using System;
using System.Data.SQLite;

public class SharingDatabase
{
    private static string dbPath = "Data Source=lanfilesharing.db";

    public static void InitializeDatabase()
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();
            string createTableQuery = @"CREATE TABLE IF NOT EXISTS Users (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        Username TEXT NOT NULL,
                                        Password TEXT NOT NULL)";
            SQLiteCommand command = new SQLiteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
        }
    }

    public static bool ValidateUser(string username, string password)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();
            string query = "SELECT COUNT(1) FROM Users WHERE Username = @username AND Password = @password";
            SQLiteCommand command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
    }

    public static void AddUser(string username, string password)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();
            string insertQuery = "INSERT INTO Users (Username, Password) VALUES (@username, @password)";
            SQLiteCommand command = new SQLiteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);
            command.ExecuteNonQuery();
        }
    }
}