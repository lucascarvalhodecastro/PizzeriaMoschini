using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaMoschini.Controllers;
using PizzeriaMoschini.Data;
using PizzeriaMoschini.Models;

namespace PizzeriaMoschini.Tests
{
    public class TablesControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly TablesController _controller;

        public TablesControllerTests()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Initialize DbContext with in-memory database
            _context = new ApplicationDbContext(options);

            // Seed in-memory database with test data
            SeedTestData();

            // Initialize TablesController with test DbContext
            _controller = new TablesController(_context);
        }

        // Seed in-memory database with test data
        private void SeedTestData()
        {
            // Clear data to prevent duplicate entries
            _context.Tables.RemoveRange(_context.Tables);

            // Add test data to in-memory database
            var tables = new List<Table>
        {
            new Table { TableID = 1, Capacity = 4 },
            new Table { TableID = 2, Capacity = 2 }
        };

            _context.Tables.AddRange(tables);
            _context.SaveChanges();
        }

        // Test: Index action should return view with list of tables
        [Fact]
        public async Task Index_ReturnsViewWithTables()
        {
            // Act - Call the Index action
            var result = await _controller.Index();

            // Assert - Verify result is ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);

            // Assert - Verify model passed to view is list of tables
            var model = Assert.IsAssignableFrom<IEnumerable<Table>>(viewResult.ViewData.Model);

            // Assert - Verify model contains correct number of tables
            Assert.Equal(2, model.Count());
        }

        // Test: Details action should return view with correct table when given a valid ID
        [Fact]
        public async Task Details_ValidId_ReturnsViewWithTable()
        {
            // Act - Call Details action with valid table ID
            var result = await _controller.Details(1);

            // Assert - Verify result is ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);

            // Assert - Verify model passed to view is table with expected ID
            var model = Assert.IsAssignableFrom<Table>(viewResult.ViewData.Model);
            Assert.Equal(1, model.TableID);
        }

        // Test: Details action should return NotFound when given invalid ID
        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            // Act - Call Details action with invalid ID
            var result = await _controller.Details(99);

            // Assert - Verify result is NotFoundResult
            Assert.IsType<NotFoundResult>(result);
        }

        // Test: Create should redirect to Index when model is valid and table is created
        [Fact]
        public async Task Create_ValidModel_RedirectsToIndex()
        {
            // Arrange - Create new table model to add
            var table = new Table { TableID = 3, Capacity = 6 };

            // Act - Call Create with valid table model
            var result = await _controller.Create(table);

            // Assert - Verify result is RedirectToActionResult
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName); 

            // Assert - Verify table was added to database
            Assert.Equal(3, _context.Tables.Count());
        }

        // Test: Create should return view when model state is invalid
        [Fact]
        public async Task Create_InvalidModel_ReturnsView()
        {
            // Arrange - Simulate invalid model state by adding error
            _controller.ModelState.AddModelError("Capacity", "Required");

            var table = new Table { TableID = 4 };

            // Act - Call Create with invalid model
            var result = await _controller.Create(table);

            // Assert - Verify result is ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);

            // Assert - Verify returned model is same as input model
            Assert.Equal(table, viewResult.ViewData.Model);
        }

        // Test: Edit should update table and redirect to Index when model is valid
        [Fact]
        public async Task Edit_ValidModel_RedirectsToIndex()
        {
            // Arrange - Retrieve table and modify capacity
            var table = await _context.Tables.FindAsync(1);
            table.Capacity = 6;

            // Act - Call Edit with valid table model
            var result = await _controller.Edit(1, table);

            // Assert - Verify result is RedirectToActionResult
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            // Assert - Verify table capacity was updated in database
            var updatedTable = await _context.Tables.FindAsync(1);
            Assert.Equal(6, updatedTable.Capacity);
        }

        // Test: Edit should return view and not update database when model state is invalid
        [Fact]
        public async Task Edit_InvalidModelState_ReturnsView()
        {
            // Arrange - Retrieve table and simulate invalid model state
            var table = new Table { TableID = 1, Capacity = 4 };
            _controller.ModelState.AddModelError("Capacity", "Required");

            // Act - Call Edit with invalid model
            var result = await _controller.Edit(1, table);

            // Assert - Verify result is ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);

            // Assert - Verify model returned view is the same as input model
            var model = Assert.IsAssignableFrom<Table>(viewResult.ViewData.Model);
            Assert.Equal(table, model);

            // Assert - Verify table capacity was not updated in database
            var unchangedTable = await _context.Tables.FindAsync(1);
            Assert.Equal(4, unchangedTable.Capacity);
        }

        // Test: DeleteConfirmed should remove table from database and redirect to Index when given valid ID
        [Fact]
        public async Task DeleteConfirmed_ValidId_RedirectsToIndex()
        {
            // Act - Call DeleteConfirmed with valid table ID
            var result = await _controller.DeleteConfirmed(1);

            // Assert - Verify result is RedirectToActionResult
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            // Assert - Verify table was removed from database
            Assert.Equal(1, _context.Tables.Count());
        }

        // Test: DeleteConfirmed should not remove any table and should still redirect to Index when given invalid ID
        [Fact]
        public async Task DeleteConfirmed_InvalidId_RedirectsToIndex()
        {
            // Act - Call DeleteConfirmed with invalid table ID
            var result = await _controller.DeleteConfirmed(99);

            // Assert - Verify result is RedirectToActionResult
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }
    }
}