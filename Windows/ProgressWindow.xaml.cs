using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace PlagiarismGuard.Windows
{
    /// <summary>
    /// Логика взаимодействия для ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private readonly Action _cancelAction;

        public ProgressWindow(Window owner, Action cancelAction = null)
        {
            InitializeComponent();
            Owner = owner;
            _cancelAction = cancelAction;
            CancelButton.Visibility = cancelAction != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateProgress(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = message;
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelAction?.Invoke();
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _cancelAction?.Invoke();
        }
    }
}
