using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Models
{
    public class LinkCheckResult
    {
        public int Id { get; set; }
        [Column("check_id")]
        [ForeignKey("Check")]
        public int CheckId { get; set; }
        public string Url { get; set; }
        [Column("is_match_found")]
        public bool IsMatchFound { get; set; }
        public string Status { get; set; }
        public Check Check { get; set; }
    }
}
