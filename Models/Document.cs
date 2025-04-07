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
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(255)]
        public string FilePath { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; }

        [Required]
        [Column(TypeName = "varchar(4)")]
        public string Format { get; set; }

        // Навигационное свойство
        public User User { get; set; }
    }
}
