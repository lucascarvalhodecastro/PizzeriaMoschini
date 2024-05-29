using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PizzeriaMoschini.Data;
using PizzeriaMoschini.Models;

namespace PizzeriaMoschini.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReservationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            IQueryable<Reservation> reservations;

            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (isAdmin)
            {
                reservations = _context.Reservations.Include(r => r.Customer).Include(r => r.Table);
            }
            else
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);

                if (customer == null)
                {
                    return NotFound("Customer not found.");
                }

                reservations = _context.Reservations
                    .Include(r => r.Customer)
                    .Include(r => r.Table)
                    .Where(r => r.CustomerID == customer.CustomerID);
            }

            reservations = reservations.OrderBy(r => r.ReservationDate).ThenBy(r => r.TimeSlot);

            ViewData["IsAdmin"] = isAdmin;

            return View(await reservations.ToListAsync());
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .FirstOrDefaultAsync(m => m.ReservationID == id);
            if (reservation == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            ViewData["IsAdmin"] = isAdmin;

            return View(reservation);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (customer == null)
            {
                return RedirectToAction("Create", "Customers");
            }

            ViewData["CustomerID"] = customer.CustomerID;
            ViewData["TimeSlots"] = new SelectList(new[] { "19:00", "20:00", "21:00" });

            return View();
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerID,ReservationDate,TimeSlot,NumberOfGuests")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                var availableTable = await CheckTableAvailability(reservation.ReservationDate, reservation.TimeSlot, reservation.NumberOfGuests);

                if (availableTable != null)
                {
                    reservation.TableID = availableTable.TableID;
                    _context.Add(reservation);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "No available table for the selected date and time slot. Please choose another time slot or date.");
                }
            }

            ViewData["CustomerID"] = reservation.CustomerID;
            ViewData["TimeSlots"] = new SelectList(new[] { "19:00", "20:00", "21:00" });

            return View(reservation);
        }

        private async Task<Table> CheckTableAvailability(DateTime date, string timeSlot, int numberOfGuests)
        {
            var tables = _context.Tables.Where(t => t.Capacity >= numberOfGuests).ToList();

            foreach (var table in tables)
            {
                var reservationCount = await _context.Reservations
                    .Where(r => r.TableID == table.TableID && r.ReservationDate == date && r.TimeSlot == timeSlot)
                    .CountAsync();

                if (reservationCount == 0)
                {
                    return table;
                }
            }

            return null;
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            ViewData["CustomerID"] = new SelectList(_context.Customers, "CustomerID", "Name", reservation.CustomerID);
            ViewData["TableID"] = new SelectList(_context.Tables, "TableID", "TableID", reservation.TableID);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationID,CustomerID,TableID,ReservationDate,TimeSlot,NumberOfGuests")] Reservation reservation)
        {
            if (id != reservation.ReservationID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.ReservationID))
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
            ViewData["CustomerID"] = new SelectList(_context.Customers, "CustomerID", "Name", reservation.CustomerID);
            ViewData["TableID"] = new SelectList(_context.Tables, "TableID", "TableID", reservation.TableID);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .FirstOrDefaultAsync(m => m.ReservationID == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.ReservationID == id);
        }
    }
}
