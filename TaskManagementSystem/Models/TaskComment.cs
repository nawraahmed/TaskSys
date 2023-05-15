using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    [Table("task_comments")]
    public partial class TaskComment
    {
        [Key]
        [Column("comment_id")]
        public int CommentId { get; set; }
        [Column("comment", TypeName = "text")]
        public string Comment { get; set; } = null!;
        [Column("task_id")]
        public int TaskId { get; set; }
        [Column("username")]
        [StringLength(50)]
        [Unicode(false)]
        public string Username { get; set; } = null!;

        [ForeignKey("TaskId")]
        [InverseProperty("TaskComments")]
        public virtual Task Task { get; set; } = null!;
        [ForeignKey("Username")]
        [InverseProperty("TaskComments")]
        public virtual User UsernameNavigation { get; set; } = null!;
    }
}
