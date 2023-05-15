using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    [Table("logs")]
    public partial class Log
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }
        [Column("source")]
        [StringLength(255)]
        [Unicode(false)]
        public string Source { get; set; } = null!;
        [Column("type")]
        [StringLength(50)]
        [Unicode(false)]
        public string Type { get; set; } = null!;
        [Column("username")]
        [StringLength(50)]
        [Unicode(false)]
        public string Username { get; set; } = null!;
        [Column("date", TypeName = "datetime")]
        public DateTime Date { get; set; }
        [Column("message", TypeName = "text")]
        public string? Message { get; set; }
        [Column("original_value", TypeName = "text")]
        public string? OriginalValue { get; set; }
        [Column("current_value", TypeName = "text")]
        public string? CurrentValue { get; set; }

        [ForeignKey("Username")]
        [InverseProperty("Logs")]
        public virtual User UsernameNavigation { get; set; } = null!;
    }
}
