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

            //Drop old table so changes apply every time you run
            string dropText = "DROP TABLE IF EXISTS Users;";
            using (var dropCommand = new SqliteCommand(dropText, connection))
            {
                dropCommand.ExecuteNonQuery();
            }

            //Create fresh table
            string createText = @"
                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL
                );";

            using (var createCommand = new SqliteCommand(createText, connection))
            {
                createCommand.ExecuteNonQuery();
            }

            //Seed updated accounts
            string adminHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            string userHash = BCrypt.Net.BCrypt.HashPassword("user123");

            string insertText = @"
                INSERT INTO Users (Username, PasswordHash, Role) VALUES ('admin', @adminHash, 'Admin');
                INSERT INTO Users (Username, PasswordHash, Role) VALUES ('user', @userHash, 'User');";

            using (var insertCommand = new SqliteCommand(insertText, connection))
            {
                insertCommand.Parameters.AddWithValue("@adminHash", adminHash);
                insertCommand.Parameters.AddWithValue("@userHash", userHash);
                insertCommand.ExecuteNonQuery();
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

    public static bool Register(string username, string password, string role)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            string query = "INSERT INTO Users (Username, PasswordHash, Role) VALUES (@username, @passwordHash, @role);";
            using (var command = new SqliteCommand(query, connection))
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);
                command.Parameters.AddWithValue("@role", role);
                try
                {
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // Unique constraint violation
                {
                    throw new Exception("Username already exists.");
                }
            }
        }
    }
}