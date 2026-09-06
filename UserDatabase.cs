using Assign_2;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;

public static class UserDatabase
{
    private static readonly string ProjectRoot =
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));

    private static readonly string DbPath = Path.Combine(ProjectRoot, "users.mdf");

    private const string DatabaseName = "UserDb_Root";

    private static readonly string ConnectionString =
        $@"Server=(localdb)\mssqllocaldb;AttachDbFilename={DbPath};Database={DatabaseName};Integrated Security=True;TrustServerCertificate=True;";

    private const string MasterConnectionString =
        @"Server=(localdb)\mssqllocaldb;Database=master;Integrated Security=True;TrustServerCertificate=True;";

    static UserDatabase()
    {
        InitializeDatabase();
    }

    private static void InitializeDatabase()
    {
        using var master = new SqlConnection(MasterConnectionString);
        master.Open();

        bool isRegistered = RunScalarBool(master, $"SELECT COUNT(*) FROM sys.databases WHERE name = '{DatabaseName}';");
        bool fileExists = File.Exists(DbPath);

        if (isRegistered && !fileExists)
        {
            RunNonQuery(master, $"DROP DATABASE IF EXISTS [{DatabaseName}];");
            isRegistered = false;
        }

        if (!isRegistered && fileExists)
        {
            RunNonQuery(master, $"CREATE DATABASE [{DatabaseName}] ON (FILENAME = '{DbPath}') FOR ATTACH;");
        }
        else if (!isRegistered && !fileExists)
        {
            RunNonQuery(master, $"CREATE DATABASE [{DatabaseName}] ON PRIMARY (NAME = {DatabaseName}_Data, FILENAME = '{DbPath}');");
        }

        using var connection = OpenConnection();

        bool tableExists = RunScalarBool(connection, "SELECT OBJECT_ID('dbo.Users', 'U');", checkNotNull: true);

        if (!tableExists)
        {
            RunNonQuery(connection, @"
                CREATE TABLE dbo.Users (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Username NVARCHAR(100) UNIQUE NOT NULL,
                    PasswordHash NVARCHAR(MAX) NOT NULL,
                    Role NVARCHAR(50) NOT NULL
                );");

            SeedDefaultUsers(connection);
        }
    }

    private static void SeedDefaultUsers(SqlConnection connection)
    {
        var defaultUsers = new (string Username, string Password, string Role)[]
        {
            ("admin", "admin123", "Admin"),
            ("alex", "user123", "User"),
            ("jordan", "user123", "User"),
            ("sam", "user123", "User"),
            ("taylor", "user123", "User"),
        };

        foreach (var user in defaultUsers)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(user.Password);

            using var command = new SqlCommand(
                "INSERT INTO dbo.Users (Username, PasswordHash, Role) VALUES (@username, @hash, @role);",
                connection);

            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@hash", hash);
            command.Parameters.AddWithValue("@role", user.Role);
            command.ExecuteNonQuery();
        }
    }

    public static bool Login(string username, string password, out string role)
    {
        role = string.Empty;

        using var connection = OpenConnection();
        using var command = new SqlCommand(
            "SELECT PasswordHash, Role FROM dbo.Users WHERE Username = @username;", connection);

        command.Parameters.AddWithValue("@username", username);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return false;
        }

        string storedHash = reader.GetString(0);
        role = reader.GetString(1);
        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }

    public static bool Register(string username, string password, string role)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand(
            "INSERT INTO dbo.Users (Username, PasswordHash, Role) VALUES (@username, @hash, @role);", connection);

        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@hash", BCrypt.Net.BCrypt.HashPassword(password));
        command.Parameters.AddWithValue("@role", role);

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SqlException)
        {
            throw new Exception("Username already exists.");
        }
    }

    public static List<UserRecord> GetAllUsers()
    {
        var users = new List<UserRecord>();

        using var connection = OpenConnection();
        using var command = new SqlCommand("SELECT Id, Username, Role FROM dbo.Users ORDER BY Username;", connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new UserRecord
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Role = reader.GetString(2)
            });
        }

        return users;
    }

    public static bool UpdateUser(int id, string newUsername, string newRole)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand(
            "UPDATE dbo.Users SET Username = @username, Role = @role WHERE Id = @id;", connection);

        command.Parameters.AddWithValue("@username", newUsername);
        command.Parameters.AddWithValue("@role", newRole);
        command.Parameters.AddWithValue("@id", id);

        try
        {
            return command.ExecuteNonQuery() > 0;
        }
        catch (SqlException)
        {
            throw new Exception("Username already exists.");
        }
    }

    public static bool UpdatePassword(int id, string newPassword)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand(
            "UPDATE dbo.Users SET PasswordHash = @hash WHERE Id = @id;", connection);

        command.Parameters.AddWithValue("@hash", BCrypt.Net.BCrypt.HashPassword(newPassword));
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }

    public static bool DeleteUser(int id)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("DELETE FROM dbo.Users WHERE Id = @id;", connection);
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }

    private static SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    private static void RunNonQuery(SqlConnection connection, string sql)
    {
        using var command = new SqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    private static bool RunScalarBool(SqlConnection connection, string sql, bool checkNotNull = false)
    {
        using var command = new SqlCommand(sql, connection);
        object result = command.ExecuteScalar();

        if (checkNotNull)
        {
            return result != DBNull.Value;
        }

        return Convert.ToInt32(result) > 0;
    }
}