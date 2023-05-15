﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    [Table("notifications")]
    public partial class Notification
    {
        [Key]
        [Column("notification_id")]
        public int NotificationId { get; set; }
        [Column("message")]
        [StringLength(255)]
        [Unicode(false)]
        public string Message { get; set; } = null!;
        [Column("type")]
        [StringLength(50)]
        [Unicode(false)]
        public string? Type { get; set; }
        [Column("status")]
        [StringLength(50)]
        [Unicode(false)]
        public string? Status { get; set; }
        [Column("username")]
        [StringLength(50)]
        [Unicode(false)]
        public string Username { get; set; } = null!;

        [ForeignKey("Username")]
        [InverseProperty("Notifications")]
        public virtual User UsernameNavigation { get; set; } = null!;
    }
}
