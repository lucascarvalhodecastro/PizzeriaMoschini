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
    // Ensure only Admin can access StaffsController
    [Authorize(Roles = "Admin")]
    public class StaffsController : Controller
    {
        // Dependency injection for accessing user and role management services
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        // Initialize _userManager, _roleManager and _context fields with injected services
        public StaffsController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Staffs
        public async Task<IActionResult> Index()
        {
            var staffRole = await _roleManager.FindByNameAsync("Staff");
            var usersInStaffRole = await _userManager.GetUsersInRoleAsync(staffRole.Name);
            var staffList = _context.Staffs.Where(s => usersInStaffRole.Select(u => u.Email).Contains(s.Email)).ToList();
            return View(staffList);
        }

        // GET: Staffs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(m => m.StaffID == id);
            if (staff == null)
            {
                return NotFound();
            }

            return View(staff);
        }

        // GET: Staffs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Staffs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create a new IdentityUser with provided email and password
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Ensure Staff role exists and add new user to Staff role
                    if (!await _roleManager.RoleExistsAsync("Staff"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Staff"));
                    }

                    await _userManager.AddToRoleAsync(user, "Staff");

                    // Create new Staff entity and save it to database
                    var staff = new Staff { Name = model.Name, Email = model.Email, Password = model.Password };

                    _context.Add(staff);

                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "Home");
                }
            }

            return View(model);
        }

        // GET: Staffs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }
            return View(staff);
        }

        // POST: Staffs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StaffID,Name,Email")] Staff staff)
        {
            if (id != staff.StaffID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(staff);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StaffExists(staff.StaffID))
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
            return View(staff);
        }

        // GET: Staffs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(m => m.StaffID == id);
            if (staff == null)
            {
                return NotFound();
            }

            return View(staff);
        }

        // POST: Staffs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);

            if (staff != null)
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(staff.Email);

                if (user != null)
                {
                    // Delete user
                    var result = await _userManager.DeleteAsync(user);

                    if (!result.Succeeded)
                    {
                        return View(staff);
                    }
                }

                // Remove Staff from database
                _context.Staffs.Remove(staff);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool StaffExists(int id)
        {
            return _context.Staffs.Any(e => e.StaffID == id);
        }
    }
}
