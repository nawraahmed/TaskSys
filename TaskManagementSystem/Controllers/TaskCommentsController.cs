using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Models;

namespace TaskManagementSystem.Controllers
{
    public class TaskCommentsController : Controller
    {
        private readonly TaskAllocationDBContext _context;

        public TaskCommentsController(TaskAllocationDBContext context)
        {
            _context = context;
        }

        // GET: TaskComments
        public async Task<IActionResult> Index()
        {
            var taskAllocationDBContext = _context.TaskComments.Include(t => t.Task).Include(t => t.UsernameNavigation);
            return View(await taskAllocationDBContext.ToListAsync());
        }

        // GET: TaskComments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.TaskComments == null)
            {
                return NotFound();
            }

            var taskComment = await _context.TaskComments
                .Include(t => t.Task)
                .Include(t => t.UsernameNavigation)
                .FirstOrDefaultAsync(m => m.CommentId == id);
            if (taskComment == null)
            {
                return NotFound();
            }

            return View(taskComment);
        }

        // GET: TaskComments/Create
        public IActionResult Create(int taskid)
        {
            ViewData["TaskId"] = _context.TaskComments.FirstOrDefault(x => x.TaskId == taskid);   
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username");
            return View();
        }

        // POST: TaskComments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CommentId,Comment,TaskId,Username")] TaskComment taskComment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(taskComment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TaskId"] = new SelectList(_context.Tasks, "TaskId", "TaskId", taskComment.TaskId);
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username", taskComment.Username);
            return View(taskComment);
        }

        // GET: TaskComments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.TaskComments == null)
            {
                return NotFound();
            }

            var taskComment = await _context.TaskComments.FindAsync(id);
            if (taskComment == null)
            {
                return NotFound();
            }
            ViewData["TaskId"] = new SelectList(_context.Tasks, "TaskId", "TaskId", taskComment.TaskId);
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username", taskComment.Username);
            return View(taskComment);
        }

        // POST: TaskComments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CommentId,Comment,TaskId,Username")] TaskComment taskComment)
        {
            if (id != taskComment.CommentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taskComment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskCommentExists(taskComment.CommentId))
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
            ViewData["TaskId"] = new SelectList(_context.Tasks, "TaskId", "TaskId", taskComment.TaskId);
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username", taskComment.Username);
            return View(taskComment);
        }

        // GET: TaskComments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.TaskComments == null)
            {
                return NotFound();
            }

            var taskComment = await _context.TaskComments
                .Include(t => t.Task)
                .Include(t => t.UsernameNavigation)
                .FirstOrDefaultAsync(m => m.CommentId == id);
            if (taskComment == null)
            {
                return NotFound();
            }

            return View(taskComment);
        }

        // POST: TaskComments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.TaskComments == null)
            {
                return Problem("Entity set 'TaskAllocationDBContext.TaskComments'  is null.");
            }
            var taskComment = await _context.TaskComments.FindAsync(id);
            if (taskComment != null)
            {
                _context.TaskComments.Remove(taskComment);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TaskCommentExists(int id)
        {
          return (_context.TaskComments?.Any(e => e.CommentId == id)).GetValueOrDefault();
        }
    }
}
