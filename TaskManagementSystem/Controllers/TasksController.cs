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
        public async Task<IActionResult> Index(int projectId, string tab = "alltasks", string search = "")
        {
            TasksVM tasksVM = new TasksVM();

            tasksVM.Tab = tab;

            tasksVM.Project = await _context.Projects.FirstOrDefaultAsync(x => x.ProjectId == projectId);

            if (tab == "mytasks")
            {
                if (!string.IsNullOrEmpty(search))
                {
                    tasksVM.Project.Tasks = await _context.Tasks
                   .Include(t => t.AssignedToUsernameNavigation)
                   .Where(p => p.ProjectId == projectId && p.AssignedToUsername == User.Identity.Name && p.Name.Contains(search)) // Filter by assigned user
                   .Include(t => t.Project)
                   .Include(t => t.TaskDocumentNavigation)
                   .ToListAsync();

                }

                else
                {

                    tasksVM.Project.Tasks = await _context.Tasks
                    .Include(t => t.AssignedToUsernameNavigation)
                    .Where(p => p.ProjectId == projectId && p.AssignedToUsername == User.Identity.Name) // Filter by assigned user
                    .Include(t => t.Project)
                    .Include(t => t.TaskDocumentNavigation)
                    .ToListAsync();

                }
            }
            else if (tab == "alltasks")
            {
                // Apply search filter if search term is provided
                if (!string.IsNullOrEmpty(search))
                {
                    tasksVM.Project.Tasks = await _context.Tasks
                        .Include(t => t.AssignedToUsernameNavigation)
                        .Where(p => p.ProjectId == projectId && p.Name.Contains(search))
                        .Include(t => t.Project)
                        .Include(t => t.TaskDocumentNavigation)
                        .ToListAsync();
                }
                else
                {
                    tasksVM.Project.Tasks = await _context.Tasks
                        .Include(t => t.AssignedToUsernameNavigation)
                        .Where(p => p.ProjectId == projectId)
                        .Include(t => t.Project)
                        .Include(t => t.TaskDocumentNavigation)
                        .ToListAsync();
                }
            }

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
                .Include(t=>t.TaskComments)
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
                ProjectMembers = _context.ProjectMembers.Where(p=>p.ProjectId== projectId),
                Document = new()
            };

            return View(tasksViewModel);
        }

        // POST: Tasks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TasksVM tasksVM, IFormFile postedFile)
        {
            // Extract the task from the view model
            Models.Task task = tasksVM.Task;

            // Retrieve the ProjectId 
            int projectId = task.ProjectId;

            // Retrieve the project using the projectId
            task.Project = _context.Projects.Include(p => p.CreatedByUsernameNavigation).FirstOrDefault(x => x.ProjectId == projectId);

            // Populate the Project property of the view model using the projectId
            tasksVM.Project = _context.Projects.Include(p=>p.CreatedByUsernameNavigation).FirstOrDefault(p => p.ProjectId == projectId);


            // Retrieve the selected project member's username
            tasksVM.SelectedProjectMemberUsername = task.AssignedToUsername;

            // Set the task's AssignedToUsername to the selected project member's username
            task.AssignedToUsernameNavigation = _context.Users.FirstOrDefault(x => x.Username == tasksVM.SelectedProjectMemberUsername);

            // Set the task's AssignedToUsername to the selected project member's username
            task.AssignedToUsername = tasksVM.SelectedProjectMemberUsername;

            
            
            // Create a notification for the assigned user
            Notification notification = new Notification
            {
                Message = $"You have been assigned a new task : {task.Name} in {task.Project.Name} Project by manager {task.Project.CreatedByUsername}.",
                Type = "New Task Assignment",
                Status = "Unread",
                Username = task.AssignedToUsername
            };

            // Add the notification to the context
            _context.Notifications.Add(notification);


            //clear the modelstate and validate it
            ModelState.Clear();
            TryValidateModel(task);
           
            if (ModelState.IsValid)
            {

                //check if the user had uploaded a file
                if (postedFile != null && postedFile.Length > 0)
                {
                    //create new document object, assign values to it
                    Document doc = new Document();

                    // Get the actual name of the uploaded file
                    string fileName = Path.GetFileName(postedFile.FileName);

                    //set the properties
                    doc.DocumentName = fileName;
                    doc.Username = User.Identity.Name;
                    doc.UploadDate = DateTime.Today;
                    doc.DocumentType = postedFile.ContentType;


                    //read the file data using memory stream
                    using (MemoryStream mStream = new MemoryStream())
                    {
                        await postedFile.CopyToAsync(mStream);
                        doc.BinaryData = mStream.ToArray();

                    }

                    //add the document to the context
                    _context.Documents.Add(doc);
                    await _context.SaveChangesAsync();

                    // Assign the document ID to the task
                    task.TaskDocument = doc.DocumentId;
 

                }
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
            task.Project = _context.Projects.Include(p => p.CreatedByUsernameNavigation).FirstOrDefault(p => p.ProjectId == projectId);


            // Retrieve the user entity based on the AssignedToUsername of the task
            task.AssignedToUsernameNavigation = _context.Users.FirstOrDefault(x => x.Username == task.AssignedToUsername);



            ModelState.Clear();
            TryValidateModel(task);
            if (ModelState.IsValid)
            {
                try
                {
                    // Update the task in the context and save changes
                    _context.Update(task);
                    await _context.SaveChangesAsync();

                    // Retrieve the project manager for the corresponding project
                    var projectManager = await _context.Projects
                        .Include(p => p.CreatedByUsernameNavigation)
                        .Where(p => p.ProjectId == projectId)
                        .Select(p => p.CreatedByUsernameNavigation)
                        .FirstOrDefaultAsync();

                    if (projectManager != null)
                    {
                        // Create a new notification for the project manager
                        var newNotification = new Notification
                        {
                            Message = $"Task {task.Name} status updated to {task.Status} by project member {task.AssignedToUsername}",
                            Type = "Task Status Update",
                            Status = "Unread",
                            Username = projectManager.Username
                        };

                        // Add the new notification to the context and save changes
                        _context.Notifications.Add(newNotification);
                        await _context.SaveChangesAsync();

                    }
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
