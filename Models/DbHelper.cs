using CMCS_POE_PART_2.Data;
using System.Data;
using System.Data.SqlClient;

namespace CMCS_POE_PART_2.Models
{
    public sealed class DbHelper
    {
        private static readonly Lazy<DbHelper> _instance = new(() => new DbHelper());
        public static DbHelper Instance => _instance.Value;

        private string _connString;
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
            cmd.Parameters.AddWithValue("@Email", email ?? throw new ArgumentNullException(nameof(email)));
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    userID = (int)reader["userID"],
                    full_names = reader["full_names"]?.ToString() ?? "",
                    surname = reader["surname"]?.ToString() ?? "",
                    email = reader["email"].ToString()!,
                    role = reader["role"]?.ToString() ?? "",
                    gender = reader["gender"]?.ToString() ?? "",
                    password = reader["password"]?.ToString() ?? "",
                    date = DateTime.Parse(reader["date"].ToString()!)
                };
            }
            return null;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand("SELECT * FROM Users WHERE userID = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    userID = (int)reader["userID"],
                    full_names = reader["full_names"]?.ToString() ?? "",
                    surname = reader["surname"]?.ToString() ?? "",
                    email = reader["email"]?.ToString() ?? "",
                    role = reader["role"]?.ToString() ?? "",
                    gender = reader["gender"]?.ToString() ?? "",
                    password = reader["password"]?.ToString() ?? "",
                    date = DateTime.Parse(reader["date"].ToString()!)
                };
            }
            return null;
        }

        public async Task<int> CreateUserAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(@"
INSERT INTO Users (full_names, surname, email, role, gender, password, date)
OUTPUT INSERTED.userID
VALUES (@full_names, @surname, @email, @role, @gender, @password, @date)", conn);
            cmd.Parameters.AddWithValue("@full_names", (object?)user.full_names ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@surname", (object?)user.surname ?? DBNull.Value);
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
                    claimID = (int)reader["claimID"],
                    number_of_sessions = Convert.ToInt32(reader["number_of_sessions"]),
                    number_of_hours = Convert.ToInt32(reader["number_of_hours"]),
                    amount_of_rate = Convert.ToInt32(reader["amount_of_rate"]),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                    module_name = reader["module_name"].ToString()!,
                    faculty_name = reader["faculty_name"].ToString()!,
                    supporting_documents = reader["supporting_documents"] == DBNull.Value ? "" : reader["supporting_documents"].ToString()!,
                    claim_status = reader["claim_status"].ToString()!,
                    creating_date = DateTime.Parse(reader["creating_date"].ToString()!),
                    lecturerID = Convert.ToInt32(reader["lecturerID"])
                });
            }
            return claims;
        }

        public async Task<List<Claim>> GetPendingClaimsAsync(string status)
        {
            var claims = new List<Claim>();
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(@"
SELECT c.*, u.full_names + ' ' + u.surname AS LecturerName
FROM Claims c
JOIN Users u ON c.lecturerID = u.userID
WHERE c.claim_status = @Status", conn);
            cmd.Parameters.AddWithValue("@Status", status);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                claims.Add(new Claim
                {
                    claimID = (int)reader["claimID"],
                    number_of_sessions = Convert.ToInt32(reader["number_of_sessions"]),
                    number_of_hours = Convert.ToInt32(reader["number_of_hours"]),
                    amount_of_rate = Convert.ToInt32(reader["amount_of_rate"]),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                    module_name = reader["module_name"].ToString()!,
                    faculty_name = reader["faculty_name"].ToString()!,
                    supporting_documents = reader["supporting_documents"] == DBNull.Value ? "" : reader["supporting_documents"].ToString()!,
                    claim_status = reader["claim_status"].ToString()!,
                    creating_date = DateTime.Parse(reader["creating_date"].ToString()!),
                    lecturerID = Convert.ToInt32(reader["lecturerID"]),
                    LecturerName = reader["LecturerName"].ToString()!
                });
            }
            return claims;
        }

        public async Task<int> CreateClaimAsync(Claim claim)
        {
            if (claim == null) throw new ArgumentNullException(nameof(claim));
            using var conn = await GetConnectionAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                using var cmd = new SqlCommand(@"
INSERT INTO Claims (number_of_sessions, number_of_hours, amount_of_rate, TotalAmount, module_name, faculty_name, supporting_documents, claim_status, creating_date, lecturerID)
OUTPUT INSERTED.claimID
VALUES (@sessions, @hours, @rate, @total, @module, @faculty, @docs, @status, @date, @lecturerID)", conn, (SqlTransaction)tx);
                cmd.Parameters.AddWithValue("@sessions", claim.number_of_sessions);
                cmd.Parameters.AddWithValue("@hours", claim.number_of_hours);
                cmd.Parameters.AddWithValue("@rate", claim.amount_of_rate);
                cmd.Parameters.AddWithValue("@total", claim.TotalAmount);
                cmd.Parameters.AddWithValue("@module", claim.module_name);
                cmd.Parameters.AddWithValue("@faculty", claim.faculty_name);
                cmd.Parameters.AddWithValue("@docs", string.IsNullOrWhiteSpace(claim.supporting_documents) ? (object)DBNull.Value : claim.supporting_documents);
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
            if (newStatus == null) throw new ArgumentNullException(nameof(newStatus));
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand("UPDATE Claims SET claim_status = @Status WHERE claimID = @Id", conn);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@Id", claimId);
            await cmd.ExecuteNonQueryAsync();
        }

        // HR: Approved claims with optional date range
        public async Task<List<Claim>> GetApprovedClaimsAsync(DateTime? from, DateTime? to)
        {
            var claims = new List<Claim>();
            using var conn = await GetConnectionAsync();
            var sql = @"
SELECT c.*, u.full_names + ' ' + u.surname AS LecturerName
FROM Claims c
JOIN Users u ON c.lecturerID = u.userID
WHERE c.claim_status = 'Approved'";

            if (from.HasValue) sql += " AND c.creating_date >= @from";
            if (to.HasValue) sql += " AND c.creating_date <= @to";

            using var cmd = new SqlCommand(sql, conn);
            if (from.HasValue) cmd.Parameters.AddWithValue("@from", from.Value);
            if (to.HasValue) cmd.Parameters.AddWithValue("@to", to.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                claims.Add(new Claim
                {
                    claimID = (int)reader["claimID"],
                    number_of_sessions = Convert.ToInt32(reader["number_of_sessions"]),
                    number_of_hours = Convert.ToInt32(reader["number_of_hours"]),
                    amount_of_rate = Convert.ToInt32(reader["amount_of_rate"]),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                    module_name = reader["module_name"].ToString()!,
                    faculty_name = reader["faculty_name"].ToString()!,
                    supporting_documents = reader["supporting_documents"] == DBNull.Value ? "" : reader["supporting_documents"].ToString()!,
                    claim_status = reader["claim_status"].ToString()!,
                    creating_date = DateTime.Parse(reader["creating_date"].ToString()!),
                    lecturerID = Convert.ToInt32(reader["lecturerID"]),
                    LecturerName = reader["LecturerName"].ToString()!
                });
            }
            return claims;
        }

        // HR: Lecturer search/list
        public async Task<List<User>> GetAllLecturersAsync(string? query)
        {
            var users = new List<User>();
            using var conn = await GetConnectionAsync();
            var sql = "SELECT * FROM Users WHERE role = 'Lecturer'";
            if (!string.IsNullOrWhiteSpace(query))
                sql += " AND (full_names LIKE @q OR surname LIKE @q OR email LIKE @q)";
            using var cmd = new SqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(query))
                cmd.Parameters.AddWithValue("@q", $"%{query}%");
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    userID = (int)reader["userID"],
                    full_names = reader["full_names"]?.ToString() ?? "",
                    surname = reader["surname"]?.ToString() ?? "",
                    email = reader["email"]?.ToString() ?? "",
                    role = reader["role"]?.ToString() ?? "",
                    gender = reader["gender"]?.ToString() ?? "",
                    password = reader["password"]?.ToString() ?? "",
                    date = DateTime.Parse(reader["date"].ToString()!)
                });
            }
            return users;
        }

        // HR: Update lecturer details
        public async Task UpdateLecturerAsync(User user)
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(@"
    UPDATE Users SET
      full_names = @fn,
      surname = @sn,
      email = @em,
      gender = @gd
    WHERE userID = @id AND role = 'Lecturer'", conn);

            cmd.Parameters.AddWithValue("@fn", user.full_names ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sn", user.surname ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@em", user.email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@gd", user.gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", user.userID);

            await cmd.ExecuteNonQueryAsync();
        }


        // Audit log
        public async Task AppendAuditAsync(int claimId, string action, int actorUserId)
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(@"
INSERT INTO AuditTrail (claimID, action, actorUserID)
VALUES (@cid, @act, @uid)", conn);
            cmd.Parameters.AddWithValue("@cid", claimId);
            cmd.Parameters.AddWithValue("@act", action);
            cmd.Parameters.AddWithValue("@uid", actorUserId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
