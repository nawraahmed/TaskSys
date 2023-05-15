using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    [Table("project_members")]
    public partial class ProjectMember
    {
        [Key]
        [Column("project_members_id")]
        public int ProjectMembersId { get; set; }
        [Column("project_id")]
        public int ProjectId { get; set; }
        [Column("username")]
        [StringLength(50)]
        [Unicode(false)]
        public string Username { get; set; } = null!;

        [ForeignKey("ProjectId")]
        [InverseProperty("ProjectMembers")]
        public virtual Project Project { get; set; } = null!;
        [ForeignKey("Username")]
        [InverseProperty("ProjectMembers")]
        public virtual User UsernameNavigation { get; set; } = null!;
    }
}
