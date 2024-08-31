using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PizzeriaMoschini.Controllers;
using PizzeriaMoschini.Data;
using PizzeriaMoschini.Models;

namespace PizzeriaMoschini.Tests
{
    public class ReservationsControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly ReservationsController _controller;

        // Set up in-memory database and mock dependencies
        public ReservationsControllerTests()
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

            // Mock IEmailSender
            _emailSenderMock = new Mock<IEmailSender>();

            // Seed in-memory database with test data
            SeedTestData();

            // Initialize ReservationsController with mock services and in-memory database
            _controller = new ReservationsController(_context, _userManagerMock.Object, _emailSenderMock.Object);
        }

        // Seed in-memory database with test data
        private void SeedTestData()
        {
            _context.Reservations.RemoveRange(_context.Reservations);
            _context.Tables.RemoveRange(_context.Tables);
            _context.Customers.RemoveRange(_context.Customers);

            var customer = new Customer
            {
                CustomerID = 1,
                Name = "John Smith",
                Email = "john@gmail.com",
                Phone = "083123456"
            };

            var table = new Table
            {
                TableID = 1,
                Capacity = 4
            };

            var reservation = new Reservation
            {
                ReservationID = 1,
                CustomerID = 1,
                TableID = 1,
                ReservationDate = System.DateTime.Today.AddDays(1),
                TimeSlot = "19:00",
                NumberOfGuests = 4,
                Customer = customer,
                Table = table
            };

            _context.Customers.Add(customer);
            _context.Tables.Add(table);
            _context.Reservations.Add(reservation);
            _context.SaveChanges();
        }

        // Test: Index action should return a view with a list of reservations for Admin or Staff
        [Fact]
        public async Task Index_ReturnsViewWithReservations_ForAdminOrStaff()
        {
            // Arrange - Set up UserManager to return an Admin user with "Admin" role
            var user = new IdentityUser { Email = "admin@admin.com" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            // Act - Call the Index action
            var result = await _controller.Index();

            // Assert - Verify the model contains the correct number of reservations
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Reservation>>(viewResult.ViewData.Model);
            Assert.Single(model); 
        }

        // Test: Index action should return a view with a list of reservations for a customer
        [Fact]
        public async Task Index_ReturnsViewWithReservations_ForCustomer()
        {
            // Arrange - Set up UserManager to return a Customer user with Customer role
            var user = new IdentityUser { Email = "john@gmail.com" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Customer" });

            // Act - Call the Index action
            var result = await _controller.Index();

            // Assert - Verify the model contains the correct number of reservations
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Reservation>>(viewResult.ViewData.Model);
            Assert.Single(model); 
        }

        // Test: Details action should return a view with the correct reservation when given a valid ID
        [Fact]
        public async Task Details_ValidId_ReturnsViewWithReservation()
        {
            // Act - Call the Details action with a valid reservation ID
            var result = await _controller.Details(1);

            // Assert - Verify the result is a ViewResult and the model is the correct reservation
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Reservation>(viewResult.ViewData.Model);
            Assert.Equal(1, model.ReservationID);
        }

        // Test: Details action should return NotFound when given an invalid ID
        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            // Act - Call the Details action with an invalid reservation ID
            var result = await _controller.Details(99);

            // Assert - Verify the result is a NotFoundResult
            Assert.IsType<NotFoundResult>(result);
        }

        // Test: Create action (POST) should redirect to Index when model is valid
        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange - Set up UserManager to return a logged-in user and prepare a valid reservation model
            var user = new IdentityUser { Email = "john@gmail.com" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);

            var reservation = new Reservation
            {
                ReservationID = 2,
                CustomerID = 1,
                TableID = 1,
                ReservationDate = System.DateTime.Today.AddDays(1),
                TimeSlot = "20:00",
                NumberOfGuests = 4
            };

            // Act - Call the Create action with the valid reservation model
            var result = await _controller.Create(reservation);

            // Assert - Verify the reservation was added to the database
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(2, _context.Reservations.Count());
        }

        // Test: Create action (POST) should return the view when the model state is invalid
        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange - Set up UserManager to return a logged-in user and prepare an invalid reservation model
            var user = new IdentityUser { Email = "john@gmail.com" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);

            _controller.ModelState.AddModelError("TimeSlot", "Required");

            var reservation = new Reservation
            {
                ReservationID = 2,
                CustomerID = 1,
                TableID = 1,
                ReservationDate = System.DateTime.Today.AddDays(1),
                NumberOfGuests = 4
            };

            // Act - Call the Create action with the invalid reservation model
            var result = await _controller.Create(reservation);

            // Assert - Verify the returned model is the same as the input model
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(reservation, viewResult.ViewData.Model);
        }

        // Test: Edit action should update the reservation and redirect to Index when the model is valid
        [Fact]
        public async Task Edit_ValidModel_RedirectsToIndex()
        {
            // Arrange - Retrieve a reservation and modify the number of guests
            var reservation = await _context.Reservations.FindAsync(1);
            reservation.NumberOfGuests = 5;

            // Act - Call the Edit action with the valid reservation model
            var result = await _controller.Edit(1, reservation);

            // Assert - Verify the reservation number of guests was updated in the database
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            var updatedReservation = await _context.Reservations.FindAsync(1);
            Assert.Equal(5, updatedReservation.NumberOfGuests);
        }

        // Test: Edit action should return the view and not update the database when the model state is invalid
        [Fact]
        public async Task Edit_InvalidModelState_ReturnsView()
        {
            // Arrange - Prepare a reservation model and simulate an invalid model state
            var reservation = new Reservation
            {
                ReservationID = 1,
                CustomerID = 1,
                TableID = 1,
                ReservationDate = System.DateTime.Today.AddDays(1),
                NumberOfGuests = 4
            };
            _controller.ModelState.AddModelError("TimeSlot", "Required");

            // Act - Call the Edit action with the invalid reservation model
            var result = await _controller.Edit(1, reservation);

            // Assert - Verify the returned model is the same as the input model
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(reservation, viewResult.ViewData.Model);
        }

        // Test: DeleteConfirmed action should remove the reservation and redirect to Index when given a valid ID
        [Fact]
        public async Task DeleteConfirmed_ValidId_RedirectsToIndex()
        {
            // Act - Call the DeleteConfirmed action with a valid reservation ID
            var result = await _controller.DeleteConfirmed(1);

            // Assert - Verify the reservation was removed from the database
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Empty(_context.Reservations);
        }

        // Test: DeleteConfirmed action should still redirect to Index when given an invalid ID
        [Fact]
        public async Task DeleteConfirmed_InvalidId_RedirectsToIndex()
        {
            // Act - Call the DeleteConfirmed action with an invalid reservation ID
            var result = await _controller.DeleteConfirmed(99);

            // Assert - Verify the result is a RedirectToActionResult
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }
    }
}
