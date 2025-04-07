using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Check")]
        public int CheckId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FilePath { get; set; }

        public DateTime GeneratedAt { get; set; }

        // Навигационное свойство
        public Check Check { get; set; }
    }
}
