// UnitTests.cs - Example unit tests for CMCS system using xUnit and Moq
// Assume a test project with references to the main project, xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing, etc.
// For session mocking, use Moq for IHttpContextAccessor or similar.
using CMCS_POE_PART_2.Controllers;
using CMCS_POE_PART_2.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CMCS_POE_PART_1.Tests
{
    public class AccountControllerTests
    {
        private readonly Mock<DbHelper> _dbHelperMock;
        private readonly AccountController _controller;
        private readonly Mock<ISession> _sessionMock;
        private readonly Mock<HttpContext> _httpContextMock;

        public AccountControllerTests()
        {
            _dbHelperMock = new Mock<DbHelper>();
            _sessionMock = new Mock<ISession>();
            _httpContextMock = new Mock<HttpContext>();
            _httpContextMock.Setup(c => c.Session).Returns(_sessionMock.Object);
            _controller = new AccountController
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };
        }

        [Fact]
        public async Task Login_Success_RedirectsToHome()
        {
            var user = new User { userID = 1, password = "hashedpw", role = "Lecturer", full_names = "Test", surname = "User" };
            _dbHelperMock.Setup(db => db.GetUserByEmailAsync("test@email.com")).ReturnsAsync(user);
            // Assume HashPassword returns "hashedpw" for "pw"
            var result = await _controller.Login("test@email.com", "pw");
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsViewWithError()
        {
            _dbHelperMock.Setup(db => db.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);
            var result = await _controller.Login("invalid@email.com", "wrong");
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Invalid credentials. Please register if new.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Register_Success_RedirectsToLogin()
        {
            var user = new User { email = "new@email.com", password = "password123", full_names = "New", surname = "User", role = "Lecturer", gender = "Male", date = DateTime.Today };
            _dbHelperMock.Setup(db => db.GetUserByEmailAsync("new@email.com")).ReturnsAsync((User)null);
            _dbHelperMock.Setup(db => db.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);
            var result = await _controller.Register(user, "password123");
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Registration successful! Please log in.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Register_PasswordMismatch_ReturnsViewWithError()
        {
            var user = new User { email = "new@email.com" };
            var result = await _controller.Register(user, "different");
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("password"));
        }

        // Add more tests for other cases like invalid password, existing email, etc.
    }

    public class ClaimsControllerTests
    {
        private readonly Mock<DbHelper> _dbHelperMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly ClaimsController _controller;
        private readonly Mock<ISession> _sessionMock;
        private readonly Mock<HttpContext> _httpContextMock;

        public ClaimsControllerTests()
        {
            _dbHelperMock = new Mock<DbHelper>();
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(Directory.GetCurrentDirectory());
            _sessionMock = new Mock<ISession>();
            _httpContextMock = new Mock<HttpContext>();
            _httpContextMock.Setup(c => c.Session).Returns(_sessionMock.Object);
            _controller = new ClaimsController(_envMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };
        }

        [Fact]
        public void New_Get_Authorized_ReturnsView()
        {
            _sessionMock.Setup(s => s.GetString("Role")).Returns("Lecturer");
            var result = _controller.New();
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Claim>(viewResult.Model);
        }

        [Fact]
        public void New_Get_Unauthorized_Redirects()
        {
            _sessionMock.Setup(s => s.GetString("Role")).Returns("Other");
            var result = _controller.New();
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task New_Post_ValidClaim_SubmitsAndRedirects()
        {
            _sessionMock.Setup(s => s.GetString("Role")).Returns("Lecturer");
            _sessionMock.Setup(s => s.GetInt32("UserId")).Returns(1);
            var claim = new Claim { number_of_sessions = 2, number_of_hours = 3, amount_of_rate = 100, module_name = "Mod", faculty_name = "Fac" };
            _dbHelperMock.Setup(db => db.CreateClaimAsync(It.IsAny<Claim>())).ReturnsAsync(1);
            var result = await _controller.New(claim, null);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("Claim #1 submitted!", _controller.TempData["Success"].ToString());
        }

        [Fact]
        public async Task New_Post_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError("key", "error");
            var claim = new Claim();
            var result = await _controller.New(claim, null);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(claim, viewResult.Model);
        }

        // Add tests for file uploads, high value flags, exceptions, etc.
    }

    public class ApprovalControllerTests
    {
        private readonly Mock<DbHelper> _dbHelperMock;
        private readonly ApprovalController _controller;
        private readonly Mock<ISession> _sessionMock;
        private readonly Mock<HttpContext> _httpContextMock;

        public ApprovalControllerTests()
        {
            _dbHelperMock = new Mock<DbHelper>();
            _sessionMock = new Mock<ISession>();
            _httpContextMock = new Mock<HttpContext>();
            _httpContextMock.Setup(c => c.Session).Returns(_sessionMock.Object);
            _controller = new ApprovalController
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };
        }

        [Fact]
        public async Task Verify_Get_Coordinator_ReturnsViewWithClaims()
        {
            _sessionMock.Setup(s => s.GetString("Role")).Returns("Coordinator");
            _dbHelperMock.Setup(db => db.GetPendingClaimsAsync("Pending")).ReturnsAsync(new List<Claim> { new Claim() });
            var result = await _controller.Verify();
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<List<Claim>>(viewResult.Model);
        }

        [Fact]
        public async Task Verify_Get_NotCoordinator_Redirects()
        {
            _sessionMock.Setup(s => s.GetString("Role")).Returns("Other");
            var result = await _controller.Verify();
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Verify_Post_UpdatesStatusAndRedirects()
        {
            _sessionMock.Setup(s => s.GetString("Role")).Returns("Coordinator");
            _dbHelperMock.Setup(db => db.UpdateClaimStatusAsync(1, "Verified")).Returns(Task.CompletedTask);
            var result = await _controller.Verify(1);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Verify", redirectResult.ActionName);
            Assert.Equal("Claim verified and sent for approval!", _controller.TempData["Success"]);
        }

        // Similar tests for Approve GET/POST
    }

    // For DbHelper, since it interacts with DB, these would be integration tests, but for unit, mock SqlConnection if possible, but typically use in-memory DB for integration.
    // Example integration test setup could use Microsoft.Data.Sqlite or similar, but for brevity, skip or add simple ones.

    // Error Handling: The existing code has try-catch blocks in controllers, setting TempData["Error"].
    // To enhance, add more specific catches, logging, or global exception handler in Program.cs.
    // For example, in Program.cs, add app.UseExceptionHandler("/Home/Error");
    // Already there if !Development.
    // In views, errors are displayed via TempData in layout.
    // This covers graceful handling and meaningful messages.
}