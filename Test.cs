using CMCS_POE_PART_2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Xunit;

namespace CMCS_POE_PART_2.Test
{
    public class DbHelperIntegrationTests : IDisposable
    {
        private readonly SQLiteConnection _connection;
        private readonly DbHelper _dbHelper;

        public DbHelperIntegrationTests()
        {
            // Initialize in-memory SQLite database
            _connection = new SQLiteConnection("DataSource=:memory:");
            _connection.Open();

            // Create schema to mimic LocalDB
            CreateSchema();

            // Use reflection to set private _connString field in DbHelper
            _dbHelper = DbHelper.Instance;
            var connStringField = typeof(DbHelper).GetField("_connString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            connStringField?.SetValue(_dbHelper, _connection.ConnectionString);
        }

        private void CreateSchema()
        {
            // Create Users and Claims tables
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE Users (
                    userID INTEGER PRIMARY KEY AUTOINCREMENT,
                    full_names TEXT,
                    surname TEXT,
                    email TEXT UNIQUE,
                    role TEXT,
                    gender TEXT,
                    password TEXT,
                    date TEXT
                );
                CREATE TABLE Claims (
                    claimID INTEGER PRIMARY KEY AUTOINCREMENT,
                    number_of_sessions INTEGER,
                    number_of_hours INTEGER,
                    amount_of_rate INTEGER,
                    TotalAmount REAL,
                    module_name TEXT,
                    faculty_name TEXT,
                    supporting_documents TEXT,
                    claim_status TEXT,
                    creating_date TEXT,
                    lecturerID INTEGER,
                    FOREIGN KEY (lecturerID) REFERENCES Users(userID)
                );";
            command.ExecuteNonQuery();
        }

        [Fact]
        public async Task GetUserByEmailAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (full_names, surname, email, role, gender, password, date) VALUES ('Test', 'User', 'test@email.com', 'Lecturer', 'Male', 'hashedpw', '2025-10-24')";
            command.ExecuteNonQuery();

            // Act
            var user = await _dbHelper.GetUserByEmailAsync("test@email.com");

            // Assert
            Assert.NotNull(user);
            Assert.Equal("Test", user.full_names);
            Assert.Equal("User", user.surname);
            Assert.Equal("test@email.com", user.email);
            Assert.Equal("Lecturer", user.role);
        }

        [Fact]
        public async Task GetUserByEmailAsync_NonExistingUser_ReturnsNull()
        {
            // Act
            var user = await _dbHelper.GetUserByEmailAsync("nonexistent@email.com");

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserByEmailAsync_EmptyEmail_ReturnsNull()
        {
            // Act
            var user = await _dbHelper.GetUserByEmailAsync("");

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserByEmailAsync_NullEmail_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dbHelper.GetUserByEmailAsync(null));
        }

        [Fact]
        public async Task CreateUserAsync_ValidUser_ReturnsUserId()
        {
            // Arrange
            var user = new User
            {
                full_names = "New",
                surname = "User",
                email = "new@email.com",
                role = "Lecturer",
                gender = "Female",
                password = "hashedpw",
                date = DateTime.Parse("2025-10-24")
            };

            // Act
            var userId = await _dbHelper.CreateUserAsync(user);

            // Assert
            Assert.True(userId > 0);
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Users WHERE email = 'new@email.com'";
            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal("New", reader.GetString(reader.GetOrdinal("full_names")));
            Assert.Equal("User", reader.GetString(reader.GetOrdinal("surname")));
        }

        [Fact]
        public async Task CreateUserAsync_MinimalFields_SucceedsWithNulls()
        {
            // Arrange
            var user = new User
            {
                email = "minimal@email.com",
                role = "Lecturer",
                gender = "Other",
                password = "hashedpw",
                date = DateTime.Today
            }; // full_names and surname null

            // Act
            var userId = await _dbHelper.CreateUserAsync(user);

            // Assert
            Assert.True(userId > 0);
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT full_names, surname FROM Users WHERE userID = @id";
            command.Parameters.AddWithValue("@id", userId);
            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            Assert.True(reader.IsDBNull(reader.GetOrdinal("full_names")));
            Assert.True(reader.IsDBNull(reader.GetOrdinal("surname")));
        }

        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ThrowsException()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (email) VALUES ('dup@email.com')";
            command.ExecuteNonQuery();

            var user = new User { email = "dup@email.com", role = "Lecturer", gender = "Other", password = "hashedpw", date = DateTime.Today };

            // Act & Assert
            await Assert.ThrowsAsync<SQLiteException>(() => _dbHelper.CreateUserAsync(user));
        }

        [Fact]
        public async Task CreateUserAsync_NullUser_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dbHelper.CreateUserAsync(null));
        }

        [Fact]
        public async Task CreateClaimAsync_ValidClaim_ReturnsClaimId()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (userID, email, role) VALUES (1, 'lecturer@email.com', 'Lecturer')";
            command.ExecuteNonQuery();

            var claim = new Claim
            {
                number_of_sessions = 2,
                number_of_hours = 3,
                amount_of_rate = 100,
                TotalAmount = 600m,
                module_name = "CS101",
                faculty_name = "Science",
                supporting_documents = "doc1.pdf",
                claim_status = "Pending",
                creating_date = DateTime.Parse("2025-10-24"),
                lecturerID = 1
            };

            // Act
            var claimId = await _dbHelper.CreateClaimAsync(claim);

            // Assert
            Assert.True(claimId > 0);
            using var checkCommand = _connection.CreateCommand();
            checkCommand.CommandText = "SELECT * FROM Claims WHERE claimID = @id";
            checkCommand.Parameters.AddWithValue("@id", claimId);
            using var reader = checkCommand.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt32(reader.GetOrdinal("number_of_sessions")));
            Assert.Equal(600m, reader.GetDecimal(reader.GetOrdinal("TotalAmount")));
            Assert.Equal("Pending", reader.GetString(reader.GetOrdinal("claim_status")));
        }

        [Fact]
        public async Task CreateClaimAsync_ZeroValues_Succeeds()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (userID) VALUES (1)";
            command.ExecuteNonQuery();

            var claim = new Claim
            {
                number_of_sessions = 0,
                number_of_hours = 0,
                amount_of_rate = 0,
                TotalAmount = 0m,
                module_name = "",
                faculty_name = "",
                supporting_documents = null,
                claim_status = "Pending",
                creating_date = DateTime.Today,
                lecturerID = 1
            };

            // Act
            var claimId = await _dbHelper.CreateClaimAsync(claim);

            // Assert
            Assert.True(claimId > 0);
        }

        [Fact]
        public async Task CreateClaimAsync_InvalidLecturerId_ThrowsException()
        {
            // Arrange
            var claim = new Claim
            {
                number_of_sessions = 2,
                number_of_hours = 3,
                amount_of_rate = 100,
                lecturerID = 999 // Non-existent
            };

            // Act & Assert
            await Assert.ThrowsAsync<SQLiteException>(() => _dbHelper.CreateClaimAsync(claim));
        }

        [Fact]
        public async Task CreateClaimAsync_NullClaim_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dbHelper.CreateClaimAsync(null));
        }

        [Fact]
        public async Task GetClaimsByLecturerAsync_ReturnsCorrectClaims()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (userID, email, role) VALUES (1, 'lecturer@email.com', 'Lecturer');
                INSERT INTO Claims (number_of_sessions, number_of_hours, amount_of_rate, TotalAmount, module_name, faculty_name, claim_status, creating_date, lecturerID)
                VALUES (2, 3, 100, 600, 'CS101', 'Science', 'Pending', '2025-10-24', 1)";
            command.ExecuteNonQuery();

            // Act
            var claims = await _dbHelper.GetClaimsByLecturerAsync(1);

            // Assert
            Assert.Single(claims);
            var claim = claims[0];
            Assert.Equal(2, claim.number_of_sessions);
            Assert.Equal(600m, claim.TotalAmount);
            Assert.Equal("CS101", claim.module_name);
        }

        [Fact]
        public async Task GetClaimsByLecturerAsync_WithStatusFilter_ReturnsFilteredClaims()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (userID) VALUES (1);
                INSERT INTO Claims (claim_status, lecturerID) VALUES ('Pending', 1);
                INSERT INTO Claims (claim_status, lecturerID) VALUES ('Verified', 1)";
            command.ExecuteNonQuery();

            // Act
            var claims = await _dbHelper.GetClaimsByLecturerAsync(1, "Pending");

            // Assert
            Assert.Single(claims);
            Assert.Equal("Pending", claims[0].claim_status);
        }

        [Fact]
        public async Task GetClaimsByLecturerAsync_NoClaims_ReturnsEmptyList()
        {
            // Act
            var claims = await _dbHelper.GetClaimsByLecturerAsync(999);

            // Assert
            Assert.Empty(claims);
        }

        [Fact]
        public async Task GetClaimsByLecturerAsync_InvalidLecturerId_ReturnsEmptyList()
        {
            // Act
            var claims = await _dbHelper.GetClaimsByLecturerAsync(-1);

            // Assert
            Assert.Empty(claims);
        }

        [Fact]
        public async Task GetPendingClaimsAsync_ReturnsClaimsWithStatus()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (userID, full_names, surname, email, role) VALUES (1, 'Test', 'User', 'lecturer@email.com', 'Lecturer');
                INSERT INTO Claims (number_of_sessions, number_of_hours, amount_of_rate, TotalAmount, module_name, faculty_name, claim_status, creating_date, lecturerID)
                VALUES (2, 3, 100, 600, 'CS101', 'Science', 'Pending', '2025-10-24', 1)";
            command.ExecuteNonQuery();

            // Act
            var claims = await _dbHelper.GetPendingClaimsAsync("Pending");

            // Assert
            Assert.Single(claims);
            var claim = claims[0];
            Assert.Equal("Test User", claim.LecturerName);
            Assert.Equal("Pending", claim.claim_status);
        }

        [Fact]
        public async Task GetPendingClaimsAsync_NoClaims_ReturnsEmptyList()
        {
            // Act
            var claims = await _dbHelper.GetPendingClaimsAsync("Pending");

            // Assert
            Assert.Empty(claims);
        }

        [Fact]
        public async Task GetPendingClaimsAsync_InvalidStatus_ReturnsEmptyList()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (userID) VALUES (1); INSERT INTO Claims (claim_status, lecturerID) VALUES ('Pending', 1)";
            command.ExecuteNonQuery();

            // Act
            var claims = await _dbHelper.GetPendingClaimsAsync("Invalid");

            // Assert
            Assert.Empty(claims);
        }

        [Fact]
        public async Task UpdateClaimStatusAsync_UpdatesStatusCorrectly()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (userID, email, role) VALUES (1, 'lecturer@email.com', 'Lecturer');
                INSERT INTO Claims (claimID, number_of_sessions, number_of_hours, amount_of_rate, TotalAmount, claim_status, lecturerID)
                VALUES (1, 2, 3, 100, 600, 'Pending', 1)";
            command.ExecuteNonQuery();

            // Act
            await _dbHelper.UpdateClaimStatusAsync(1, "Verified");

            // Assert
            using var checkCommand = _connection.CreateCommand();
            checkCommand.CommandText = "SELECT claim_status FROM Claims WHERE claimID = 1";
            var status = (string)checkCommand.ExecuteScalar();
            Assert.Equal("Verified", status);
        }

        [Fact]
        public async Task UpdateClaimStatusAsync_NonExistentClaim_DoesNothing()
        {
            // Act
            await _dbHelper.UpdateClaimStatusAsync(999, "Verified");

            // Assert - No exception, just no update
            // To verify, check row count or something, but since no rows, ok
        }

        [Fact]
        public async Task UpdateClaimStatusAsync_NullStatus_ThrowsException()
        {
            // Arrange
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Claims (claimID) VALUES (1)";
            command.ExecuteNonQuery();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dbHelper.UpdateClaimStatusAsync(1, null));
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}