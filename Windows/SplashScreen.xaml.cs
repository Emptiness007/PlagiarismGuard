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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PlagiarismGuard.Windows
{
    /// <summary>
    /// Логика взаимодействия для SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            Loaded += SplashScreen_Loaded;
        }

        private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = (Storyboard)FindResource("FadeInAnimation");
            fadeIn.Begin(this);

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = System.TimeSpan.FromSeconds(3);
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                var fadeOut = (Storyboard)FindResource("FadeOutAnimation");
                fadeOut.Begin(this);
            };
            timer.Start();
        }

        private void FadeOutAnimation_Completed(object sender, System.EventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
