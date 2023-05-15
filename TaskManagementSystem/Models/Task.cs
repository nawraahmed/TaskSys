using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    [Table("tasks")]
    public partial class Task
    {
        public Task()
        {
            TaskComments = new HashSet<TaskComment>();
        }

        [Key]
        [Column("task_id")]
        public int TaskId { get; set; }
        [Column("name")]
        [StringLength(255)]
        [Unicode(false)]
        public string Name { get; set; } = null!;
        [Column("description", TypeName = "text")]
        public string? Description { get; set; }
        [Column("status")]
        [StringLength(50)]
        [Unicode(false)]
        public string Status { get; set; } = null!;
        [Column("deadline", TypeName = "date")]
        public DateTime? Deadline { get; set; }
        [Column("project_id")]
        public int ProjectId { get; set; }
        [Column("assigned_to_username")]
        [StringLength(50)]
        [Unicode(false)]
        public string AssignedToUsername { get; set; } = null!;
        [Column("task_document")]
        public int? TaskDocument { get; set; }

        [ForeignKey("AssignedToUsername")]
        [InverseProperty("Tasks")]
        public virtual User AssignedToUsernameNavigation { get; set; } = null!;
        [ForeignKey("ProjectId")]
        [InverseProperty("Tasks")]
        public virtual Project Project { get; set; } = null!;
        [ForeignKey("TaskDocument")]
        [InverseProperty("Tasks")]
        public virtual Document? TaskDocumentNavigation { get; set; }
        [InverseProperty("Task")]
        public virtual ICollection<TaskComment> TaskComments { get; set; }
    }
}
