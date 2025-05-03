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
        public string Role { get; set; } 

        [MaxLength(100)]
        public string? Email { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        public string GeneratePass()
        {
            Random rnd = new Random();
            char[] numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            char[] symbols = { '|', '-', '_', '!', '@', '#', '$', '%', '&', '*', '=', '+' };
            char[] uppercase = { 'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P', 'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'Z', 'X', 'C', 'V', 'B', 'N', 'M' };
            char[] lowercase = { 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'z', 'x', 'c', 'v', 'b', 'n', 'm' };

            var password = new List<char>();
            password.Add(numbers[rnd.Next(numbers.Length)]);
            password.Add(numbers[rnd.Next(numbers.Length)]);
            password.Add(symbols[rnd.Next(symbols.Length)]);
            password.Add(uppercase[rnd.Next(uppercase.Length)]);
            password.Add(uppercase[rnd.Next(uppercase.Length)]);
            password.Add(lowercase[rnd.Next(lowercase.Length)]);
            password.Add(lowercase[rnd.Next(lowercase.Length)]);
            password.Add(lowercase[rnd.Next(lowercase.Length)]);

            for (int i = 0; i < password.Count; i++)
            {
                int randomIndex = rnd.Next(0, password.Count);
                char temp = password[randomIndex];
                password[randomIndex] = password[i];
                password[i] = temp;
            }

            return new string(password.ToArray());
        }
    }
}
