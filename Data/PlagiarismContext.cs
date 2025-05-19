using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
using PlagiarismGuard.Windows;
using System.Windows;

namespace PlagiarismGuard.Data
{
    public class PlagiarismContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentText> DocumentTexts { get; set; }
        public DbSet<Check> Checks { get; set; }
        public DbSet<CheckResult> CheckResults { get; set; }
        public DbSet<LinkCheckResult> LinkCheckResults { get; set; }

        public PlagiarismContext()
        {
            try
            {
                Database.EnsureCreated();
                EnsureAdminUser();
            }
            catch (MySqlException)
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.bin");
                if (System.IO.File.Exists(configPath))
                {
                    System.IO.File.Delete(configPath);
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = GetConnectionString();
                optionsBuilder.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 11)),
                    mysqlOptions => mysqlOptions.EnableStringComparisonTranslations()
                );
            }
        }

        private static string GetConnectionString()
        {
            while (true)
            {
                string connectionString = ConfigurationManager.LoadConnectionString();

                if (string.IsNullOrEmpty(connectionString))
                {
                    var configWindow = new DatabaseConfigWindow();
                    bool? result = configWindow.ShowDialog();

                    if (result != true)
                    {
                        throw new InvalidOperationException("Настройка подключения к базе данных не завершена.");
                    }

                    connectionString = configWindow.ConnectionString;
                    ConfigurationManager.SaveConnectionString(connectionString);
                }

                try
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                    }
                    return connectionString; 
                }
                catch (MySqlException)
                {
                    string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.bin");
                    if (System.IO.File.Exists(configPath))
                    {
                        System.IO.File.Delete(configPath);
                    }
                    continue;
                }
            }
        }

        private void EnsureAdminUser()
        {
            if (!Users.Any(u => u.Role == "admin"))
            {
                var adminUser = new User
                {
                    Username = "admin",
                    Role = "admin",
                    Email = "",
                    CreatedAt = DateTime.Now
                };

                string generatedPassword = adminUser.GeneratePass();
                adminUser.PasswordHash = PasswordHelper.HashPassword(generatedPassword);

                Users.Add(adminUser);
                SaveChanges();

                CustomMessageBox.Show(
                    $"Создан пользователь администратора.\nЛогин: admin\nПароль: {generatedPassword}\nСохраните эти данные для входа!", "Успех", CustomMessageBox.MessageType.Information);
            }
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
    }
}