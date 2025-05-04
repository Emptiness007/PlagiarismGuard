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
        [Column("check_id")]
        public int CheckId { get; set; }

        [ForeignKey("SourceDocument")]
        [Column("source_document_id")]
        public int? SourceDocumentId { get; set; }

        [Required]
        [Column("matched_text")]

        public string MatchedText { get; set; }

        public float Similarity { get; set; }

        public Check Check { get; set; }
        public Document SourceDocument { get; set; }
    }
}
