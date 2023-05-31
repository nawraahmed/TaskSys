﻿using System;
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
                ProjectMembers = _context.ProjectMembers.Where(p=>p.ProjectId== projectId)
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
            // Extract the task from the view model
            Models.Task task = tasksVM.Task;

            // Retrieve the ProjectId 
            int projectId = task.ProjectId;

            // Retrieve the project using the projectId
            task.Project = _context.Projects.FirstOrDefault(x => x.ProjectId == projectId);

            // Populate the Project property of the view model using the projectId
            tasksVM.Project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId);


            // Retrieve the selected project member's username
            tasksVM.SelectedProjectMemberUsername = task.AssignedToUsername;

            // Set the task's AssignedToUsername to the selected project member's username
            task.AssignedToUsernameNavigation = _context.Users.FirstOrDefault(x => x.Username == tasksVM.SelectedProjectMemberUsername);

            // Set the task's AssignedToUsername to the selected project member's username
            task.AssignedToUsername = tasksVM.SelectedProjectMemberUsername;

            // Retrieve the currently logged-in user
            var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Username == User.Identity.Name);

            if (currentUser != null)
            {
                // Set the CreatedByUsernameNavigation property of the project to the current user
                task.Project.CreatedByUsernameNavigation = currentUser;
            }
            //clear the modelstate and validate it
            ModelState.Clear();
            TryValidateModel(task);
           
            if (ModelState.IsValid)
            {
                // Add the task to the context and save changes
                _context.Add(task);
                await _context.SaveChangesAsync();
                // Redirect to the index action of the tasks controller, passing the projectId as a route value
                return RedirectToAction(nameof(Index),new { projectId = task.ProjectId });
            }
            // Retrieve the project members for the selected project
            tasksVM.ProjectMembers = _context.ProjectMembers.Where(p => p.ProjectId == projectId);


            //ViewData["AssignedToUsername"] = new SelectList(_context.Users, "Username", "Username", task.AssignedToUsername);
            //ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectId", task.ProjectId);
            //ViewData["TaskDocument"] = new SelectList(_context.Documents, "DocumentId", "DocumentId", task.TaskDocument);

            // Return the view with the updated view model
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

            var projectId = task.ProjectId;
         

            ViewData["AssignedToUsername"] = new SelectList(_context.ProjectMembers.Where(x=>x.ProjectId==projectId), "Username", "Username", task.AssignedToUsername);
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


            // Retrieve the user entity based on the AssignedToUsername of the task
            task.AssignedToUsernameNavigation = _context.Users.FirstOrDefault(x => x.Username == task.AssignedToUsername);


            // Retrieve the currently logged-in user
            var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Username == User.Identity.Name);

            if (currentUser != null)
            {
                // Set the CreatedByUsernameNavigation property of the project to the current user
                task.Project.CreatedByUsernameNavigation = currentUser;
            }

            ModelState.Clear();
            TryValidateModel(task);
            if (ModelState.IsValid)
            {
                try
                {
                    // Update the task in the context and save changes
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
                // Redirect to the index action of the tasks controller, passing the projectId as a route value
                return RedirectToAction(nameof(Index),new { projectId = task.ProjectId });
            }


            // Set the ViewData for the AssignedToUsername, ProjectId, and TaskDocument fields
            ViewData["AssignedToUsername"] = new SelectList(_context.ProjectMembers.Where(x => x.ProjectId == projectId), "Username", "Username", task.AssignedToUsername);
            ViewData["ProjectId"] = task.ProjectId;
            ViewData["TaskDocument"] = new SelectList(_context.Documents, "DocumentId", "DocumentId", task.TaskDocument);


            // Return the view with the updated task
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
