using CMCS_POE_PART_2.Models;
using System;
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

            // Inject connection string into DbHelper using reflection
            _dbHelper = DbHelper.Instance;
            var connStringField = typeof(DbHelper).GetField("_connString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            connStringField?.SetValue(_dbHelper, _connection.ConnectionString);
        }

        private void CreateSchema()
        {
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

        // ? User Tests

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
            Assert.Equal("Lecturer", user.role);
        }

        [Fact]
        public async Task GetUserByEmailAsync_NonExistingUser_ReturnsNull()
        {
            var user = await _dbHelper.GetUserByEmailAsync("missing@email.com");
            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserByEmailAsync_EmptyEmail_ReturnsNull()
        {
            var user = await _dbHelper.GetUserByEmailAsync("");
            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserByEmailAsync_NullEmail_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dbHelper.GetUserByEmailAsync(null));
        }

        [Fact]
        public async Task CreateUserAsync_ValidUser_ReturnsUserId()
        {
            var user = new User
            {
                full_names = "New",
                surname = "User",
                email = "new@email.com",
                role = "Lecturer",
                gender = "Female",
                password = "hashedpw",
                date = DateTime.Today
            };

            var userId = await _dbHelper.CreateUserAsync(user);
            Assert.True(userId > 0);
        }

        [Fact]
        public async Task CreateUserAsync_MinimalFields_SucceedsWithNulls()
        {
            var user = new User
            {
                email = "minimal@email.com",
                role = "Lecturer",
                gender = "Other",
                password = "hashedpw",
                date = DateTime.Today
            };

            var userId = await _dbHelper.CreateUserAsync(user);
            Assert.True(userId > 0);
        }

        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ThrowsException()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (email) VALUES ('dup@email.com')";
            command.ExecuteNonQuery();

            var user = new User { email = "dup@email.com", role = "Lecturer", gender = "Other", password = "hashedpw", date = DateTime.Today };
            await Assert.ThrowsAsync<SQLiteException>(() => _dbHelper.CreateUserAsync(user));
        }

        [Fact]
        public async Task CreateUserAsync_NullUser_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dbHelper.CreateUserAsync(null));
        }

        // ? Claim Tests

        [Fact]
        public async Task CreateClaimAsync_ValidClaim_ReturnsClaimId()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (userID, email, role) VALUES (1, 'lecturer@email.com', 'Lecturer')";
            command.ExecuteNonQuery();

            var claim = new Claim
            {
                number_of_sessions = 2,
                number_of_hours = 3,
                amount_of_rate = 100,
                TotalAmount = 600,
                module_name = "CS101",
                faculty_name = "Science",
                supporting_documents = "doc1.pdf",
                claim_status = "Pending",
                creating_date = DateTime.Today,
                lecturerID = 1
            };

            var claimId = await _dbHelper.CreateClaimAsync(claim);
            Assert.True(claimId > 0);
        }

        [Fact]
        public async Task CreateClaimAsync_ZeroValues_Succeeds()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (userID) VALUES (1)";
            command.ExecuteNonQuery();

            var claim = new Claim
            {
                number_of_sessions = 0,
                number_of_hours = 0,
                amount_of_rate = 0,
                TotalAmount = 0,
                module_name = "",
                faculty_name = "",
                claim_status = "Pending",
                creating_date = DateTime.Today,
                lecturerID = 1
            };

            var claimId = await _dbHelper.CreateClaimAsync(claim);
            Assert.True(claimId > 0);
        }

        [Fact]
        public async Task CreateClaimAsync_InvalidLecturerId_ThrowsException()
        {
            var claim = new Claim
            {
                number_of_sessions = 2,
                number_of_hours = 3,
                amount_of_rate = 100,
                lecturerID = 999
            };

            await Assert.ThrowsAsync<SQLiteException>(() => _dbHelper.CreateClaimAsync(claim));
        }

        [Fact]
        public async Task CreateClaimAsync_NullClaim_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dbHelper.CreateClaimAsync(null));
        }

        [Fact]
        public async Task GetClaimsByLecturerAsync_ReturnsCorrectClaims()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (userID, email, role) VALUES (1, 'lecturer@email.com', 'Lecturer');
                INSERT INTO Claims (number_of_sessions, number_of_hours, amount_of_rate, TotalAmount, module_name, faculty_name, claim_status, creating_date, lecturerID)
                VALUES (2, 3, 100, 600, 'CS101', 'Science', 'Pending', '2025-10-24', 1)";
            command.ExecuteNonQuery();

            var claims = await _dbHelper.GetClaimsByLecturerAsync(1);
            Assert.Single(claims);
            Assert.Equal("CS101", claims[0].module_name);
        }

        [Fact]
        public async Task GetClaimsByLecturerAsync_WithStatusFilter_ReturnsFilteredClaims()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (userID) VALUES (1);
                INSERT INTO Claims (claim_status, lecturerID) VALUES ('Pending', 1);
                INSERT INTO Claims (claim_status, lecturerID) VALUES ('Verified', 1)";
            command.ExecuteNonQuery();

            var claims = await _dbHelper.GetClaimsByLecturerAsync(1, "Pending");
            Assert.Single(claims);
            Assert.Equal("Pending", claims[0].claim_status);
        }

        [Fact]
        public async Task UpdateClaimStatusAsync_UpdatesStatusCorrectly()
        {
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

            // Assert: No exception thrown, no update performed
            // You can optionally check affected rows if needed
        }

        [Fact]
        public async Task UpdateClaimStatusAsync_NullStatus_ThrowsException()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Claims (claimID) VALUES (1)";
            command.ExecuteNonQuery();

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dbHelper.UpdateClaimStatusAsync(1, null));
        }

        [Fact]
        public async Task GetPendingClaimsAsync_ReturnsClaimsWithStatus()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (userID, full_names, surname, email, role) VALUES (1, 'Test', 'User', 'lecturer@email.com', 'Lecturer');
                INSERT INTO Claims (number_of_sessions, number_of_hours, amount_of_rate, TotalAmount, module_name, faculty_name, claim_status, creating_date, lecturerID)
                VALUES (2, 3, 100, 600, 'CS101', 'Science', 'Pending', '2025-10-24', 1)";
            command.ExecuteNonQuery();

            var claims = await _dbHelper.GetPendingClaimsAsync("Pending");

            Assert.Single(claims);
            Assert.Equal("Test User", claims[0].LecturerName);
            Assert.Equal("Pending", claims[0].claim_status);
        }

        [Fact]
        public async Task GetPendingClaimsAsync_NoClaims_ReturnsEmptyList()
        {
            var claims = await _dbHelper.GetPendingClaimsAsync("Pending");
            Assert.Empty(claims);
        }

        [Fact]
        public async Task GetPendingClaimsAsync_InvalidStatus_ReturnsEmptyList()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (userID) VALUES (1);
                INSERT INTO Claims (claim_status, lecturerID) VALUES ('Pending', 1)";
            command.ExecuteNonQuery();

            var claims = await _dbHelper.GetPendingClaimsAsync("Invalid");
            Assert.Empty(claims);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
