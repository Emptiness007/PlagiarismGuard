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
        [Column("text_content")]
        public string TextContent { get; set; }
        [Column("processed_at")]

        public DateTime ProcessedAt { get; set; }

        public Document Document { get; set; }
    }
}
