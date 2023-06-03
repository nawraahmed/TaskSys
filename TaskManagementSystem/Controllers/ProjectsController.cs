using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Hubs;
using TaskManagementSystem.Areas.Identity.Data;
using TaskManagementSystem.Models;
using TaskManagementSystem.ViewModels;
using TaskManagementSystem.Controllers;

namespace TaskManagementSystem.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly TaskAllocationDBContext _context;
        private readonly IHubContext<NotificationsHub> _hubcontext;


        public ProjectsController(TaskAllocationDBContext context, IHubContext<NotificationsHub> hubcontext)
        {
            _context = context;
            _hubcontext = hubcontext;

        }



        public IActionResult GetUsersSelectList()
        {
            var users = _context.Users.Select(m => new SelectListItem { Text = m.Username }).ToList();
            return Json(users);
        }










        // GET: Projects
        public async Task<IActionResult> Index(string search)
        {
            List<Project> projects = await _context.Projects
                .Include(p => p.CreatedByUsernameNavigation)
                .Where(p => p.ProjectMembers.Any(m => m.Username == User.Identity.Name) || p.CreatedByUsername == User.Identity.Name)
                .ToListAsync();

            foreach (var project in projects)
            {
                if (project.Status != "Completed" && project.Deadline < DateTime.Now.Date)
                {
                    project.Status = "Overdue";
                }

                // Load tasks into memory
                var tasks = await _context.Tasks
                    .Where(t => t.ProjectId == project.ProjectId)
                    .ToListAsync();

                if (tasks.Count > 0 && tasks.All(t => t.Status == "Completed"))
                {
                    project.Status = "Completed";
                }


                _context.Projects.Update(project);
            }

            await _context.SaveChangesAsync();

            return View(projects);
        }

        public async Task<IActionResult> MyProjectsIndex(string search)
        {
            //display the projects that it is created by this user
            IQueryable<Project> taskAllocationDBContext = _context.Projects.Include(p => p.CreatedByUsernameNavigation).Where(x => x.CreatedByUsername == User.Identity.Name);

            if (!string.IsNullOrEmpty(search))
            {
                taskAllocationDBContext = taskAllocationDBContext.Where(p => p.Name.Contains(search));
            }
            return View(await taskAllocationDBContext.ToListAsync());
        }



        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.CreatedByUsernameNavigation)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            // Fetch the existing project members for the selected project
            var projectMembers = _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.ProjectId)
                .Select(pm => pm.Username)
                .ToList();

            var pviewModel = new ProjectsUsersVM
            {
                Project = project,
                Users = _context.Users.ToList(),
                SelectedMembers = projectMembers
            };

            return View(pviewModel);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {

            //instatination of view model, new project object, populate the users list, new list for the members
            //to assign values to them from the view of create.cshtml
            var pviewModel = new ProjectsUsersVM
            {
                Project = new(),
                Users = _context.Users,
                SelectedMembers = new List<String>()
            };
            return View(pviewModel);
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,Name,Description,Deadline,Budget,CreatedByUsername,Status, SelectedMembers, Tasks, project_members_id")] Project project, List<String> selectedMembers)
        {
            //close any previously opened connection
            await _context.Database.CloseConnectionAsync();


            //store the current logged in user and assign it to the CreatedByUsername and the navigational property
            var user = await _context.Users.SingleOrDefaultAsync(x => x.Username == User.Identity.Name);
            project.CreatedByUsername = user.Username;
            project.CreatedByUsernameNavigation = user;


            // Initialize the navigational properties to ensure that ModelState is valid
            project.ProjectMembers = new List<ProjectMember>();
            project.Tasks = new List<Models.Task>();

            //clearing model state after the newely posted project object
            ModelState.Clear();
            TryValidateModel(project);

            //Only if the modelstate is valid, the data will be inserted
            if (ModelState.IsValid)
            {

                // Insert one row for the new project in the projects table
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();


                //if the List is not empty, insert to project members table using a loop
                if (selectedMembers != null && selectedMembers.Count > 0)
                {
                    //add the current logged in user in the selectedMembers array
                    selectedMembers.Add(User.Identity.Name);

                    //loop to insert a record in projectMemebr table for each member in the list
                    foreach (var memberName in selectedMembers)
                    {
                        

                        //assign the values to a new project member 
                        var projectMember = new ProjectMember
                        {
                            ProjectId = project.ProjectId,
                            Username = memberName.Trim() //triming the names
                        };


                        //adding a row for each member
                        _context.ProjectMembers.Add(projectMember);

                        //create notification
                        var message = $"You have been assigned to a new project : {project.Name} by manager {project.CreatedByUsername}.";
                        var type = "New Project Assignment";
                        var status = "Unread";
                        var username = projectMember.Username;
                        await NotificationsController.SendNotification(message, type, status, username, _context);
                        var notifications = new List<Notification> {
    new Notification { Message = message , Status = status}
};
                        if (_hubcontext != null)
                        {
                            await _hubcontext.Clients.All.SendAsync("getUpdatedNotifications", notifications);
                        }

                    }

                    await _context.SaveChangesAsync();

                    TempData["msg"] = "Project Added Successfully";

                    //add a log for peoject creation
                    LogsController.CreateLog(_context, "web", "Projects/Create", User.Identity.Name, "New Project Created", null, project);


                }



                //if all of this done, return to the index
                return RedirectToAction("Index");

            }


            //passing the view model to the view
            var pviewModel = new ProjectsUsersVM
            {
                Project = new(),
                Users = _context.Users,
                SelectedMembers = new List<String>()

            };
            return View(pviewModel);

        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Fetch the existing project members for the selected project
            var projectMembers = _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.ProjectId)
                .Select(pm => pm.Username)
                .ToList();


            var pviewModel = new ProjectsUsersVM
            {
                Project = project,
                Users = _context.Users.ToList(),
                SelectedMembers = projectMembers
            };

            ViewData["CreatedByUsername"] = new SelectList(_context.Users, "Username", "Username", project.CreatedByUsername);
            return View(pviewModel);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, ProjectsUsersVM editProject, List<String> selectedMembers)
        {

            //if the id is not found
            if (id != editProject.Project.ProjectId)
            {
                return NotFound();
            }


           

            // Initialize the navigational properties to ensure that ModelState is valid
            editProject.Project.ProjectMembers = new List<ProjectMember>();
            editProject.Project.Tasks = new List<Models.Task>();
            editProject.Users = _context.Users.ToList();

            //store the current logged in user and assign it to the CreatedByUsername and the navigational property
            var user = await _context.Users.SingleOrDefaultAsync(x => x.Username == User.Identity.Name);
            editProject.Project.CreatedByUsername = user.Username;
            editProject.Project.CreatedByUsernameNavigation = user;

            //clearing model state after the newely posted project object
            ModelState.Clear();
            TryValidateModel(editProject);

            //check if the model state is valid
            if (ModelState.IsValid)
            {

                // Retrieve the existing project from the database
                var existingProject = await _context.Projects
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.ProjectId == id);

                //store the new values in the edit form
                existingProject.Name = editProject.Project.Name;
                existingProject.Description = editProject.Project.Description;
                existingProject.Deadline = editProject.Project.Deadline;
                existingProject.Budget = editProject.Project.Budget;
                existingProject.Status = editProject.Project.Status;


                // Update the project members with the selected members
                if (selectedMembers != null)
                {
                    //add the current logged in user in the selectedMembers array
                    selectedMembers.Add(User.Identity.Name);

                    var membersToAdd = selectedMembers.Except(existingProject.ProjectMembers.Select(m => m.Username)).ToList();
                    var membersToRemove = existingProject.ProjectMembers.Where(m => !selectedMembers.Contains(m.Username)).ToList();

                    foreach (var memberName in membersToAdd)
                    {
                        // Create a new project member
                        var projectMember = new ProjectMember
                        {
                            ProjectId = editProject.Project.ProjectId,
                            Username = memberName.Trim()
                        };

                        // Add the project member to the existing project
                        existingProject.ProjectMembers.Add(projectMember);
                    }

                    foreach (var member in membersToRemove)
                    {
                        // Remove the project member from the existing project members list
                        existingProject.ProjectMembers.Remove(member);
                    }
                }


                // Check if the status has changed
                bool statusChanged = false;

                var originalProject = _context.Projects.AsNoTracking().FirstOrDefault(t => t.ProjectId == existingProject.ProjectId);
                if (originalProject != null && originalProject.Status != existingProject.Status)
                {
                    statusChanged = true;
                }

                //update the projects table
                _context.Projects.Update(existingProject);
                await _context.SaveChangesAsync();
                //add a log for peoject updating
                LogsController.CreateLog(_context, "web", "Projects/Edit", User.Identity.Name, "A Project has been edited", existingProject.Name, existingProject);

                TempData["msg"] = "Project Updated Successfully";



                if (statusChanged)
                {
                    // Retrieve the project manager for the corresponding project
                    var projectManager = await _context.Projects
                        .Include(p => p.CreatedByUsernameNavigation)
                        .Where(p => p.ProjectId == existingProject.ProjectId)
                        .Select(p => p.CreatedByUsernameNavigation)
                        .FirstOrDefaultAsync();

                    if (projectManager != null)
                    {
                        // Create a new notification for the project manager

                        var message = $"Project {existingProject.Name} status updated to {existingProject.Status}.";
                        var type = "Project Status Updated";
                        var status = "Unread";
                        var username = projectManager.Username;
                        await NotificationsController.SendNotification(message, type, status, username, _context);
                        var notifications = new List<Notification> {
    new Notification { Message = message , Status = status}
};
                        if (_hubcontext != null)
                        {
                            await _hubcontext.Clients.All.SendAsync("getUpdatedNotifications", notifications);
                        }


                    }
                }




                return RedirectToAction("Index");
            }
            else
            {
                var pviewModel = new ProjectsUsersVM
                {
                    Project = editProject.Project,
                    Users = _context.Users
                };



                return View(pviewModel);
            }


        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.CreatedByUsernameNavigation)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Projects == null)
            {
                return Problem("Entity set 'TaskAllocationDBContext.Projects'  is null.");
            }
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {




                // Remove the task comments associated with the project
                var projectTaskComments = await _context.TaskComments
                    .Include(tc => tc.Task)
                    .Where(tc => tc.Task.ProjectId == id)
                    .ToListAsync();

                foreach (var comment in projectTaskComments)
                {
                    _context.TaskComments.Remove(comment);

                    //add a log for task comment removal
                    LogsController.CreateLog(_context, "web", "Projects/Delete", User.Identity.Name, "A task comment was deleted", comment, null);
                }






                // Retrieve the project tasks associated with the project
                var projectTasks = await _context.Tasks
                    .Where(t => t.ProjectId == id)
                    .ToListAsync();

                // Remove the project tasks
                foreach (var task in projectTasks)
                {
                    _context.Tasks.Remove(task);

                    //add a log for task removal
                    LogsController.CreateLog(_context, "web", "Projects/Delete", User.Identity.Name, "A task was deleted", task, null);
                }

                // Iterate over the project tasks
                foreach (var task in projectTasks)
                {
                    // Retrieve the task documents associated with each task
                    var taskDocuments = await _context.Documents
                        .Where(d => d.Tasks.Contains(task))
                        .ToListAsync();

                    // Remove the task documents
                    _context.Documents.RemoveRange(taskDocuments);

                    // Add a log for task document removal
                    LogsController.CreateLog(_context, "web", "Projects/Delete", User.Identity.Name, "Task documents were deleted", taskDocuments, null);
                }

                // Retrieve the project members associated with the project
                var projectMembers = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == id)
                    .ToListAsync();

                // Remove the project members
                foreach (var projectMember in projectMembers)
                {
                    _context.ProjectMembers.Remove(projectMember);
                    //add a log for peoject members removal
                    LogsController.CreateLog(_context, "web", "Projects/Delete", User.Identity.Name, "A project member was removed", null, null);
                }

                //then remove the project itself
                _context.Projects.Remove(project);

                //add a log for peoject removal
                LogsController.CreateLog(_context, "web", "Projects/Delete", User.Identity.Name, "A project was deleted", null, null);
            }

            await _context.SaveChangesAsync();
            TempData["msg"] = "Project Deleted Successfully";
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return (_context.Projects?.Any(e => e.ProjectId == id)).GetValueOrDefault();
        }


        public IActionResult Dashboard(int projectId)
        {
            try
            {
                // Get the project name based on the project ID
                string projectName = _context.Projects
                    .Where(p => p.ProjectId == projectId)
                    .Select(p => p.Name)
                    .FirstOrDefault();




                // Calculate the number of completed tasks for the project
                int completedTasksCount = _context.Tasks
                    .Count(t => t.ProjectId == projectId && t.Status == "Completed");

                // Calculate the number of remaining tasks for the project
                int remainingTasksCount = _context.Tasks
                    .Count(t => t.ProjectId == projectId && t.Status != "Completed");

                // Find the best project member based on the number of tasks he completed
                var bestMember = _context.Tasks
                    .Where(t => t.ProjectId == projectId && t.Status == "Completed")
                    .GroupBy(t => t.AssignedToUsername)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                // Calculate the completion percentage
                double completionPercentage = 0;
                if (completedTasksCount + remainingTasksCount > 0)
                {
                    completionPercentage = (double)completedTasksCount / (completedTasksCount + remainingTasksCount) * 100;
                }

                // Fetch the data for the chart
                var memberUsernames = _context.Tasks
                    .Where(t => t.ProjectId == projectId && t.Status == "Completed")
                    .GroupBy(t => t.AssignedToUsername)
                    .Select(g => g.Key)
                    .ToList();

                var taskCounts = _context.Tasks
                    .Where(t => t.ProjectId == projectId && t.Status == "Completed")
                    .GroupBy(t => t.AssignedToUsername)
                    .Select(g => g.Count())
                    .ToList();


                // Pass the data to the view
                ViewBag.ProjectName = projectName;
                ViewBag.ProjectId = projectId;
                ViewBag.CompletedTasksCount = completedTasksCount;
                ViewBag.RemainingTasksCount = remainingTasksCount;
                ViewBag.BestMember = bestMember;
                ViewBag.CompletionPercentage = completionPercentage.ToString("F0"); // Round the percentage to 0 decimal places
                ViewBag.Members = memberUsernames;
                ViewBag.TaskCounts = taskCounts;

                return View(ViewBag);
            }
            catch
            {
                return BadRequest();
            }
        }


    }

}
