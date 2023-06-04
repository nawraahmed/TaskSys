using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TaskManagementSystem.Models;

namespace TaskManagementSystem.Controllers
{
    public class LogsController : Controller
    {
        private readonly TaskAllocationDBContext _context;

        public LogsController(TaskAllocationDBContext context)
        {
            _context = context;
        }

        // GET: Logs
        public async Task<IActionResult> Index()
        {
            var taskAllocationDBContext = _context.Logs.Include(l => l.UsernameNavigation);
            return View(await taskAllocationDBContext.ToListAsync());
        }

        // GET: Logs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Logs == null)
            {
                return NotFound();
            }

            var log = await _context.Logs
                .Include(l => l.UsernameNavigation)
                .FirstOrDefaultAsync(m => m.LogId == id);
            if (log == null)
            {
                return NotFound();
            }

            return View(log);
        }


        public static void CreateLog(TaskAllocationDBContext context, string source, string type, string username, string message, object entity, EntityState state)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            

            // Create a new log object
            var log = new Log
            {
                Source = $"{source}",
                Type = type,
                Username = username,
                Date = DateTime.Now,
                Message = message
            };

            // Check if the entity is being added, modified or deleted
            if (state == EntityState.Added || state == EntityState.Modified)
            {
                var entry = context.Entry(entity);
                // Get the original values from the database
                var databaseValues = entry.GetDatabaseValues();

                // Convert current values to a readable format
                var currentValues = entry.CurrentValues.Properties
                    .ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString());

                if (databaseValues != null)
                {
                    // Convert original values to a readable format
                    var originalValues = databaseValues.Properties
                        .ToDictionary(p => p.Name, p => databaseValues[p]?.ToString());

                    // Serialize dictionaries to JSON strings
                    log.OriginalValue = JsonConvert.SerializeObject(originalValues);
                }
                else
                {
                    // If there are no original values, set OriginalValue to null
                    log.OriginalValue = null;
                }

                // Serialize current values to a JSON string
                log.CurrentValue = JsonConvert.SerializeObject(currentValues);
            }
            else if (state == EntityState.Deleted)
            {
                // Get the original values of the entity before deleting it
                var entry = context.Entry(entity);
                var originalValues = entry.OriginalValues.Properties
                    .ToDictionary(p => p.Name, p => entry.OriginalValues[p]?.ToString());

                // Serialize original values to a JSON string
                log.OriginalValue = JsonConvert.SerializeObject(originalValues);

                // Set CurrentValue to null since the entity is being deleted
                log.CurrentValue = null;
            }

            // Add the log to the context and save the changes
            context.Logs.Add(log);
            context.SaveChanges();
        }


       
        private bool LogExists(int id)
        {
          return (_context.Logs?.Any(e => e.LogId == id)).GetValueOrDefault();
        }
    }
}
