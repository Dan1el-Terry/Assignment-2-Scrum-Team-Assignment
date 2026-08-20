using System;
using Microsoft.Data.Sqlite;

public static class UserDatabase
{
    private const string ConnectionString = "Data Source=users.db";

    static UserDatabase()
    {
        InitializeDatabase();
    }

    private static void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            string commandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL
                );";

            using (var command = new SqliteCommand(commandText, connection))
            {
                command.ExecuteNonQuery();
            }

            // Seed default accounts if empty
            string checkText = "SELECT COUNT(1) FROM Users;";
            using (var checkCommand = new SqliteCommand(checkText, connection))
            {
                if ((long)checkCommand.ExecuteScalar() == 0)
                {
                    string adminHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                    string UserHash = BCrypt.Net.BCrypt.HashPassword("User123");

                    string insertText = @"
                        INSERT INTO Users (Username, PasswordHash, Role) VALUES ('admin', @adminHash, 'Admin');
                        INSERT INTO Users (Username, PasswordHash, Role) VALUES ('User', @UserHash, 'User');";

                    using (var insertCommand = new SqliteCommand(insertText, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@adminHash", adminHash);
                        insertCommand.Parameters.AddWithValue("@UserHash", UserHash);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }

    public static bool Login(string username, string password, out string role)
    {
        role = string.Empty;    

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string query = "SELECT PasswordHash, Role FROM Users WHERE Username = @username;";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string storedHash = reader.GetString(0);
                        role = reader.GetString(1);
                        return BCrypt.Net.BCrypt.Verify(password, storedHash);
                    }
                }
            }
        }
        return false;
    }
}