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
    /// Interaktionslogik für TraineeView.xaml
    /// </summary>
    public partial class TraineeView : Page
    {
        public TraineeView()
        {
            InitializeComponent();

            /* publish the grid to the view model*/
            ViewModel.MainGrid = mainGrid;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this).Navigate(new HomeView());
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewLoaded();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewUnloaded();
        }
    }
}
