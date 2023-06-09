﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Hubs;
using TaskManagementSystem.Models;
using TaskManagementSystem.ViewModels;

namespace TaskManagementSystem.Controllers
{
    public class TasksController : Controller
    {
        private readonly TaskAllocationDBContext _context;
        private readonly IHubContext<NotificationsHub> _hubcontext;

        public TasksController(TaskAllocationDBContext context, IHubContext<NotificationsHub> hubcontext)
        {
            _context = context;
            _hubcontext = hubcontext;
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
            // Update task statuses based on the deadline
            foreach (var task in tasksVM.Project.Tasks)
            {
                if (task.Status != "Completed" && task.Deadline < DateTime.Now.Date)
                {
                    task.Status = "Overdue";
                    _context.Tasks.Update(task);
                }
            }

            await _context.SaveChangesAsync();
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

            // Custom validations
            if (task.Deadline <= DateTime.Today)
            {
                ModelState.AddModelError("Task.DueDate", "Due date must be in the future.");
            }

            if (task.Name.Length < 3)
            {
                ModelState.AddModelError("Task.Name", "Name must have at least 3 characters.");
            }

            if (task.Description.Length < 3)
            {
                ModelState.AddModelError("Task.Description", "Description must have at least 3 characters.");
            }
            if (task.Description.Length > 500)
            {
                ModelState.AddModelError("Task.Description", "Description cannot exceed 500 characters.");
            }


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

                    //add a log for document uploading
                    LogsController.CreateLog(_context, "web", "Tasks/Create", User.Identity.Name, "New Document was uploaded", doc, EntityState.Added);

                    await _context.SaveChangesAsync();

                    //retreive the document id from the db
                    int documentId = _context.Documents
                    .OrderByDescending(d => d.DocumentId)
                    .Select(d => d.DocumentId)
                    .FirstOrDefault();


                    // Assign the document ID to the task
                    task.TaskDocument = documentId;
 

                }
                // Add the task to the context and save changes
                _context.Add(task);

                //add a log for task creation
                LogsController.CreateLog(_context, "web", "Tasks/Create", User.Identity.Name, "New task was created", task, EntityState.Added);


                await _context.SaveChangesAsync();

                TempData["msg"] = "New Task Assigned successfully!";


                // Create a notification for the assigned user

                var message = $"You have been assigned a new task : {task.Name} in {task.Project.Name} Project by manager {task.Project.CreatedByUsername}.";
                var type = "New Task Assignment";
                var status = "Unread";
                var username = task.AssignedToUsername;
                await NotificationsController.SendNotification(message, type, status, username, _context);
                var notification = new List<Notification> {
    new Notification { Message = message , Status = status, Type = type }
};
                if (_hubcontext != null)
                {
                    await _hubcontext.Clients.All.SendAsync("getUpdatedNotifications", notification);
                }


                // Redirect to the index action of the tasks controller, passing the projectId as a route value
                return RedirectToAction(nameof(Index),new { projectId = task.ProjectId });
            }
            // Retrieve the project members for the selected project
            tasksVM.ProjectMembers = _context.ProjectMembers.Where(p => p.ProjectId == projectId);


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

            var task = await _context.Tasks
                .Include(t => t.TaskDocumentNavigation)
                .FirstOrDefaultAsync(m => m.TaskId == id);

            if (task == null)
            {
                return NotFound();
            }

            var projectId = task.ProjectId;

            var tasksViewModel = new TasksVM
            {
                Task = task,
                Project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId),
                ProjectMembers = _context.ProjectMembers.Where(p => p.ProjectId == projectId),
                Document = new()
            };

            ViewData["AssignedToUsername"] = new SelectList(_context.ProjectMembers.Where(x=>x.ProjectId==projectId), "Username", "Username", task.AssignedToUsername);
           // ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectId", task.ProjectId);
            ViewData["TaskDocument"] = new SelectList(_context.Documents, "DocumentId", "DocumentId", task.TaskDocument);
        return(View(tasksViewModel));
        }

        // POST: Tasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TasksVM tasksVM, IFormFile? Document)
        {
            // Extract the task from the view model
            Models.Task task = tasksVM.Task;

            // Retrieve the ProjectId 
            int projectId = task.ProjectId;

            // Retrieve the project using the projectId
            task.Project = _context.Projects.Include(p => p.CreatedByUsernameNavigation).FirstOrDefault(x => x.ProjectId == projectId);

            // Populate the Project property of the view model using the projectId
            tasksVM.Project = _context.Projects.Include(p => p.CreatedByUsernameNavigation).FirstOrDefault(p => p.ProjectId == projectId);


            // Retrieve the selected project member's username
            tasksVM.SelectedProjectMemberUsername = task.AssignedToUsername;

            // Set the task's AssignedToUsername to the selected project member's username
            task.AssignedToUsernameNavigation = _context.Users.FirstOrDefault(x => x.Username == tasksVM.SelectedProjectMemberUsername);

            // Set the task's AssignedToUsername to the selected project member's username
            task.AssignedToUsername = tasksVM.SelectedProjectMemberUsername;


            ModelState.Clear();
            TryValidateModel(task);
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if a document already exists to handle deletion
                    if (tasksVM.Document != null && tasksVM.Document.BinaryData.Length > 0)
                    {
                        // Delete the existing document, if any
                        if (task.TaskDocumentNavigation != null)
                        {
                            // Remove the document from the documents table first
                            // Then remove the document from the tasks table as well
                            _context.Documents.Remove(task.TaskDocumentNavigation);

                            //add a log for document deletion
                            LogsController.CreateLog(_context, "web", "Tasks/Edit", User.Identity.Name, "A document was deleted", task.TaskDocumentNavigation, EntityState.Deleted);

                            await _context.SaveChangesAsync();
                        }


                    }
                    // Check if a new document is being uploaded
                    if (Document != null)
                    {

                        string fileName = Path.GetFileName(Document.FileName);

                        // Create a new document entity
                        var document = new Document
                        {
                            DocumentName = fileName,
                            Username = User.Identity.Name,
                            UploadDate = DateTime.Today,
                            DocumentType = Document.ContentType

                        };

                        //read the file data using memory stream
                        using (MemoryStream mStream = new MemoryStream())
                        {
                            await Document.CopyToAsync(mStream);
                            document.BinaryData = mStream.ToArray();

                        }

                        //add the document to the context
                        _context.Documents.Add(document);

                        //add a log for document uploading
                        LogsController.CreateLog(_context, "web", "Tasks/Edit", User.Identity.Name, "New document was uploaded", document, EntityState.Added);

                        await _context.SaveChangesAsync();

                        // Assign the document ID to the task
                        task.TaskDocument = document.DocumentId;

                        
                    }

                    // Check if the status has changed
                    bool statusChanged = false;

                    var originalTask = _context.Tasks.AsNoTracking().FirstOrDefault(t => t.TaskId == task.TaskId);
                    if (originalTask != null && originalTask.Status != task.Status)
                    {
                        statusChanged = true;
                    }

                    // Update the task in the context
                    _context.Update(task);

                    //add a log for task updating
                    LogsController.CreateLog(_context, "web", "Tasks/Edit", User.Identity.Name, "A task was updated", task, EntityState.Modified);


                    await _context.SaveChangesAsync();

                    TempData["msg"] = "Task Updated successfully.";

                    if (statusChanged)
                    {
                        // Retrieve the project manager for the corresponding project
                        var projectManager = await _context.Projects
                            .Include(p => p.CreatedByUsernameNavigation)
                            .Where(p => p.ProjectId == projectId)
                            .Select(p => p.CreatedByUsernameNavigation)
                            .FirstOrDefaultAsync();

                        if (projectManager != null)
                        {
                            // Create a new notification for the project manager

                            var message = $"Task {task.Name} status updated to {task.Status} by project member {task.AssignedToUsername}";
                            var type = "Task Status Update";
                            var status = "Unread";
                            var username = projectManager.Username;
                            await NotificationsController.SendNotification(message, type, status, username, _context);
                            var notification = new List<Notification> {
  new Notification { Message = message , Status = status, Type = type }
};
                            if (_hubcontext != null)
                            {
                                await _hubcontext.Clients.All.SendAsync("getUpdatedNotifications", notification);
                            }


                        }
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
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                LogsController.CreateLogExceptions(_context, User.Identity.Name, "Tasks/Delete", "Task was not found");
                return NotFound();
            }

            // Delete the associated records in the referencing table
            var associatedRecords = await _context.TaskComments.Where(r => r.TaskId == id).ToListAsync();
            _context.TaskComments.RemoveRange(associatedRecords);

            
            // Delete the task from the database
            _context.Tasks.Remove(task);

            //add a log for task removal
            LogsController.CreateLog(_context, "web", "Tasks/Delete", User.Identity.Name, "A task was removed", task, EntityState.Deleted);

            await _context.SaveChangesAsync();

            TempData["msg"] = "Task Deleted successfully!";
            return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
        }

        private bool TaskExists(int id)
        {
            return (_context.Tasks?.Any(e => e.TaskId == id)).GetValueOrDefault();
        }





        // GET: Tasks/DeleteDocument/5
        public async Task<IActionResult> DeleteDocument(int? id)
        {
            if (id == null)
            {
                return NotFound(); // No ID specified, return Not Found
            }

            var document = await _context.Documents.FindAsync(id);

            if (document == null)
            {
                return NotFound(); // Document not found, return Not Found
            }

            // Get the first task associated with the document
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskDocument == document.DocumentId);

            if (task == null)
            {
                LogsController.CreateLogExceptions(_context, User.Identity.Name, "Tasks/DeleteDocument", "Task was not found");
                return NotFound(); // Task not found, return Not Found
            }

            // Pass both the document and task ID to the view
            var viewModel = new DeleteDocumentViewModel
            {
                Document = document,
                TaskId = task.TaskId
            };

            return View(viewModel); // Render the view with the document and task ID
        }



        // POST: Tasks/DeleteDocumentConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocumentConfirmed(int id)
        {
            var document = await _context.Documents.FindAsync(id);

            if (document == null)
            {
                return NotFound();
            }

            // Get the associated tasks and set the task_document value to null
            var tasks = await _context.Tasks.Where(t => t.TaskDocument == id).ToListAsync();
            foreach (var task in tasks)
            {
                task.TaskDocument = null;
                _context.Tasks.Update(task); // Update the task in the context
            }

            // Delete the document from the database
            _context.Documents.Remove(document);

           
            await _context.SaveChangesAsync();

            // Redirect to the Edit action of TasksController with the ID of the first associated task

                return RedirectToAction("Edit", "Tasks", new { id = tasks[0].TaskId });
            
        }





    }
}
