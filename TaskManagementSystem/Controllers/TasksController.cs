using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Models;
using TaskManagementSystem.ViewModels;

namespace TaskManagementSystem.Controllers
{
    public class TasksController : Controller
    {
        private readonly TaskAllocationDBContext _context;

        public TasksController(TaskAllocationDBContext context)
        {
            _context = context;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(int? projectId)
        {

            TasksVM tasksVM = new TasksVM();
            tasksVM.Project = await _context.Projects.Where(x => x.ProjectId == projectId).FirstOrDefaultAsync();
            tasksVM.Project.Tasks = await _context.Tasks.Include(t => t.AssignedToUsernameNavigation).Where(p => p.ProjectId == projectId)
                    .Include(t => t.Project).Include(t => t.TaskDocumentNavigation)
                 .ToListAsync();
            return View(tasksVM);



        }
        public async Task<IActionResult> MyTasks(int? projectId)
        {

            TasksVM tasksVM = new TasksVM();
            tasksVM.Project = await _context.Projects.Where(x => x.ProjectId == projectId).FirstOrDefaultAsync();
            tasksVM.Project.Tasks = await _context.Tasks.Include(t => t.AssignedToUsernameNavigation).Where(p => p.ProjectId == projectId).Where(x=>x.AssignedToUsername==User.Identity.Name)
                    .Include(t => t.Project).Include(t => t.TaskDocumentNavigation)
                 .ToListAsync();
            return View(tasksVM);



        }
        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.AssignedToUsernameNavigation)
                .Include(t => t.Project)
                .Include(t => t.TaskDocumentNavigation)
                .FirstOrDefaultAsync(m => m.TaskId == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: Tasks/Create
        public IActionResult Create(int projectId)
        {
            //ViewData["AssignedToUsername"] = new SelectList(_context.Users, "Username", "Username");
            //ViewData["ProjectId"] = projectId; //new SelectList(_context.Projects, "ProjectId", "ProjectId");
            //ViewData["TaskDocument"] = new SelectList(_context.Documents, "DocumentId", "DocumentId");
            //return View();
            var tasksViewModel = new TasksVM
            {
                Task = new(),
                Project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId),
                ProjectMembers = _context.ProjectMembers
            };

            return View(tasksViewModel);
        }

        // POST: Tasks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TasksVM tasksVM)
        {
            Models.Task task = tasksVM.Task;
            // Retrieve the ProjectId 
            int projectId = task.ProjectId;
            task.Project = _context.Projects.FirstOrDefault(x => x.ProjectId == projectId);
            // Populate the Project property using the projectId
            tasksVM.Project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            task.AssignedToUsernameNavigation = (User)_context.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
            task.AssignedToUsername = User.Identity.Name;
          //  task.Project =_context.Projects.FirstOrDefault(x => x.ProjectId==pro)
            ModelState.Clear();
            TryValidateModel(task);
           
            if (ModelState.IsValid)
            {
                _context.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index),new { projectId = task.ProjectId });
            }
            //ViewData["AssignedToUsername"] = new SelectList(_context.Users, "Username", "Username", task.AssignedToUsername);
            //ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectId", task.ProjectId);
            //ViewData["TaskDocument"] = new SelectList(_context.Documents, "DocumentId", "DocumentId", task.TaskDocument);
            return View(tasksVM);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            ViewData["AssignedToUsername"] = new SelectList(_context.Users, "Username", "Username", task.AssignedToUsername);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectId", task.ProjectId);
            ViewData["TaskDocument"] = new SelectList(_context.Documents, "DocumentId", "DocumentId", task.TaskDocument);
            return View(task);
        }

        // POST: Tasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit( [Bind("TaskId,Name,Description,Status,Deadline,ProjectId,AssignedToUsername,TaskDocument")] Models.Task task)
        {
            // Retrieve the ProjectId 
            int projectId = task.ProjectId;
            // Populate the Project property using the projectId
            task.Project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            task.AssignedToUsernameNavigation = (User)_context.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
            task.AssignedToUsername = User.Identity.Name;
            //  task.Project =_context.Projects.FirstOrDefault(x => x.ProjectId==pro)
            ModelState.Clear();
            TryValidateModel(task);
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(task);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.TaskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index),new { projectId = task.ProjectId });
            }
            ViewData["AssignedToUsername"] = new SelectList(_context.Users, "Username", "Username", task.AssignedToUsername);
            ViewData["ProjectId"] = task.ProjectId;
            ViewData["TaskDocument"] = new SelectList(_context.Documents, "DocumentId", "DocumentId", task.TaskDocument);
            return View(task);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.AssignedToUsernameNavigation)
                .Include(t => t.Project)
                .Include(t => t.TaskDocumentNavigation)
                .FirstOrDefaultAsync(m => m.TaskId == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tasks == null)
            {
                return Problem("Entity set 'TaskAllocationDBContext.Tasks'  is null.");
            }
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
        }

        private bool TaskExists(int id)
        {
            return (_context.Tasks?.Any(e => e.TaskId == id)).GetValueOrDefault();
        }



    }
}
