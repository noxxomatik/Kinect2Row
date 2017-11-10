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
    /// Interaktionslogik für SkeletonFrontalView.xaml
    /// </summary>
    public partial class SkeletonSideView : UserControl
    {
        public SkeletonSideView()
        {
            InitializeComponent();
        }

        private void Viewbox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ((SkeletonSideViewModel)DataContext).Model.AreaWidth += e.Delta / 100 * 0.1f;
        }
    }
}
