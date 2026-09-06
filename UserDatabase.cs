using Assign_2;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
/// <summary>
/// Note if you delete/change information in the table inside the app, you can just delete the .mdf and .ldf file and reopen 
/// the app the pre created uers will pop up agian
/// </summary>
public static class UserDatabase
{
    private static readonly string ProjectRoot =
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));

    private static readonly string DbPath = Path.Combine(ProjectRoot, "users.mdf");

    private static readonly string ConnectionString =
        $@"Server=(localdb)\mssqllocaldb;AttachDbFilename={DbPath};Integrated Security=True;TrustServerCertificate=True;";

    private const string MasterConnectionString =
        @"Server=(localdb)\mssqllocaldb;Database=master;Integrated Security=True;TrustServerCertificate=True;";

    static UserDatabase()
    {
        try
        {
            InitializeDatabase();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Init Error: {ex.Message}");
            throw;
        }
    }

    private static void InitializeDatabase()
    {
        if (!File.Exists(DbPath))
        {
            CreateDatabaseFile();
        }

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        if (!UsersTableExists(connection))
        {
            CreateUsersTable(connection);
            SeedDefaultUsers(connection);
        }
    }
    //Keep this as its run by initalizeddatabase if the .mdf file is deleted
    private static void CreateDatabaseFile()
    {
        using var masterConnection = new SqlConnection(MasterConnectionString);
        masterConnection.Open();

        string createDbQuery = $@"
            CREATE DATABASE [UserDb_Root] 
            ON PRIMARY (NAME = UserDb_Root_Data, FILENAME = '{DbPath}')";

        using var command = new SqlCommand(createDbQuery, masterConnection);
        command.ExecuteNonQuery();
    }

    private static bool UsersTableExists(SqlConnection connection)
    {
        string query = "SELECT OBJECT_ID('dbo.Users', 'U');";
        using var command = new SqlCommand(query, connection);
        return command.ExecuteScalar() != DBNull.Value;
    }

    private static void CreateUsersTable(SqlConnection connection)
    {
        string createTableQuery = @"
            CREATE TABLE dbo.Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Username NVARCHAR(100) UNIQUE NOT NULL,
                PasswordHash NVARCHAR(MAX) NOT NULL,
                Role NVARCHAR(50) NOT NULL
            );";

        using var command = new SqlCommand(createTableQuery, connection);
        command.ExecuteNonQuery();
    }

    private static void SeedDefaultUsers(SqlConnection connection)
    {
        //Add new passwords make them simple to input
        string adminHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        string user1Hash = BCrypt.Net.BCrypt.HashPassword("user123");
        string user2Hash = BCrypt.Net.BCrypt.HashPassword("user123");
        string user3Hash = BCrypt.Net.BCrypt.HashPassword("user123");
        string user4Hash = BCrypt.Net.BCrypt.HashPassword("user123");

        string insertQuery = @"
            INSERT INTO dbo.Users (Username, PasswordHash, Role) VALUES 
            ('admin', @adminHash, 'Admin'),
            ('alex', @user1Hash, 'User'),
            ('jordan', @user2Hash, 'User'),
            ('sam', @user3Hash, 'User'),
            ('taylor', @user4Hash, 'User');";

        using var command = new SqlCommand(insertQuery, connection);
        command.Parameters.AddWithValue("@adminHash", adminHash);
        command.Parameters.AddWithValue("@user1Hash", user1Hash);
        command.Parameters.AddWithValue("@user2Hash", user2Hash);
        command.Parameters.AddWithValue("@user3Hash", user3Hash);
        command.Parameters.AddWithValue("@user4Hash", user4Hash);
        command.ExecuteNonQuery();
    }

    public static bool Login(string username, string password, out string role)
    {
        role = string.Empty;

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        string query = "SELECT PasswordHash, Role FROM dbo.Users WHERE Username = @username;";

        using var command = new SqlCommand(query, connection);
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
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        string query = "INSERT INTO dbo.Users (Username, PasswordHash, Role) VALUES (@username, @passwordHash, @role);";
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
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

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        string query = "SELECT Id, Username, Role FROM dbo.Users ORDER BY Username;";

        using var command = new SqlCommand(query, connection);
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
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        string query = "UPDATE dbo.Users SET Username = @username, Role = @role WHERE Id = @id;";

        using var command = new SqlCommand(query, connection);
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
    //Update password currently doesn't require an existing user need to change that
    public static bool UpdatePassword(int id, string newPassword)
    {
        string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        string query = "UPDATE dbo.Users SET PasswordHash = @passwordHash WHERE Id = @id;";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@passwordHash", newHash);
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }
    //dont know if we need DeleteUser or not
    public static bool DeleteUser(int id)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        string query = "DELETE FROM dbo.Users WHERE Id = @id;";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }
}