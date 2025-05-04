using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Models
{
    public class Check
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Document")]
        [Column("document_id")]
        public int DocumentId { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        public float Similarity { get; set; }
        [Column("ai_generated_percentage")]
        public float? AiGeneratedPercentage { get; set; }
        [Column("checked_at")]
        public DateTime CheckedAt { get; set; }

        public Document Document { get; set; }
        public User User { get; set; }
    }
}
