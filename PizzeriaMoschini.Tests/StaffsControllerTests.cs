using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PizzeriaMoschini.Controllers;
using PizzeriaMoschini.Data;
using PizzeriaMoschini.Models;

namespace PizzeriaMoschini.Tests
{
    public class StaffsControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly StaffsController _controller;

        public StaffsControllerTests()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Initialize DbContext with in-memory database
            _context = new ApplicationDbContext(options);

            // Mock UserManager
            _userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(),
                null, null, null, null, null, null, null, null);

            // Mock RoleManager
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null, null, null, null);

            // Seed in-memory database with test data
            SeedTestData();

            // Initialize StaffsController with mock services and in-memory database
            _controller = new StaffsController(_userManagerMock.Object, _roleManagerMock.Object, _context);
        }

        // Seed in-memory database with test data
        private void SeedTestData()
        {
            // Clear any existing data to prevent conflicts
            _context.Staffs.RemoveRange(_context.Staffs);

            // Add test data to in-memory database
            var staffs = new List<Staff>
            {
                new Staff { StaffID = 1, Name = "John Smith", Email = "john@example.com" },
                new Staff { StaffID = 2, Name = "Jane Doe", Email = "jane@example.com" }
            };

            _context.Staffs.AddRange(staffs);
            _context.SaveChanges();
        }

        // Test: Index action should return view with list of staff members
        [Fact]
        public async Task Index_ReturnsViewWithStaffs()
        {
            // Arrange - Set up RoleManager to return Staff role and UserManager to return users in Staff role
            var staffRole = new IdentityRole("Staff");
            _roleManagerMock.Setup(rm => rm.FindByNameAsync("Staff")).ReturnsAsync(staffRole);
            _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Staff")).ReturnsAsync(new List<IdentityUser>
            {
                new IdentityUser { Email = "john@example.com" },
                new IdentityUser { Email = "jane@example.com" }
            });

            // Act - Call Index action
            var result = await _controller.Index();

            // Assert - Verify result is ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);

            // Assert - Verify model passed to view is list of staff members
            var model = Assert.IsAssignableFrom<IEnumerable<Staff>>(viewResult.ViewData.Model);

            // Assert - Verify model contains correct number of staff members
            Assert.Equal(2, model.Count()); 
        }

        // Test: Details action should return view with correct staff when given valid ID
        [Fact]
        public async Task Details_ValidId_ReturnsViewWithStaff()
        {
            // Act - Call Details action with valid staff ID
            var result = await _controller.Details(1);

            // Assert - Verify result is ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);

            // Assert - Verify model passed to view is expected staff member
            var model = Assert.IsAssignableFrom<Staff>(viewResult.ViewData.Model);
            Assert.Equal(1, model.StaffID);
        }

        // Test: Details action should return NotFound when given invalid ID
        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            // Act - Call Details action with invalid staff ID
            var result = await _controller.Details(99);

            // Assert - Verify result is NotFoundResult
            Assert.IsType<NotFoundResult>(result);
        }

        // Test: Create action (POST) should redirect to Index when model is valid
        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange - Set up a valid CreateStaffViewModel
            var model = new CreateStaffViewModel
            {
                Name = "New Staff",
                Email = "newstaff@example.com",
                Password = "Test1234!"
            };

            // Set up UserManager to simulate successful user creation
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<IdentityUser>(), model.Password))
                            .ReturnsAsync(IdentityResult.Success);

            // Set up RoleManager to simulate that the role Staff exists
            _roleManagerMock.Setup(rm => rm.RoleExistsAsync("Staff")).ReturnsAsync(true);

            // Act - Call Create action with valid model
            var result = await _controller.Create(model);

            // Assert - Verify the result is a RedirectToActionResult
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            // Assert - Verify the staff was added to the database
            Assert.Equal(3, _context.Staffs.Count()); 
        }

        // Test: Create action (POST) should return the view when the model state is invalid
        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange - Set up an invalid CreateStaffViewModel with an invalid email
            var model = new CreateStaffViewModel
            {
                Name = "New Staff",
                Email = "invalidemail",
                Password = "Test1234!"
            };

            // Arrange - Simulate invalid model state by adding an error
            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act - Call the Create action with the invalid model
            var result = await _controller.Create(model);

            // Assert - Verify the result is a ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);

            // Assert - Verify the returned model is the same as the input model
            Assert.Equal(model, viewResult.ViewData.Model);
        }

        // Test: Edit action should update the staff and redirect to Index when the model is valid
        [Fact]
        public async Task Edit_ValidModel_RedirectsToIndex()
        {
            // Arrange - Retrieve a staff member and modify their name
            var staff = await _context.Staffs.FindAsync(1);
            staff.Name = "Updated Name";

            // Act - Call the Edit action with the valid staff model
            var result = await _controller.Edit(1, staff);

            // Assert - Verify the result is a RedirectToActionResult
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            // Assert - Verify the staff's name was updated in the database
            var updatedStaff = await _context.Staffs.FindAsync(1);
            Assert.Equal("Updated Name", updatedStaff.Name);
        }

        // Test: Edit action should return the view and not update the database when the model state is invalid
        [Fact]
        public async Task Edit_InvalidModelState_ReturnsView()
        {
            // Arrange - Retrieve a staff member and set up an invalid model state
            var staff = new Staff { StaffID = 1, Name = "Updated Name", Email = "john@example.com" };
            _controller.ModelState.AddModelError("Name", "Required");

            // Assert - Verify the result is a ViewResult
            var result = await _controller.Edit(1, staff);

            // Assert - Verify the returned model is the same as the input model
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(staff, viewResult.ViewData.Model);

            // Assert - Verify the staff's name was not updated in the database
            var unchangedStaff = await _context.Staffs.FindAsync(1);
            Assert.Equal("John Smith", unchangedStaff.Name);
        }

        // Test: DeleteConfirmed should remove the staff and redirect to Index when given a valid ID
        [Fact]
        public async Task DeleteConfirmed_ValidId_RedirectsToIndex()
        {
            // Arrange - Capture the initial number of staff members
            var initialStaffCount = _context.Staffs.Count();

            // Act - Call the DeleteConfirmed with a valid staff ID
            var result = await _controller.DeleteConfirmed(1);

            // Assert - Verify the staff was removed from the database
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(initialStaffCount - 1, _context.Staffs.Count());
        }

        // Test: DeleteConfirmed should not remove any staff and should still redirect to Index when given an invalid ID
        [Fact]
        public async Task DeleteConfirmed_InvalidId_RedirectsToIndex()
        {
            // Arrange - Capture the initial number of staff members
            var initialStaffCount = _context.Staffs.Count();

            // Act - Call DeleteConfirmed with an invalid staff ID
            var result = await _controller.DeleteConfirmed(99);

            // Assert - Verify no staff was removed from the database
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(initialStaffCount, _context.Staffs.Count()); 
        }
    }
}
