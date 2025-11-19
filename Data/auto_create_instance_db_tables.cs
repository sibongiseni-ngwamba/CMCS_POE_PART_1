using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace CMCS_POE_PART_2.Data
{
    /// <summary>
    /// Auto-initializes LocalDB instance, database, and tables for CMCS.
    /// Exposes connection string for DbHelper—thread-safe getter.
    /// </summary>
    public class auto_create_instance_db_tables
    {
        private readonly string instanceName = "claim_system";
        private readonly string databaseName = "cmcs_database";
        private string? _connectionStringToDatabase;  // Backing field for lazy

        /// <summary>
        /// Connection to instance (master—no DB specified).
        /// </summary>
        private string connectionStringToInstance => $@"Server=(localdb)\{instanceName};Integrated Security=true;";

        /// <summary>
        /// Full connection string to the claims database—lazy-evaluated for thread-safety.
        /// </summary>
        public string connectionStringToDatabase
        {
            get
            {
                if (_connectionStringToDatabase == null)
                {
                    Interlocked.CompareExchange(ref _connectionStringToDatabase, $@"Server=(localdb)\{instanceName};Database={databaseName};Integrated Security=true;", null);
                }
                return _connectionStringToDatabase ?? throw new InvalidOperationException("Connection string initialization failed—check LocalDB.");
            }
        }

        /// <summary>
        /// Initializes the entire system: Instance, DB, tables.
        /// Call once on app startup for idempotent setup.
        /// </summary>
        public async Task InitializeSystemAsync()
        {
            try
            {
                // Check and create LocalDB instance
                CreateClaimSystemInstance();

                // Check and create Database
                CreateDatabase();

                // Check and create Tables
                CreateTables();

                Console.WriteLine("LocalDB instance, database, and tables verified successfully! Conn: " + connectionStringToDatabase);  // Debug flex
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing system: {ex.Message}");
                // Forward-thinking: In prod, log to Serilog; here, console suffices for PoE.
                throw;  // Bubble up—don't swallow; let app crash loud if DB's DOA.
            }

            // ... (end of InitializeSystem try block, after CreateTables)
            var healthCheckConn = new SqlConnection(connectionStringToDatabase);
            healthCheckConn.Open();
            var healthCmd = new SqlCommand("SELECT 1", healthCheckConn);
            var start = DateTime.UtcNow;
            await healthCmd.ExecuteScalarAsync();
            var latency = (DateTime.UtcNow - start).TotalMilliseconds;
            Console.WriteLine($"DB Health: Latency {latency:0.00}ms – Green for go-live.");
            healthCheckConn.Close();
        }

        // -----------------------------
        // LocalDB Instance Handling
        // -----------------------------
        private void CreateClaimSystemInstance()
        {
            if (CheckInstanceExists())
            {
                Console.WriteLine($"LocalDB instance '{instanceName}' already exists.");
                return;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c sqllocaldb create \"{instanceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                    Console.WriteLine($"LocalDB instance '{instanceName}' created successfully! Output: {output.Trim()}");
                else
                    throw new InvalidOperationException($"Error creating instance: {error}");
            }
        }

        private bool CheckInstanceExists()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c sqllocaldb info \"{instanceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error) &&
                    error.Contains($"LocalDB instance \"{instanceName}\" doesn't exist", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return !string.IsNullOrWhiteSpace(output)
                    && !output.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase);
            }
        }

        // -----------------------------
        // Database Handling
        // -----------------------------
        private void CreateDatabase()
        {
            string createDbQuery = $@"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')
                BEGIN
                    CREATE DATABASE [{databaseName}];
                END";

            using (var connection = new SqlConnection(connectionStringToInstance))
            {
                connection.Open();
                using (var command = new SqlCommand(createDbQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"Database '{databaseName}' verified or created.");
        }

        // -----------------------------
        // Table Handling
        // -----------------------------
        private void CreateTables()
        {
            string createUsersTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
                BEGIN
                    CREATE TABLE Users (
                        userID INT PRIMARY KEY IDENTITY(1,1),
                        full_names VARCHAR(100),
                        surname VARCHAR(100),
                        email VARCHAR(100) UNIQUE,
                        role VARCHAR(100),
                        gender VARCHAR(100),
                        password VARCHAR(100),
                        date DATE
                    );
                END";

            // Claims table: Create if not exists, then add columns if missing
            string createClaimsTable = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Claims' AND xtype='U')
            BEGIN
                CREATE TABLE Claims (
                    claimID INT PRIMARY KEY IDENTITY(1,1),
                    number_of_sessions INT,
                    number_of_hours DECIMAL(5,2),
                    amount_of_rate DECIMAL(10,2),
                    TotalAmount DECIMAL(10,2),
                    module_name VARCHAR(100),
                    faculty_name VARCHAR(100),
                    supporting_documents VARCHAR(100),
                    claim_status VARCHAR(100) DEFAULT 'Pending',
                    creating_date DATE,
                    lecturerID INT,
                    FOREIGN KEY (lecturerID) REFERENCES Users(userID)
                );
            END
            ELSE
            BEGIN
                -- Add TotalAmount if missing
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Claims' AND COLUMN_NAME = 'TotalAmount')
                BEGIN
                    ALTER TABLE Claims ADD TotalAmount DECIMAL(10,2);
                END
                -- Similarly for other columns if needed, e.g., number_of_hours
            END";
            using (var connection = new SqlConnection(connectionStringToDatabase))
            {
                connection.Open();

                using (var cmd = new SqlCommand(createUsersTable, connection))
                    cmd.ExecuteNonQuery();

                using (var cmd = new SqlCommand(createClaimsTable, connection))
                    cmd.ExecuteNonQuery();
            }

            Console.WriteLine("Tables 'Users' and 'Claims' verified or created.");
        }

    }
}
