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
        public async Task<IActionResult> Index(int? taskid)
        {
            var taskAllocationDBContext = _context.TaskComments.Where(t=>t.TaskId==taskid).Include(t => t.Task).Include(t => t.UsernameNavigation);
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
            ViewData["TaskId"] = taskid; 
            ViewData["Username"] = new SelectList(_context.Users, "Username", "Username");
            return View();
        }

        // POST: TaskComments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Comment,TaskId,Username")] TaskComment taskComment)
        {
            int taskid = taskComment.TaskId;
            // Models.Task task = taskComment.Task;
            taskComment.Task = _context.Tasks.Include(x=>x.AssignedToUsernameNavigation).FirstOrDefault(x => x.TaskId == taskid);
            taskComment.Task.Project = _context.Projects.Include(p=>p.CreatedByUsernameNavigation).FirstOrDefault(x => x.ProjectId == taskComment.Task.ProjectId);
            taskComment.UsernameNavigation = (User)_context.Users.FirstOrDefault(x => x.Username == User.Identity.Name);

            if (taskComment.Comment.Length < 5)
            {
                ModelState.AddModelError("Comment", "Comment must have at least 5 characters.");
            }


            ModelState.Clear();
            TryValidateModel(taskComment);
            if (ModelState.IsValid)
            {
                _context.Add(taskComment);

                //add a log for adding a comment
                LogsController.CreateLog(_context, "web", "TaskComments/Create", User.Identity.Name, "A task comment was added", taskComment, EntityState.Added);



                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), "Tasks",new {id = taskComment.TaskId});
            }
            ViewData["TaskId"] = taskComment.TaskId;
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
        public async Task<IActionResult> Edit( [Bind("CommentId,Comment,TaskId,Username")] TaskComment taskComment)
        {
            int taskid = taskComment.TaskId;
            // Models.Task task = taskComment.Task;
            taskComment.Task = _context.Tasks.Include(x => x.AssignedToUsernameNavigation).FirstOrDefault(x => x.TaskId == taskid);
            taskComment.Task.Project = _context.Projects.Include(p => p.CreatedByUsernameNavigation).FirstOrDefault(x => x.ProjectId == taskComment.Task.ProjectId);
            taskComment.UsernameNavigation = (User)_context.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
            ModelState.Clear();
            TryValidateModel(taskComment);
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
                return RedirectToAction(nameof(Details), "Tasks", new { id = taskComment.TaskId });
            }
            ViewData["TaskId"] = taskComment.TaskId;
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
            return RedirectToAction(nameof(Details), "Tasks", new { id = taskComment.TaskId });
        }

        private bool TaskCommentExists(int id)
        {
          return (_context.TaskComments?.Any(e => e.CommentId == id)).GetValueOrDefault();
        }
    }
}
