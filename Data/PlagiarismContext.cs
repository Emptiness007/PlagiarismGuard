using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlagiarismGuard.Models;

namespace PlagiarismGuard.Data
{
    public class PlagiarismContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentText> DocumentTexts { get; set; }
        public DbSet<Check> Checks { get; set; }
        public DbSet<CheckResult> CheckResults { get; set; }
        public DbSet<LinkCheckResult> LinkCheckResults { get; set; }

        public PlagiarismContext() : this(GetConnectionString())
        {
        }

        public PlagiarismContext(string connectionString)
        {
            _connectionString = connectionString;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_connectionString, new MySqlServerVersion(new Version(8, 0, 11)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>()
                .HasColumnType("varchar(5)")
                .HasDefaultValue("user")
                .IsRequired();

            modelBuilder.Entity<Document>()
                .Property(d => d.Format)
                .HasConversion<string>()
                .HasColumnType("varchar(4)")
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Связи
            modelBuilder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentText>()
                .HasOne(dt => dt.Document)
                .WithMany()
                .HasForeignKey(dt => dt.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Check>()
                .HasOne(c => c.Document)
                .WithMany()
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Check>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CheckResult>()
                .HasOne(cr => cr.Check)
                .WithMany()
                .HasForeignKey(cr => cr.CheckId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CheckResult>()
                .HasOne(cr => cr.SourceDocument)
                .WithMany()
                .HasForeignKey(cr => cr.SourceDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

        }

        public static string GetConnectionString()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            return configuration.GetConnectionString("PlagiarismDatabase");
        }
    }
}