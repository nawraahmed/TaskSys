﻿using TaskManagementSystem.Models;

namespace TaskManagementSystem.ViewModels
{
    public class TasksVM
    {

        public Project Project { get; set; }
        public Models.Task Task { get; set; }
        public IEnumerable<ProjectMember> ProjectMembers { get; set; }
        public string SelectedProjectMemberUsername { get; set; }

        public string Tab { get; set; }

        public Document Document { get; set; }



    }
}
