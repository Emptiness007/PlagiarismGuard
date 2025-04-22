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
        [Column("check_id")]
        public int CheckId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("file_path")]
        public string FilePath { get; set; }
        [Column("generated_at")]

        public DateTime GeneratedAt { get; set; }

        public Check Check { get; set; }
    }
}
