using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Models
{
    public class DocumentText
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Document")]
        public int DocumentId { get; set; }

        [Required]
        [Column(TypeName = "longtext")]
        public string TextContent { get; set; }

        public DateTime ProcessedAt { get; set; }

        // Навигационное свойство
        public Document Document { get; set; }
    }
}
