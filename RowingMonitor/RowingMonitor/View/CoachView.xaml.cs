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
    /// Interaktionslogik für CoachView.xaml
    /// </summary>
    public partial class CoachView : Page
    {
        public CoachView()
        {
            InitializeComponent();

            /* publish the grid to the view model*/
            ViewModel.MainGrid = MainGrid;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewLoaded();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewUnloaded();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this).Navigate(new HomeView());
        }
    }
}
