using CMCS.Data;
using CMCS.Models;
using CMCS_POE_PART_2.Models;
using System.Data;
using System.Data.SqlClient;

namespace CMCS.Models
{
    public sealed class DbHelper
    {
        private static readonly Lazy<DbHelper> _instance = new(() => new DbHelper());
        public static DbHelper Instance => _instance.Value;

        private readonly string _connString;
        private DbHelper()
        {
            var dbInit = new auto_create_instance_db_tables();
            _connString = dbInit.connectionStringToDatabase;
        }

        private async Task<SqlConnection> GetConnectionAsync()
        {
            var conn = new SqlConnection(_connString);
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();
            return conn;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand("SELECT * FROM Users WHERE email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    userID = reader.GetInt32("userID"),
                    full_names = reader.GetString("full_names"),
                    surname = reader.GetString("surname"),
                    email = reader.GetString("email"),
                    role = reader.GetString("role"),
                    gender = reader.GetString("gender"),
                    password = reader.GetString("password"),
                    date = reader.GetDateTime("date")
                };
            }
            return null;
        }

        public async Task<int> CreateUserAsync(User user)
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand("INSERT INTO Users (full_names, surname, email, role, gender, password, date) OUTPUT INSERTED.userID VALUES (@full_names, @surname, @email, @role, @gender, @password, @date)", conn);
            cmd.Parameters.AddWithValue("@full_names", user.full_names ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@surname", user.surname ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", user.email);
            cmd.Parameters.AddWithValue("@role", user.role);
            cmd.Parameters.AddWithValue("@gender", user.gender);
            cmd.Parameters.AddWithValue("@password", user.password);
            cmd.Parameters.AddWithValue("@date", user.date);
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<List<Claim>> GetClaimsByLecturerAsync(int lecturerId, string? status = null)
        {
            var claims = new List<Claim>();
            using var conn = await GetConnectionAsync();
            var sql = "SELECT * FROM Claims WHERE lecturerID = @lecturerID" + (status != null ? " AND claim_status = @Status" : "");
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@lecturerID", lecturerId);
            if (status != null) cmd.Parameters.AddWithValue("@Status", status);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                claims.Add(new Claim
                {
                    claimID = reader.GetInt32("claimID"),
                    number_of_sessions = reader.GetInt32("number_of_sessions"),
                    number_of_hours = reader.GetInt32("number_of_hours"),
                    amount_of_rate = reader.GetInt32("amount_of_rate"),
                    TotalAmount = reader.GetDecimal("TotalAmount"),
                    module_name = reader.GetString("module_name"),
                    faculty_name = reader.GetString("faculty_name"),
                    supporting_documents = reader.IsDBNull("supporting_documents") ? "" : reader.GetString("supporting_documents"),
                    claim_status = reader.GetString("claim_status"),
                    creating_date = reader.GetDateTime("creating_date"),
                    lecturerID = reader.GetInt32("lecturerID")
                });
            }
            return claims;
        }

        public async Task<List<Claim>> GetPendingClaimsAsync(string status)
        {
            var claims = new List<Claim>();
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand("SELECT c.*, u.full_names + ' ' + u.surname AS LecturerName FROM Claims c JOIN Users u ON c.lecturerID = u.userID WHERE c.claim_status = @Status", conn);
            cmd.Parameters.AddWithValue("@Status", status);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                claims.Add(new Claim
                {
                    claimID = reader.GetInt32("claimID"),
                    number_of_sessions = reader.GetInt32("number_of_sessions"),
                    number_of_hours = reader.GetInt32("number_of_hours"),
                    amount_of_rate = reader.GetInt32("amount_of_rate"),
                    TotalAmount = reader.GetDecimal("TotalAmount"),
                    module_name = reader.GetString("module_name"),
                    faculty_name = reader.GetString("faculty_name"),
                    supporting_documents = reader.IsDBNull("supporting_documents") ? "" : reader.GetString("supporting_documents"),
                    claim_status = reader.GetString("claim_status"),
                    creating_date = reader.GetDateTime("creating_date"),
                    lecturerID = reader.GetInt32("lecturerID"),
                    LecturerName = reader.GetString("LecturerName")
                });
            }
            return claims;
        }

        public async Task<int> CreateClaimAsync(Claim claim)
        {
            using var conn = await GetConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                using var cmd = new SqlCommand("INSERT INTO Claims (number_of_sessions, number_of_hours, amount_of_rate, TotalAmount, module_name, faculty_name, supporting_documents, claim_status, creating_date, lecturerID) OUTPUT INSERTED.claimID VALUES (@sessions, @hours, @rate, @total, @module, @faculty, @docs, @status, @date, @lecturerID)", conn, (SqlTransaction)tx);
                cmd.Parameters.AddWithValue("@sessions", claim.number_of_sessions);
                cmd.Parameters.AddWithValue("@hours", claim.number_of_hours);
                cmd.Parameters.AddWithValue("@rate", claim.amount_of_rate);
                cmd.Parameters.AddWithValue("@total", claim.TotalAmount);
                cmd.Parameters.AddWithValue("@module", claim.module_name);
                cmd.Parameters.AddWithValue("@faculty", claim.faculty_name);
                cmd.Parameters.AddWithValue("@docs", claim.supporting_documents ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", claim.claim_status);
                cmd.Parameters.AddWithValue("@date", claim.creating_date);
                cmd.Parameters.AddWithValue("@lecturerID", claim.lecturerID);
                var id = (int)await cmd.ExecuteScalarAsync();
                await tx.CommitAsync();
                return id;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateClaimStatusAsync(int claimId, string newStatus)
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand("UPDATE Claims SET claim_status = @Status WHERE claimID = @Id", conn);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@Id", claimId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
