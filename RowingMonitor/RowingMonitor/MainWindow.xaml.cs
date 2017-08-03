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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using RowingMonitor.ViewModel;
using RowingMonitor.View;

namespace RowingMonitor
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            settingsFlyout.IsOpen = true;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            mainFrame.NavigationService.Navigate(new HomeView());
        }
    }
}
