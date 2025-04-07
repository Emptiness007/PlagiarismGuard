using Microsoft.EntityFrameworkCore;
using PlagiarismGuard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Data
{
    public class PlagiarismContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentText> DocumentTexts { get; set; }
        public DbSet<Check> Checks { get; set; }
        public DbSet<CheckResult> CheckResults { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("Server=127.0.0.1;Database=Plagiarism;Port=3307;user=root;pwd=;", new MySqlServerVersion(new Version(8, 0, 11)));
        }
    }
}
