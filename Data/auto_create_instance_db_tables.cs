using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace CMCS_POE_PART_2.Data
{
    public class auto_create_instance_db_tables
    {
        private readonly string instanceName = "claim_system";
        private readonly string databaseName = "cmcs_database";
        private string? _connectionStringToDatabase;

        private string connectionStringToInstance => $@"Server=(localdb)\{instanceName};Integrated Security=true;";
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

        public async Task InitializeSystemAsync()
        {
            try
            {
                CreateClaimSystemInstance();
                CreateDatabase();
                CreateTables();
                Console.WriteLine("LocalDB instance, database, and tables verified successfully! Conn: " + connectionStringToDatabase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing system: {ex.Message}");
                throw;
            }

            using var healthCheckConn = new SqlConnection(connectionStringToDatabase);
            healthCheckConn.Open();
            using var healthCmd = new SqlCommand("SELECT 1", healthCheckConn);
            var start = DateTime.UtcNow;
            await healthCmd.ExecuteScalarAsync();
            var latency = (DateTime.UtcNow - start).TotalMilliseconds;
            Console.WriteLine($"DB Health: Latency {latency:0.00}ms – Green for go-live.");
            healthCheckConn.Close();
        }

        private void CreateClaimSystemInstance()
        {
            if (CheckInstanceExists())
            {
                Console.WriteLine($"LocalDB instance '{instanceName}' already exists.");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c sqllocaldb create \"{instanceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = psi };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode == 0) Console.WriteLine($"LocalDB instance '{instanceName}' created successfully! Output: {output.Trim()}");
            else throw new InvalidOperationException($"Error creating instance: {error}");
        }

        private bool CheckInstanceExists()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c sqllocaldb info \"{instanceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = psi };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error) &&
                error.Contains($"LocalDB instance \"{instanceName}\" doesn't exist", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return !string.IsNullOrWhiteSpace(output)
                   && !output.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase);
        }

        private void CreateDatabase()
        {
            string createDbQuery = $@"
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')
BEGIN
  CREATE DATABASE [{databaseName}];
END";

            using var connection = new SqlConnection(connectionStringToInstance);
            connection.Open();
            using var command = new SqlCommand(createDbQuery, connection);
            command.ExecuteNonQuery();

            Console.WriteLine($"Database '{databaseName}' verified or created.");
        }

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
  IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Claims' AND COLUMN_NAME = 'TotalAmount')
  BEGIN
    ALTER TABLE Claims ADD TotalAmount DECIMAL(10,2);
  END
END";

            string createAuditTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditTrail' AND xtype='U')
BEGIN
  CREATE TABLE AuditTrail (
    auditID INT PRIMARY KEY IDENTITY(1,1),
    claimID INT NOT NULL,
    action VARCHAR(50) NOT NULL,
    actorUserID INT NOT NULL,
    timestamp DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (claimID) REFERENCES Claims(claimID),
    FOREIGN KEY (actorUserID) REFERENCES Users(userID)
  );
END";

            using var connection = new SqlConnection(connectionStringToDatabase);
            connection.Open();
            using (var cmd = new SqlCommand(createUsersTable, connection)) cmd.ExecuteNonQuery();
            using (var cmd = new SqlCommand(createClaimsTable, connection)) cmd.ExecuteNonQuery();
            using (var cmd = new SqlCommand(createAuditTable, connection)) cmd.ExecuteNonQuery();

            Console.WriteLine("Tables 'Users', 'Claims', and 'AuditTrail' verified or created.");
        }
    }
}
