using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PlagiarismGuard.Data;
using PlagiarismGuard.Services;
using PlagiarismGuard.Models;
using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Configuration;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace PlagiarismGuard
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            services.AddDbContext<PlagiarismContext>(options =>
                options.UseMySql(configuration.GetConnectionString("PlagiarismDatabase"), new MySqlServerVersion(new Version(8, 0, 11))));
            services.AddScoped<TextExtractorService>();
            services.AddScoped<PlagiarismCheckService>();
            services.AddScoped<ReportGeneratorService>();
            ServiceProvider = services.BuildServiceProvider();

            SeedAdminUser();


        }

        private void SeedAdminUser()
        {
            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<PlagiarismContext>();

            if (!context.Users.Any(u => u.Role == "admin"))
            {
                var admin = new User
                {
                    Username = "admin",
                    Email = "",
                    PasswordHash = PasswordHelper.HashPassword("admin123"),
                    Role = "admin",
                    CreatedAt = DateTime.UtcNow
                };


                context.Users.Add(admin);
                context.SaveChanges();

            }
        }
    }
}