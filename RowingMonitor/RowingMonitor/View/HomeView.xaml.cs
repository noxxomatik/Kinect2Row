using RowingMonitor.ViewModel;
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

namespace RowingMonitor.View
{
    /// <summary>
    /// Interaktionslogik für HomeView.xaml
    /// </summary>
    public partial class HomeView : Page
    {
        public HomeView()
        {
            InitializeComponent();
            DataContext = new HomeViewModel();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ((HomeViewModel)DataContext).GUIGrid = calibrationGrid;
            ((HomeViewModel)DataContext).ViewLoaded();
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this).Navigate(new DebugView());
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // dispose all links when page is unloaded
            ((HomeViewModel)DataContext).ViewUnloaded();
        }
    }
}
