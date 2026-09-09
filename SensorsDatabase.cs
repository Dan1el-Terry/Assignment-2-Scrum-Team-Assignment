using Microsoft.Data.SqlClient;
using System;

namespace Assign_2
{
    public static class SensorsDatabase
    {
        public static void Initialize()
        {
            using var connection = UserDatabase.OpenConnection();

            // Create Locations table
            bool locationsExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Locations', 'U');",
                true);

            if (!locationsExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.Locations
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        City NVARCHAR(100) NOT NULL,
                        Suburb NVARCHAR(100) NOT NULL
                    );");
            }

            // Create Sensors table
            bool sensorsExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Sensors', 'U');",
                true);

            if (!sensorsExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.Sensors
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Date_Installed DATETIME2 NOT NULL,
                        Make NVARCHAR(100) NOT NULL,
                        Model NVARCHAR(100) NOT NULL,
                        Location_Id INT NOT NULL,

                        CONSTRAINT FK_Sensors_Locations
                        FOREIGN KEY (Location_Id)
                        REFERENCES dbo.Locations(Id)
                    );");
            }

            // Create Data table
            bool dataExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Data', 'U');",
                true);

            if (!dataExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.Data
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        [Timestamp] DATETIME2 NOT NULL
                            DEFAULT GETDATE(),
                        Temperature FLOAT NOT NULL,
                        Sensor_Id INT NOT NULL,

                        CONSTRAINT FK_Data_Sensors
                        FOREIGN KEY (Sensor_Id)
                        REFERENCES dbo.Sensors(Id)
                    );");
            }
        }

        private static void RunNonQuery(
            SqlConnection connection,
            string sql)
        {
            using var command =
                new SqlCommand(sql, connection);

            command.ExecuteNonQuery();
        }

        private static bool RunScalarBool(
            SqlConnection connection,
            string sql,
            bool checkNotNull = false)
        {
            using var command =
                new SqlCommand(sql, connection);

            object result = command.ExecuteScalar();

            if (checkNotNull)
            {
                return result != null &&
                       result != DBNull.Value;
            }

            return Convert.ToInt32(result) > 0;
        }
    }
}