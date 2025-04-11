using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Required]
        [Column(TypeName = "varchar(5)")]
        public string Role { get; set; } // "admin" или "user"

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
