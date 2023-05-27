using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    [Table("users")]
    public partial class User
    {
        public User()
        {
            Documents = new HashSet<Document>();
            Logs = new HashSet<Log>();
            Notifications = new HashSet<Notification>();
            ProjectMembers = new HashSet<ProjectMember>();
            Projects = new HashSet<Project>();
            TaskComments = new HashSet<TaskComment>();
            Tasks = new HashSet<Task>();
        }

        [Key]
        [Column("username")]
        [StringLength(50)]
        [Unicode(false)]
        public string Username { get; set; } = null!;

        [InverseProperty("UsernameNavigation")]
        public virtual ICollection<Document> Documents { get; set; }
        [InverseProperty("UsernameNavigation")]
        public virtual ICollection<Log> Logs { get; set; }
        [InverseProperty("UsernameNavigation")]
        public virtual ICollection<Notification> Notifications { get; set; }
        [InverseProperty("UsernameNavigation")]
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; }
        [InverseProperty("CreatedByUsernameNavigation")]
        public virtual ICollection<Project> Projects { get; set; }
        [InverseProperty("UsernameNavigation")]
        public virtual ICollection<TaskComment> TaskComments { get; set; }
        [InverseProperty("AssignedToUsernameNavigation")]
        public virtual ICollection<Task> Tasks { get; set; }


    }
}
