using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("file_name")]
        public string FileName { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("file_path")]
        public string FilePath { get; set; }
        [Column("file_size")]

        public long FileSize { get; set; }
        [Column("uploaded_at")]

        public DateTime UploadedAt { get; set; }

        [Required]
        [Column(TypeName = "varchar(4)")]
        public string Format { get; set; }

        // Навигационное свойство
        public User User { get; set; }
    }
}
