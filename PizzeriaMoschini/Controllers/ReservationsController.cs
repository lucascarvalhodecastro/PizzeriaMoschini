using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PizzeriaMoschini.Data;
using PizzeriaMoschini.Models;
using PizzeriaMoschini.Services;

namespace PizzeriaMoschini.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Dependency injection for accessing user management and email services
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EmailService _emailService;

        // Initialize _context, _userManager and _emailService fields with injected services
        public ReservationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            // Get logged in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Redirect to login if user is not logged in
                return Redirect("/Identity/Account/Login");
            }

            // Get roles of logged in user
            var roles = await _userManager.GetRolesAsync(user);

            IQueryable<Reservation> reservations;

            // Check if user is Admin or Staff
            bool isAdminOrStaff = roles.Contains("Admin") || roles.Contains("Staff");

            if (isAdminOrStaff)
            {
                // If the user is an Admin or Staff, show all reservations
                reservations = _context.Reservations.Include(r => r.Customer).Include(r => r.Table);
            }
            else
            {
                // Associate Customer with user
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);

                if (customer == null)
                {
                    // Return not found if Customer does not exist
                    return NotFound("Customer not found.");
                }

                // Customer can see only their reservations
                reservations = _context.Reservations
                    .Include(r => r.Customer)
                    .Include(r => r.Table)
                    .Where(r => r.CustomerID == customer.CustomerID);
            }

            // Order reservations first by date, then by time slot
            reservations = reservations.OrderBy(r => r.ReservationDate).ThenBy(r => r.TimeSlot);

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

            return View(reservation);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create()
        {
            // Get logged in user
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Redirect to login if user is not logged in
                return Redirect("/Identity/Account/Login");
            }

            // Check if the user is Admin or Staff
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdminOrStaff = roles.Contains("Admin") || roles.Contains("Staff");

            Customer customer;

            if (isAdminOrStaff)
            {
                // Create customer for Admin/Staff
                customer = new Customer
                {
                    // Use Staff email as customer name
                    Name = user.Email,
                    Phone = "N/A",
                    Email = user.Email
                };

                // Save the customer if it does not exist
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customer.Email);

                if (existingCustomer == null)
                {
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    customer = existingCustomer;
                }
            }
            else
            {
                // Associate Customer with user
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);

                if (existingCustomer == null)
                {
                    // Redirect to create customer if the customer does not exist
                    return RedirectToAction("Create", "Customers");
                }

                customer = existingCustomer;
            }

            // Pass customer ID and available time slots to the view
            ViewData["CustomerID"] = customer.CustomerID;
            ViewData["TimeSlots"] = new SelectList(new[] { "18:00", "19:00", "20:00", "21:00", "22:00" });

            return View();
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerID,ReservationDate,TimeSlot,NumberOfGuests")] Reservation reservation)
        {
            // Get logged in user
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdminOrStaff = roles.Contains("Admin") || roles.Contains("Staff");

            // Validation to ensure reservation date is in the future
            if (reservation.ReservationDate < DateTime.Today)
            {
                ModelState.AddModelError("ReservationDate", "Sorry about that, but the reservation date must be today or later.");
            }
            // Validation to ensure time slot is in the future
            else if (reservation.ReservationDate == DateTime.Today)
            {
                var currentTime = DateTime.Now.TimeOfDay;
                var selectedTimeSlot = TimeSpan.Parse(reservation.TimeSlot);

                if (selectedTimeSlot <= currentTime)
                {
                    ModelState.AddModelError("TimeSlot", "Sorry about that, but the time slot must be later than the current time.");
                }
            }

            // Check if customer already has a reservation on this date 
            if (!isAdminOrStaff)
            {
                var existingReservation = await _context.Reservations
                .Where(r => r.CustomerID == reservation.CustomerID && r.ReservationDate == reservation.ReservationDate)
                .FirstOrDefaultAsync();

                if (existingReservation != null)
                {
                    ModelState.AddModelError("ReservationDate", "Sorry about that, but you already have a reservation on this date. However, you are welcome to modify the time slot or the number of guests if needed.");
                }
            }

            if (ModelState.IsValid)
            {
                // Check if there is available table for the selected date, time slot and number of guests
                var availableTable = await CheckTableAvailability(reservation.ReservationDate, reservation.TimeSlot, reservation.NumberOfGuests);

                if (availableTable != null)
                {
                    // If table is available, assign it to reservation and save
                    reservation.TableID = availableTable.TableID;
                    _context.Add(reservation);
                    await _context.SaveChangesAsync();

                    if (roles.Contains("Admin") || roles.Contains("Staff"))
                    {
                        TempData["SuccessMessage"] = "Reservation has been successfully made!";
                    }
                    else
                    {
                        var customer = await _context.Customers.FindAsync(reservation.CustomerID);

                        if (customer != null)
                        {
                            // Send confirmation email
                            var subject = "Reservation Confirmation - Pizzeria Moschini";
                            var body = $"<p>Dear {customer.Name},</p><p>Your reservation has been confirmed for {reservation.ReservationDate.ToShortDateString()} at {reservation.TimeSlot}.</p><p>Number of Guests: {reservation.NumberOfGuests}</p><p>We look forward to seeing you!</p><p>Best regards,<br/>Pizzeria Moschini</p>";
                            await _emailService.SendEmailAsync(customer.Email, subject, body);
                        }

                        TempData["SuccessMessage"] = "Your reservation has been successfully made! An email with the reservation details has been sent to your email address.";
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // If no table is available, add error message to model state
                    ModelState.AddModelError("NumberOfGuests", "Unfortunately, we don't have any available tables for your selected date and time slot. Please try choosing a different time or date. Thank you!");
                }
            }

            // Pass customer ID and available time slots to view
            ViewData["CustomerID"] = reservation.CustomerID;
            ViewData["TimeSlots"] = new SelectList(new[] { "18:00", "19:00", "20:00", "21:00", "22:00" });

            return View(reservation);
        }

        // Check if there is available table for the selected date, time slot and number of guests
        private async Task<Table> CheckTableAvailability(DateTime date, string timeSlot, int numberOfGuests)
        {
            // Get all tables that can accommodate the selected number of guests
            var tables = _context.Tables.Where(t => t.Capacity >= numberOfGuests).ToList();

            foreach (var table in tables)
            {
                // Count reservations for the selected table, date and time slot
                var reservationCount = await _context.Reservations
                    .Where(r => r.TableID == table.TableID && r.ReservationDate == date && r.TimeSlot == timeSlot)
                    .CountAsync();

                // If no conflict with selected time slot, return the table
                if (reservationCount == 0)
                {
                    return table;
                }
            }

            // Return null if no available table is found
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

            // Validation to ensure reservation date is in the future
            if (reservation.ReservationDate < DateTime.Today)
            {
                ModelState.AddModelError("ReservationDate", "Sorry about that, but the reservation date must be today or later.");
            }
            // Validation to ensure time slot is in the future
            else if (reservation.ReservationDate == DateTime.Today)
            {
                var currentTime = DateTime.Now.TimeOfDay;
                var selectedTimeSlot = TimeSpan.Parse(reservation.TimeSlot);

                if (selectedTimeSlot <= currentTime)
                {
                    ModelState.AddModelError("TimeSlot", "Sorry about that, but the time slot must be later than the current time.");
                }
            }

            // Get logged in user
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdminOrStaff = roles.Contains("Admin") || roles.Contains("Staff");

            // Check if customer already has a reservation on the same date
            if (!isAdminOrStaff)
            {
                var existingReservation = await _context.Reservations
                .Where(r => r.CustomerID == reservation.CustomerID && r.ReservationDate == reservation.ReservationDate && r.ReservationID != reservation.ReservationID)
                .FirstOrDefaultAsync();

                if (existingReservation != null)
                {
                    ModelState.AddModelError("ReservationDate", "Sorry about that, but you already have a reservation on this date. However, you are welcome to modify the time slot or the number of guests if needed.");
                }
            }

            if (ModelState.IsValid)
            {
                var availableTable = await CheckTableAvailability(reservation.ReservationDate, reservation.TimeSlot, reservation.NumberOfGuests);

                if (availableTable != null)
                {
                    reservation.TableID = availableTable.TableID;
                    try
                    {
                        // Update the reservation in the database
                        _context.Update(reservation);
                        await _context.SaveChangesAsync();

                        user = await _userManager.GetUserAsync(User);
                        roles = await _userManager.GetRolesAsync(user);

                        if (roles.Contains("Admin") || roles.Contains("Staff"))
                        {
                            TempData["SuccessMessage"] = "Reservation updated successfully.";
                        }
                        else
                        {
                            var customer = await _context.Customers.FindAsync(reservation.CustomerID);

                            if (customer != null)
                            {
                                // Send email
                                var subject = "Reservation Updated - Pizzeria Moschini";
                                var body = $"<p>Dear {customer.Name},</p><p>Your reservation has been updated to {reservation.ReservationDate.ToShortDateString()} at {reservation.TimeSlot}.</p><p>Number of Guests: {reservation.NumberOfGuests}</p><p>We look forward to seeing you!</p><p>Best regards,<br/>Pizzeria Moschini</p>";
                                await _emailService.SendEmailAsync(customer.Email, subject, body);
                            }

                            TempData["SuccessMessage"] = "Reservation updated successfully. An email with the updated reservation details has been sent to your email address.";
                        }

                        return RedirectToAction(nameof(Index));
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
                }
                else
                {
                    ModelState.AddModelError("NumberOfGuests", "Unfortunately, we don't have any available tables for your selected date and time slot. Please try choosing a different time or date. Thank you!");
                }
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
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Admin") || roles.Contains("Staff"))
                {
                    TempData["SuccessMessage"] = "Reservation cancelled successfully!";
                }
                else
                {
                    var customer = await _context.Customers.FindAsync(reservation.CustomerID);

                    if (customer != null)
                    {
                        // Send email
                        var subject = "Reservation Cancellation - Pizzeria Moschini";
                        var body = $"<p>Dear {customer.Name},</p><p>Your reservation for {reservation.ReservationDate.ToShortDateString()} at {reservation.TimeSlot} has been successfully cancelled.</p><p>We hope to see you in the future.</p><p>Best regards,<br/>Pizzeria Moschini</p>";
                        await _emailService.SendEmailAsync(customer.Email, subject, body);
                    }

                    TempData["SuccessMessage"] = "Reservation cancelled successfully! An email confirmation has been sent to your email address.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.ReservationID == id);
        }
    }
}
