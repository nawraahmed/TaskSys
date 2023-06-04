using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Hubs;
using TaskManagementSystem.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TaskManagementSystem.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly TaskAllocationDBContext _context;
        private readonly NotificationsHub _hubcontext;

        public NotificationsController(TaskAllocationDBContext context, NotificationsHub hubcontext)
        {
            _context = context;
            _hubcontext = hubcontext;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var taskAllocationDBContext = _context.Notifications
                .Include(n => n.UsernameNavigation)
                .Include(n => n.UsernameNavigation.Projects)
                .Where(n => n.Username == User.Identity.Name)
                .OrderByDescending(n => n.Notification_Date); 
            return View(await taskAllocationDBContext.ToListAsync());
        }

        // GET: Notifications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Notifications == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications
                .Include(n => n.UsernameNavigation)
                .FirstOrDefaultAsync(m => m.NotificationId == id);
            if (notification == null)
            {
                return NotFound();
            }
            // Update the status to "Read"
            notification.Status = "Read";

            //add log for nortification modification
            LogsController.CreateLog(_context, "web", "Notifications/Details", User.Identity.Name, "A notification state modified to Read", notification, EntityState.Modified);

            // Save the changes to the database
            await _context.SaveChangesAsync();

            return View(notification);
        }

        // GET: Notifications/Create
        public IActionResult Create()
        {
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username");
            return View();
        }

        // POST: Notifications/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NotificationId,Message,Type,Status,Username")] Notification notification)
        {
            if (ModelState.IsValid)
            {
                _context.Add(notification);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username", notification.Username);
            return View(notification);
        }

        // GET: Notifications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Notifications == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username", notification.Username);
            return View(notification);
        }

        // POST: Notifications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NotificationId,Message,Type,Status,Username")] Notification notification)
        {
            if (id != notification.NotificationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(notification);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotificationExists(notification.NotificationId))
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
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username", notification.Username);
            return View(notification);
        }

        // GET: Notifications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Notifications == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications
                .Include(n => n.UsernameNavigation)
                .FirstOrDefaultAsync(m => m.NotificationId == id);
            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }

        // POST: Notifications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Notifications == null)
            {
                return Problem("Entity set 'TaskAllocationDBContext.Notifications'  is null.");
            }
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NotificationExists(int id)
        {
          return (_context.Notifications?.Any(e => e.NotificationId == id)).GetValueOrDefault();
        }

        public async Task<IActionResult> Client()
        {
            ViewData["Notifications"] = _context.Notifications.Where(n => n.Username == User.Identity.Name).ToList();
            return View();
        }
        public async Task<IActionResult> GetAll()
        {

            var notifications = _context.Notifications.Where(n => n.Username == User.Identity.Name).OrderByDescending(n=>n.Notification_Date).ToList();
            return Json(notifications);
        }
        public async System.Threading.Tasks.Task NotificationBroadcast()
        {
            await _hubcontext.NotificationsHubBroadcast(_context.Notifications.ToList());
        }

        public static async System.Threading.Tasks.Task SendNotification(String message,string type,string status,string username, TaskAllocationDBContext context) {
            var newNotification = new Notification
            {
                Message = message,
                Type = type,
                Status = status,
                Username = username
            };

            context.Add(newNotification);
            LogsController.CreateLog(context, "web", "Notifications/Create", username, "A notification sent:  "+ type, newNotification, EntityState.Added);
            await context.SaveChangesAsync();
        }

    }
}
