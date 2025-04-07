using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Models
{
    public class CheckResult
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Check")]
        public int CheckId { get; set; }

        [ForeignKey("SourceDocument")]
        public int SourceDocumentId { get; set; }

        [Required]
        [Column(TypeName = "longtext")]
        public string MatchedText { get; set; }

        public float Similarity { get; set; }

        // Навигационные свойства
        public Check Check { get; set; }
        public Document SourceDocument { get; set; }
    }
}
