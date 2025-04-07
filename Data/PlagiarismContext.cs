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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка enum для ролей пользователей
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>()
                .HasColumnType("varchar(5)")
                .HasDefaultValue("user")
                .IsRequired();

            // Настройка enum для форматов документов
            modelBuilder.Entity<Document>()
                .Property(d => d.Format)
                .HasConversion<string>()
                .HasColumnType("varchar(4)")
                .IsRequired();

            // Уникальные ограничения
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

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Check)
                .WithMany()
                .HasForeignKey(r => r.CheckId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
