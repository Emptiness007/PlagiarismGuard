using Microsoft.EntityFrameworkCore;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
using PlagiarismGuard.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static PlagiarismGuard.Windows.CustomMessageBox;

namespace PlagiarismGuard.Pages
{
    /// <summary>
    /// Логика взаимодействия для HistoryPage.xaml
    /// </summary>
    public partial class HistoryPage : Page
    {
        private readonly PlagiarismContext _context;
        private readonly ReportGeneratorService _reportGenerator;

        public HistoryPage(PlagiarismContext context, ReportGeneratorService reportGenerator)
        {
            InitializeComponent();
            _context = context;
            _reportGenerator = reportGenerator;
            LoadChecks();
        }

        private void LoadChecks()
        {
            if (CurrentUser.Instance.Role == "admin")
            {
                var checks = _context.Checks
                    .Include(c => c.Document)
                    .Include(c => c.User)
                    .ToList();
                ChecksDataGrid.ItemsSource = checks;
            }
            else
            {
                var checks = _context.Checks
                    .Include(c => c.Document)
                    .Include(c => c.User)
                    .Where(c => c.UserId == CurrentUser.Instance.Id)
                    .ToList();
                ChecksDataGrid.ItemsSource = checks;
            }
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                int checkId = (int)button.Tag;
                var check = _context.Checks.FirstOrDefault(c => c.Id == checkId);

                if (check == null)
                {
                    CustomMessageBox.Show("Проверка не найдена!", "Ошибка", MessageType.Error, Window.GetWindow(this));
                    return;
                }

                var results = _context.CheckResults
                    .Where(cr => cr.CheckId == checkId)
                    .ToList();
                var linkResults = _context.LinkCheckResults
                                .Where(lcr => lcr.CheckId == checkId)
                                .ToList();

                _reportGenerator.GeneratePlagiarismReport(check, results, linkResults);
            }
        }
    }
}
