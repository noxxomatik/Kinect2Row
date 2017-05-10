using Microsoft.Kinect;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using Xceed.Wpf.Toolkit;

namespace RowingMonitor
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // bind view model to view
            DataContext = new MainViewModel();           

            // initialize the components (controls) of the window
            InitializeComponent();
        }

        private void betaUpDown_Spin(object sender, Xceed.Wpf.Toolkit.SpinEventArgs e)
        {
            ButtonSpinner spinner = (ButtonSpinner) sender;
            TextBox textBox = (TextBox) spinner.Content;

            if (e.Direction == SpinDirection.Increase) {
                ((MainViewModel)DataContext).Beta += 0.001;
            }
            else {
                ((MainViewModel) DataContext).Beta -= 0.001;
            }
        }

        private void fcminUpDown_Spin(object sender, Xceed.Wpf.Toolkit.SpinEventArgs e)
        {
            ButtonSpinner spinner = (ButtonSpinner) sender;
            TextBox textBox = (TextBox) spinner.Content;

            if (e.Direction == SpinDirection.Increase) {
                ((MainViewModel) DataContext).Fcmin += 1.0;
            }
            else {
                ((MainViewModel) DataContext).Fcmin -= 1.0;
            }
        }
    }
}
