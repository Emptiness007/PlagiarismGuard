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
        public int DocumentId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public float Similarity { get; set; }

        public DateTime CheckedAt { get; set; }

        // Навигационные свойства
        public Document Document { get; set; }
        public User User { get; set; }
    }
}
