﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Areas.Identity.Data;
using TaskManagementSystem.Models;
using TaskManagementSystem.ViewModels;

namespace TaskManagementSystem.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly TaskAllocationDBContext _context;


        public ProjectsController(TaskAllocationDBContext context)
        {
            _context = context;

        }



        public IActionResult GetUsersSelectList()
        {
            var users = _context.Users.Select(m => new SelectListItem { Text = m.Username }).ToList();
            return Json(users);
        }





        // GET: Projects
        public async Task<IActionResult> Index(string search)
        {
            //check if either the logged in user is a project member or the project is created by him!
            IQueryable<Project> taskAllocationDBContext = _context.Projects
             .Include(p => p.CreatedByUsernameNavigation)
             .Where(p => p.ProjectMembers.Any(m => m.Username == User.Identity.Name) || p.CreatedByUsername == User.Identity.Name);


            if (!string.IsNullOrEmpty(search))
            {
                taskAllocationDBContext = taskAllocationDBContext.Where(p => p.Name.Contains(search));

            }
            return View(await taskAllocationDBContext.ToListAsync());
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

                    }

                    await _context.SaveChangesAsync();
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


                //update the projects table
                _context.Projects.Update(existingProject);
                await _context.SaveChangesAsync();


                TempData["UpdateSuccess"] = "Project Updated Successfully";
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


                //remove the documents

                //remove the project tasks
                // Retrieve the project tasks associated with the project
                var projectTasks = await _context.Tasks
                    .Where(t => t.ProjectId == id)
                    .ToListAsync();

                // Remove the project tasks
                foreach (var task in projectTasks)
                {
                    _context.Tasks.Remove(task);
                }


                //remove the project members
                // Retrieve the project members associated with the project
                var projectMembers = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == id)
                    .ToListAsync();

                // Remove the project members
                foreach (var projectMember in projectMembers)
                {
                    _context.ProjectMembers.Remove(projectMember);
                }

                //then remove the project itself
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return (_context.Projects?.Any(e => e.ProjectId == id)).GetValueOrDefault();
        }

    }

}
