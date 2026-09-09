using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;

namespace Assign_2
{
    public static class UserDatabase
    {
        private static string ProjectRoot =
            Path.GetFullPath(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"..\..\..\"));

        private static string DbPath =
            Path.Combine(ProjectRoot, "appdatabase.mdf");

        private const string DatabaseName = "UserDb_Root";

        private static string ConnectionString =
            $@"Server=(localdb)\mssqllocaldb;
                AttachDbFilename={DbPath};
                Database={DatabaseName};
                Integrated Security=True;
                TrustServerCertificate=True;";

        private const string MasterConnectionString =
            @"Server=(localdb)\mssqllocaldb;
              Database=master;
              Integrated Security=True;
              TrustServerCertificate=True;";

        // =========================================================
        // DATABASE STARTUP
        // =========================================================

        static UserDatabase()
        {
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            using var master =
                new SqlConnection(MasterConnectionString);

            master.Open();

            bool isRegistered = RunScalarBool(
                master,
                $"SELECT COUNT(*) FROM sys.databases WHERE name = '{DatabaseName}';");

            bool fileExists = File.Exists(DbPath);

            // Database exists but MDF file is missing
            if (isRegistered && !fileExists)
            {
                RunNonQuery(
                    master,
                    $"DROP DATABASE IF EXISTS [{DatabaseName}];");

                isRegistered = false;
            }

            // MDF exists but database isn't attached
            if (!isRegistered && fileExists)
            {
                RunNonQuery(
                    master,
                    $"CREATE DATABASE [{DatabaseName}] " +
                    $"ON (FILENAME = '{DbPath}') FOR ATTACH;");
            }
            // MDF doesn't exist
            else if (!isRegistered && !fileExists)
            {
                RunNonQuery(
                    master,
                    $"CREATE DATABASE [{DatabaseName}] " +
                    $"ON PRIMARY " +
                    $"(NAME = {DatabaseName}_Data, " +
                    $"FILENAME = '{DbPath}');");
            }

            using var connection = OpenConnection();

            // =====================================================
            // ROLES TABLE
            // =====================================================

            bool rolesExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Roles', 'U');",
                true);

            if (!rolesExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.Roles
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        RoleName NVARCHAR(50) UNIQUE NOT NULL
                    );");

                SeedRoles(connection);
            }

            // =====================================================
            // USERS TABLE
            // =====================================================

            bool usersExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Users', 'U');",
                true);

            if (!usersExists)
            {
                CreateUsersTable(connection);
                SeedDefaultUsers(connection);
            }
        }

        // =========================================================
        // CREATE USERS TABLE
        // =========================================================

        private static void CreateUsersTable(
            SqlConnection connection)
        {
            RunNonQuery(connection, @"
                CREATE TABLE dbo.Users
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,

                    Username NVARCHAR(255) UNIQUE NOT NULL,

                    PasswordHash NVARCHAR(MAX) NOT NULL,

                    firstnames NVARCHAR(255) NOT NULL,

                    lastnames NVARCHAR(255) NOT NULL,

                    date_created DATETIME2 NOT NULL,

                    RoleId INT NOT NULL,

                    CONSTRAINT FK_Users_Roles
                        FOREIGN KEY (RoleId)
                        REFERENCES dbo.Roles(Id)
                );");
        }

        // =========================================================
        // SEED ROLES
        // =========================================================

        private static void SeedRoles(
            SqlConnection connection)
        {
            string[] roles =
            {
                "Admin",
                "User"
            };

            foreach (string role in roles)
            {
                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Roles
                        (RoleName)
                      VALUES
                        (@role);",
                    connection);

                command.Parameters.AddWithValue(
                    "@role",
                    role);

                command.ExecuteNonQuery();
            }
        }

        // =========================================================
        // SEED DEFAULT USERS
        // =========================================================

        private static void SeedDefaultUsers(
            SqlConnection connection)
        {
            var defaultUsers =
                new
                (
                    string Username,
                    string Password,
                    int RoleId,
                    string Firstname,
                    string Lastname,
                    DateTime DateCreated
                )[]
                {
                    (
                        "admin",
                        "admin123",
                        1,
                        "Admin",
                        "Account",
                        new DateTime(2026, 9, 9)
                    ),

                    (
                        "alex",
                        "user123",
                        2,
                        "Alex",
                        "User",
                        new DateTime(2026, 9, 9)
                    ),

                    (
                        "jordan",
                        "user123",
                        2,
                        "Jordan",
                        "User",
                        new DateTime(2026, 9, 9)
                    ),

                    (
                        "sam",
                        "user123",
                        2,
                        "Sam",
                        "User",
                        new DateTime(2026, 9, 9)
                    ),

                    (
                        "taylor",
                        "user123",
                        2,
                        "Taylor",
                        "User",
                        new DateTime(2026, 9, 9)
                    ),

                    (
                        "bot",
                        "temp",
                        1,
                        "Backupcharacter1",
                        "",
                        new DateTime(2026, 9, 9)
                    )
                };

            foreach (var user in defaultUsers)
            {
                string hash =
                    BCrypt.Net.BCrypt.HashPassword(
                        user.Password);

                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Users
                    (
                        Username,
                        PasswordHash,
                        firstnames,
                        lastnames,
                        date_created,
                        RoleId
                    )
                    VALUES
                    (
                        @username,
                        @hash,
                        @firstname,
                        @lastname,
                        @date_created,
                        @roleId
                    );",
                    connection);

                command.Parameters.AddWithValue(
                    "@username",
                    user.Username);

                command.Parameters.AddWithValue(
                    "@hash",
                    hash);

                command.Parameters.AddWithValue(
                    "@firstname",
                    user.Firstname);

                command.Parameters.AddWithValue(
                    "@lastname",
                    user.Lastname);

                command.Parameters.AddWithValue(
                    "@date_created",
                    user.DateCreated);

                command.Parameters.AddWithValue(
                    "@roleId",
                    user.RoleId);

                command.ExecuteNonQuery();
            }
        }

        // =========================================================
        // LOGIN
        // =========================================================

        public static bool Login(
            string username,
            string password,
            out string role)
        {
            role = string.Empty;

            using var connection =
                OpenConnection();

            using var command =
                new SqlCommand(
                    @"SELECT
                        r.RoleName,
                        u.PasswordHash
                      FROM dbo.Users u
                      INNER JOIN dbo.Roles r
                        ON u.RoleId = r.Id
                      WHERE u.Username = @username;",
                    connection);

            command.Parameters.AddWithValue(
                "@username",
                username);

            using var reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return false;
            }

            string roleName =
                reader.GetString(0);

            string storedHash =
                reader.GetString(1);

            bool passwordCorrect =
                BCrypt.Net.BCrypt.Verify(
                    password,
                    storedHash);

            if (!passwordCorrect)
            {
                return false;
            }

            role = roleName;

            return true;
        }

        // =========================================================
        // REGISTER
        // =========================================================

        public static bool Register(
            string username,
            string password,
            string role)
        {
            using var connection =
                OpenConnection();

            // Get RoleId
            using var roleCommand =
                new SqlCommand(
                    @"SELECT Id
                      FROM dbo.Roles
                      WHERE RoleName = @role;",
                    connection);

            roleCommand.Parameters.AddWithValue(
                "@role",
                role);

            object roleResult =
                roleCommand.ExecuteScalar();

            if (roleResult == null)
            {
                throw new Exception(
                    "Invalid role.");
            }

            int roleId =
                Convert.ToInt32(roleResult);

            using var command =
                new SqlCommand(
                    @"INSERT INTO dbo.Users
                    (
                        Username,
                        PasswordHash,
                        firstnames,
                        lastnames,
                        date_created,
                        RoleId
                    )
                    VALUES
                    (
                        @username,
                        @hash,
                        @firstname,
                        @lastname,
                        @date_created,
                        @roleId
                    );",
                    connection);

            command.Parameters.AddWithValue(
                "@username",
                username);

            command.Parameters.AddWithValue(
                "@hash",
                BCrypt.Net.BCrypt.HashPassword(
                    password));

            command.Parameters.AddWithValue(
                "@firstname",
                "");

            command.Parameters.AddWithValue(
                "@lastname",
                "");

            command.Parameters.AddWithValue(
                "@date_created",
                DateTime.Now);

            command.Parameters.AddWithValue(
                "@roleId",
                roleId);

            try
            {
                command.ExecuteNonQuery();

                return true;
            }
            catch (SqlException)
            {
                throw new Exception(
                    "Username already exists.");
            }
        }

        // =========================================================
        // GET ALL USERS
        // =========================================================

        public static List<UserRecord> GetAllUsers()
        {
            var users =
                new List<UserRecord>();

            using var connection =
                OpenConnection();

            using var command =
                new SqlCommand(
                    @"SELECT
                        u.Id,
                        u.Username,
                        r.RoleName
                      FROM dbo.Users u
                      INNER JOIN dbo.Roles r
                        ON u.RoleId = r.Id
                      ORDER BY u.Username;",
                    connection);

            using var reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                users.Add(
                    new UserRecord
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Role = reader.GetString(2)
                    });
            }

            return users;
        }

        // =========================================================
        // UPDATE USER
        // =========================================================

        public static bool UpdateUser(
            int id,
            string newUsername,
            string newRole)
        {
            using var connection =
                OpenConnection();

            // Find RoleId
            using var roleCommand =
                new SqlCommand(
                    @"SELECT Id
                      FROM dbo.Roles
                      WHERE RoleName = @role;",
                    connection);

            roleCommand.Parameters.AddWithValue(
                "@role",
                newRole);

            object roleResult =
                roleCommand.ExecuteScalar();

            if (roleResult == null)
            {
                throw new Exception(
                    "Invalid role.");
            }

            int roleId =
                Convert.ToInt32(roleResult);

            using var command =
                new SqlCommand(
                    @"UPDATE dbo.Users
                      SET Username = @username,
                          RoleId = @roleId
                      WHERE Id = @id;",
                    connection);

            command.Parameters.AddWithValue(
                "@username",
                newUsername);

            command.Parameters.AddWithValue(
                "@roleId",
                roleId);

            command.Parameters.AddWithValue(
                "@id",
                id);

            try
            {
                return command.ExecuteNonQuery() > 0;
            }
            catch (SqlException)
            {
                throw new Exception(
                    "Username already exists.");
            }
        }

        // =========================================================
        // UPDATE PASSWORD
        // =========================================================

        public static bool UpdatePassword(
            int id,
            string newPassword)
        {
            using var connection =
                OpenConnection();

            using var command =
                new SqlCommand(
                    @"UPDATE dbo.Users
                      SET PasswordHash = @hash
                      WHERE Id = @id;",
                    connection);

            command.Parameters.AddWithValue(
                "@hash",
                BCrypt.Net.BCrypt.HashPassword(
                    newPassword));

            command.Parameters.AddWithValue(
                "@id",
                id);

            return command.ExecuteNonQuery() > 0;
        }

        // =========================================================
        // DELETE USER
        // =========================================================

        public static bool DeleteUser(
            int id)
        {
            using var connection =
                OpenConnection();

            using var command =
                new SqlCommand(
                    @"DELETE FROM dbo.Users
                      WHERE Id = @id;",
                    connection);

            command.Parameters.AddWithValue(
                "@id",
                id);

            return command.ExecuteNonQuery() > 0;
        }

        // =========================================================
        // OPEN CONNECTION
        // =========================================================

        public static SqlConnection OpenConnection()
        {
            var connection =
                new SqlConnection(
                    ConnectionString);

            connection.Open();

            return connection;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private static void RunNonQuery(
            SqlConnection connection,
            string sql)
        {
            using var command =
                new SqlCommand(
                    sql,
                    connection);

            command.ExecuteNonQuery();
        }

        private static bool RunScalarBool(
            SqlConnection connection,
            string sql,
            bool checkNotNull = false)
        {
            using var command =
                new SqlCommand(
                    sql,
                    connection);

            object result =
                command.ExecuteScalar();

            if (checkNotNull)
            {
                return result != null &&
                       result != DBNull.Value;
            }

            return Convert.ToInt32(result) > 0;
        }
    }
}