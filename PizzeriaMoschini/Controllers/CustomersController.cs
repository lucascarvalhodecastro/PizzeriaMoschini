using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PizzeriaMoschini.Data;
using PizzeriaMoschini.Models;

namespace PizzeriaMoschini.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Dependency injection for accessing user management services
        private readonly UserManager<IdentityUser> _userManager;

        // Dependency injection for accessing role management services
        private readonly RoleManager<IdentityRole> _roleManager;

        // Initialize _context, _userManager and _roleManager fields with injected services
        public CustomersController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Customers
        // Ensure only Admin and Staff can access Index view
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Index()
        {
            // Get all Staff and Admin users
            var staffRole = await _roleManager.FindByNameAsync("Staff");
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            var staffUsers = await _userManager.GetUsersInRoleAsync(staffRole.Name);
            var adminUsers = await _userManager.GetUsersInRoleAsync(adminRole.Name);

            // Get the emails of all staff and admin users
            var excludedEmails = staffUsers.Select(s => s.Email).Union(adminUsers.Select(a => a.Email));

            // Order customers by name alphabetically and exclude staff and admin users
            var customers = await _context.Customers
                .Where(c => !excludedEmails.Contains(c.Email))
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerID == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public async Task<IActionResult> Create()
        {
            // Get logged in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Redirect to login if user is not logged-in
                return Redirect("/Identity/Account/Login");
            }

            // Check if the customer exists in database
            var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (existingCustomer != null)
            {
                // If the customer exists, redirect to reservation creation view
                return RedirectToAction("Create", "Reservations");
            }

            // Create new customer with user's email
            var customer = new Customer
            {
                Email = user.Email
            };

            // Return view with new customer
            return View(customer);
        }

        // POST: Customers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerID,Name,Phone,Email")] Customer customer)
        {
            // Get logged in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Redirect to login if user is not logged-in
                return Redirect("/Identity/Account/Login");
            }

            // Associate Customer with logged-in user
            customer.Email = user.Email;

            if (ModelState.IsValid)
            {
                // Add new customer to database
                _context.Add(customer);
                await _context.SaveChangesAsync();

                // Redirect to reservation creation view
                return RedirectToAction("Create", "Reservations");
            }

            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerID,Name,Phone,Email")] Customer customer)
        {
            if (id != customer.CustomerID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerID == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer != null)
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(customer.Email);

                if (user != null)
                {
                    // Delete user
                    var result = await _userManager.DeleteAsync(user);

                    if (!result.Succeeded)
                    {
                        return View(customer);
                    }
                }

                // Remove Customer from database
                _context.Customers.Remove(customer);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerID == id);
        }
    }
}
