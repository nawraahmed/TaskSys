using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    [Table("documents")]
    public partial class Document
    {
        public Document()
        {
            Tasks = new HashSet<Task>();
        }

        [Key]
        [Column("document_id")]
        public int DocumentId { get; set; }
        [Column("document_name")]
        [StringLength(255)]
        [Unicode(false)]
        public string DocumentName { get; set; } = null!;
        [Column("username")]
        [StringLength(50)]
        [Unicode(false)]
        public string Username { get; set; } = null!;
        [Column("upload_date", TypeName = "datetime")]
        public DateTime UploadDate { get; set; }
        [Column("document_type")]
        [StringLength(50)]
        [Unicode(false)]
        public string? DocumentType { get; set; }
        [Column("binary_data")]
        public byte[]? BinaryData { get; set; }

        [ForeignKey("Username")]
        [InverseProperty("Documents")]
        public virtual User UsernameNavigation { get; set; } = null!;
        [InverseProperty("TaskDocumentNavigation")]
        public virtual ICollection<Task> Tasks { get; set; }
    }
}
