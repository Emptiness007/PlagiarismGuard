using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Models
{
    public class CurrentUser
    {
        public static CurrentUser Instance { get; set; }
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
    }
}
