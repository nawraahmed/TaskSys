using System;
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
            var users = _context.Users.Select(m => new SelectListItem {Text = m.Username }).ToList();
            return Json(users);
        }

        



        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var taskAllocationDBContext = _context.Projects.Include(p => p.CreatedByUsernameNavigation);
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
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {

            var pviewModel = new ProjectsUsersVM
            {
                Project = new(),
                Users = _context.Users
            };
            return View(pviewModel);
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,Name,Description,Deadline,Budget,CreatedByUsername,Status, SelectedMembers, Tasks, project_members_id")] Project project, String[] selectedMembers)
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


                //insert to project members table if the array is not empty 
                if (selectedMembers != null && selectedMembers.Length > 0)
                {

                    //loop to insert all the members in the table
                    foreach (var memberName in selectedMembers)
                    {

                        //assign the values to a new project member 
                        var projectMember = new ProjectMember
                        {
                            ProjectId = project.ProjectId,
                            Username = memberName.Trim() //triming the names (if more than one is inserted)
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

            var pviewModel = new ProjectsUsersVM
            {
                Project = project,
                Users = _context.Users
            };

            ViewData["CreatedByUsername"] = new SelectList(_context.Users, "Username", "Username", project.CreatedByUsername);
            return View(pviewModel);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectsUsersVM editProject)
        {
            if (id != editProject.Project.ProjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {

                _context.Update(editProject.Project);
                    await _context.SaveChangesAsync();
                    TempData["CreateSuccess"] = "Project Updated Successfully";
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
