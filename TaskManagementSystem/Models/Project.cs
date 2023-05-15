using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    [Table("projects")]
    public partial class Project
    {
        public Project()
        {
            ProjectMembers = new HashSet<ProjectMember>();
            Tasks = new HashSet<Task>();
        }

        [Key]
        [Column("project_id")]
        public int ProjectId { get; set; }
        [Column("name")]
        [StringLength(255)]
        [Unicode(false)]
        public string Name { get; set; } = null!;
        [Column("description", TypeName = "text")]
        public string? Description { get; set; }
        [Column("deadline", TypeName = "date")]
        public DateTime? Deadline { get; set; }
        [Column("budget", TypeName = "decimal(10, 2)")]
        public decimal? Budget { get; set; }
        [Column("created_by_username")]
        [StringLength(50)]
        [Unicode(false)]
        public string CreatedByUsername { get; set; } = null!;
        [Column("status")]
        [StringLength(50)]
        [Unicode(false)]
        public string? Status { get; set; }

        [ForeignKey("CreatedByUsername")]
        [InverseProperty("Projects")]
        public virtual User CreatedByUsernameNavigation { get; set; } = null!;
        [InverseProperty("Project")]
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; }
        [InverseProperty("Project")]
        public virtual ICollection<Task> Tasks { get; set; }
    }
}
