using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using RowingMonitor.ViewModel;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

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

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).UseKinectJointFilter = false;
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).UseKinectJointFilter = true;
        }

        private void RadioButton_Checked_2(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).UseZVC = true;
        }

        private void RadioButton_Checked_3(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).UseZVC = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotMeasuredVariables.Add(PlotOptionsMeasuredVariables.RawPosition);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotMeasuredVariables.Remove(PlotOptionsMeasuredVariables.RawPosition);
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotMeasuredVariables.Add(PlotOptionsMeasuredVariables.SmoothedPosition);
        }

        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotMeasuredVariables.Remove(PlotOptionsMeasuredVariables.SmoothedPosition);
        }

        private void CheckBox_Checked_2(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotMeasuredVariables.Add(PlotOptionsMeasuredVariables.Velocity);
        }

        private void CheckBox_Checked_3(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotMeasuredVariables.Add(PlotOptionsMeasuredVariables.SegmentHits);
        }

        private void CheckBox_Unchecked_2(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotMeasuredVariables.Remove(PlotOptionsMeasuredVariables.Velocity);
        }

        private void CheckBox_Unchecked_3(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotMeasuredVariables.Remove(PlotOptionsMeasuredVariables.SegmentHits);
        }

        private void CheckBox_Checked_4(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.AnkleLeft);
        }

        private void CheckBox_Checked_5(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.AnkleRight);
        }

        private void CheckBox_Checked_6(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.ElbowLeft);
        }

        private void CheckBox_Checked_7(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.ElbowRight);
        }

        private void CheckBox_Checked_8(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.FootLeft);
        }

        private void CheckBox_Checked_9(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.FootRight);
        }

        private void CheckBox_Checked_10(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.HandLeft);
        }

        private void CheckBox_Checked_11(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.HandRight);
        }

        private void CheckBox_Checked_12(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.HandTipLeft);
        }

        private void CheckBox_Checked_13(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.HandTipRight);
        }

        private void CheckBox_Checked_14(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.Head);
        }

        private void CheckBox_Checked_15(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.HipLeft);
        }

        private void CheckBox_Checked_16(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.HipRight);
        }

        private void CheckBox_Checked_17(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.Neck);
        }

        private void CheckBox_Checked_18(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.KneeLeft);
        }

        private void CheckBox_Checked_19(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.KneeRight);
        }

        private void CheckBox_Checked_20(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.ShoulderLeft);
        }

        private void CheckBox_Checked_21(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.ShoulderRight);
        }

        private void CheckBox_Checked_22(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.SpineBase);
        }

        private void CheckBox_Checked_23(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.SpineMid);
        }

        private void CheckBox_Checked_24(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.SpineShoulder);
        }

        private void CheckBox_Checked_25(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.ThumbLeft);
        }

        private void CheckBox_Checked_26(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.ThumbRight);
        }

        private void CheckBox_Checked_27(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.WristLeft);
        }

        private void CheckBox_Checked_28(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Add(JointType.WristRight);
        }

        private void CheckBox_Unchecked_4(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.AnkleLeft);
        }

        private void CheckBox_Unchecked_5(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.AnkleRight);
        }

        private void CheckBox_Unchecked_6(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.ElbowLeft);
        }

        private void CheckBox_Unchecked_7(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.ElbowRight);
        }

        private void CheckBox_Unchecked_8(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.FootLeft);
        }

        private void CheckBox_Unchecked_9(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.FootRight);
        }

        private void CheckBox_Unchecked_10(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.HandLeft);
        }

        private void CheckBox_Unchecked_11(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.HandRight);
        }

        private void CheckBox_Unchecked_12(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.HandTipLeft);
        }

        private void CheckBox_Unchecked_13(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.HandTipRight);
        }

        private void CheckBox_Unchecked_14(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.Head);
        }

        private void CheckBox_Unchecked_15(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.HipLeft);
        }

        private void CheckBox_Unchecked_16(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.HipRight);
        }

        private void CheckBox_Unchecked_17(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.Neck);
        }

        private void CheckBox_Unchecked_18(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.KneeLeft);
        }

        private void CheckBox_Unchecked_19(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.KneeRight);
        }

        private void CheckBox_Unchecked_20(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.ShoulderLeft);
        }

        private void CheckBox_Unchecked_21(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.ShoulderRight);
        }

        private void CheckBox_Unchecked_22(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.SpineBase);
        }

        private void CheckBox_Unchecked_23(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.SpineMid);
        }

        private void CheckBox_Unchecked_24(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.SpineShoulder);
        }

        private void CheckBox_Unchecked_25(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.ThumbLeft);
        }

        private void CheckBox_Unchecked_26(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.ThumbRight);
        }

        private void CheckBox_Unchecked_27(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.WristLeft);
        }

        private void CheckBox_Unchecked_28(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).PlotJointTypes.Remove(JointType.WristRight);
        }
    }
}
