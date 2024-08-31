using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PizzeriaMoschini.Controllers;
using PizzeriaMoschini.Data;
using PizzeriaMoschini.Models;

namespace PizzeriaMoschini.Tests
{
    public class CustomersControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly CustomersController _controller;

        // Set up in-memory database and mock dependencies
        public CustomersControllerTests()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

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

            // Initialize CustomersController with mock services and in-memory database
            _controller = new CustomersController(_context, _userManagerMock.Object, _roleManagerMock.Object);
        }

        private void SeedTestData()
        {
            // Clear data to prevent duplicates
            _context.Customers.RemoveRange(_context.Customers);

            // Add test data to in-memory database
            var customers = new List<Customer>
        {
            new Customer { CustomerID = 1, Name = "John Smith", Email = "john@gmail.com", Phone = "083123456" },
            new Customer { CustomerID = 2, Name = "Mary Smith", Email = "mary@gmail.com", Phone = "083456123" }
        };

            _context.Customers.AddRange(customers);
            _context.SaveChanges();
        }

        // Test: Index action should return a view with a list of customers
        [Fact]
        public async Task Index_ReturnsViewWithCustomers()
        {
            // Arrange - Set up RoleManager to return Staff role and UserManager to return no users in Staff role
            var staffRole = new IdentityRole { Name = "Staff" };
            _roleManagerMock.Setup(r => r.FindByNameAsync("Staff")).ReturnsAsync(staffRole);
            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Staff")).ReturnsAsync(new List<IdentityUser>());

            // Act - Call Index
            var result = await _controller.Index();

            // Assert - Verify the model contains the correct number of customers
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Customer>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
        }

        // Test: Details action should return a view with the correct customer when given a valid ID
        [Fact]
        public async Task Details_ValidId_ReturnsViewWithCustomer()
        {
            // Act - Call the Details action with a valid customer ID
            var result = await _controller.Details(1);

            // Assert - Verify the model passed to the view is the expected customer
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Customer>(viewResult.ViewData.Model);
            Assert.Equal(1, model.CustomerID);
        }

        // Test: Details action should return NotFound when given an invalid ID
        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            // Act - Call Details action with invalid customer ID
            var result = await _controller.Details(99);

            // Assert - Verify result is NotFoundResult
            Assert.IsType<NotFoundResult>(result);
        }

        // Test: Create action (GET) should return the view when a new customer is being created
        [Fact]
        public async Task Create_Get_ReturnsView()
        {
            // Arrange - Set up UserManager to return logged-in user
            var user = new IdentityUser { Email = "user@gmail.com" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);

            // Act - Call Create
            var result = await _controller.Create();

            // Assert - Verify the model passed to the view is a new Customer with the user's email
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Customer>(viewResult.ViewData.Model);
            Assert.Equal(user.Email, model.Email);
        }

        // Test: Create action (POST) should redirect to Reservations/Create when model is valid
        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToReservationsCreate()
        {
            // Arrange - Set up UserManager to return a logged-in user and prepare a valid customer model
            var user = new IdentityUser { Email = "john@gmail.com" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);

            var customer = new Customer
            {
                CustomerID = 3,
                Name = "John Smith",
                Email = "john@gmail.com",
                Phone = "083123456"
            };

            // Act - Call Create with valid customer model
            var result = await _controller.Create(customer);

            // Assert - Verify the customer was added to the database
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Create", redirectToActionResult.ActionName);
            Assert.Equal("Reservations", redirectToActionResult.ControllerName);
            Assert.Equal(3, _context.Customers.Count());
        }

        // Test: Create action (POST) should return the view when the model state is invalid
        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange - Set up UserManager to return a logged-in user and prepare an invalid customer model
            var user = new IdentityUser { Email = "john@gmail.com" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);

            // Simulate invalid model state by adding an error
            _controller.ModelState.AddModelError("Name", "Required");

            var customer = new Customer
            {
                CustomerID = 3,
                Email = "john@gmail.com",
                Phone = "083123456"
            };

            // Act - Call Create with invalid customer model
            var result = await _controller.Create(customer);

            // Assert - Verify the returned model is the same as the input model
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(customer, viewResult.ViewData.Model);
        }

        // Test: Edit action should update the customer and redirect to Index when the model is valid
        [Fact]
        public async Task Edit_ValidModel_RedirectsToIndex()
        {
            // Arrange - Retrieve a customer and modify their name
            var customer = await _context.Customers.FindAsync(1);
            customer.Name = "Updated Name";

            // Act - Call Edit with the valid customer model
            var result = await _controller.Edit(1, customer);

            // Assert - Verify the customer's name was updated in the database
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            var updatedCustomer = await _context.Customers.FindAsync(1);
            Assert.Equal("Updated Name", updatedCustomer.Name);
        }

        // Test: Edit action should return the view and not update the database when the model state is invalid
        [Fact]
        public async Task Edit_InvalidModelState_ReturnsView()
        {
            // Arrange - Prepare a customer model and simulate an invalid model state
            var customer = new Customer
            {
                CustomerID = 1,
                Name = "John Smith",
                Email = "john@gmail.com",
                Phone = "083123456"
            };
            _controller.ModelState.AddModelError("Email", "Required");

            // Act - Call Edit with invalid customer model
            var result = await _controller.Edit(1, customer);

            // Assert - Verify the returned model is the same as the input model
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(customer, viewResult.ViewData.Model);
        }

        // Test: DeleteConfirmed action should remove the customer and redirect to Index when given a valid ID
        [Fact]
        public async Task DeleteConfirmed_ValidId_RedirectsToIndex()
        {
            // Act - Call the DeleteConfirmed action with a valid customer ID
            var result = await _controller.DeleteConfirmed(1);

            // Assert - Verify the customer was removed from the database
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Single(_context.Customers);
        }

        // Test: DeleteConfirmed action should still redirect to Index when given an invalid ID
        [Fact]
        public async Task DeleteConfirmed_InvalidId_RedirectsToIndex()
        {
            // Act - Call DeleteConfirmed with an invalid customer ID
            var result = await _controller.DeleteConfirmed(99);

            // Assert - Verify the result is a RedirectToActionResult
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }
    }
}