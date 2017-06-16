using Microsoft.Kinect;
using RowingMonitor.Model.Util;
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

namespace RowingMonitor
{
    /// <summary>
    /// Interaktionslogik für RowingMonitorWindow.xaml
    /// </summary>
    public partial class RowingMonitorWindow : Window
    {
        public RowingMonitorWindow()
        {
            InitializeComponent();

            ViewModel.Frame1 = frame1;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.UseKinectJointFilter = false;
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            ViewModel.UseKinectJointFilter = true;
        }

        private void RadioButton_Checked_2(object sender, RoutedEventArgs e)
        {
            ViewModel.UseZVC = true;
        }

        private void RadioButton_Checked_3(object sender, RoutedEventArgs e)
        {
            ViewModel.UseZVC = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Add(DataStreamType.RawPosition);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Remove(DataStreamType.RawPosition);
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Add(DataStreamType.SmoothedPosition);
        }

        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Remove(DataStreamType.SmoothedPosition);
        }

        private void CheckBox_Checked_2(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Add(DataStreamType.Velocity);
        }

        private void CheckBox_Checked_3(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Add(DataStreamType.SegmentHits);
        }

        private void CheckBox_Unchecked_2(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Remove(DataStreamType.Velocity);
        }

        private void CheckBox_Unchecked_3(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Remove(DataStreamType.SegmentHits);
        }

        private void CheckBox_Checked_4(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.AnkleLeft);
        }

        private void CheckBox_Checked_5(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.AnkleRight);
        }

        private void CheckBox_Checked_6(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.ElbowLeft);
        }

        private void CheckBox_Checked_7(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.ElbowRight);
        }

        private void CheckBox_Checked_8(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.FootLeft);
        }

        private void CheckBox_Checked_9(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.FootRight);
        }

        private void CheckBox_Checked_10(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.HandLeft);
        }

        private void CheckBox_Checked_11(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.HandRight);
        }

        private void CheckBox_Checked_12(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.HandTipLeft);
        }

        private void CheckBox_Checked_13(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.HandTipRight);
        }

        private void CheckBox_Checked_14(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.Head);
        }

        private void CheckBox_Checked_15(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.HipLeft);
        }

        private void CheckBox_Checked_16(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.HipRight);
        }

        private void CheckBox_Checked_17(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.Neck);
        }

        private void CheckBox_Checked_18(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.KneeLeft);
        }

        private void CheckBox_Checked_19(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.KneeRight);
        }

        private void CheckBox_Checked_20(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.ShoulderLeft);
        }

        private void CheckBox_Checked_21(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.ShoulderRight);
        }

        private void CheckBox_Checked_22(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.SpineBase);
        }

        private void CheckBox_Checked_23(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.SpineMid);
        }

        private void CheckBox_Checked_24(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.SpineShoulder);
        }

        private void CheckBox_Checked_25(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.ThumbLeft);
        }

        private void CheckBox_Checked_26(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.ThumbRight);
        }

        private void CheckBox_Checked_27(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.WristLeft);
        }

        private void CheckBox_Checked_28(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Add(JointType.WristRight);
        }

        private void CheckBox_Unchecked_4(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.AnkleLeft);
        }

        private void CheckBox_Unchecked_5(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.AnkleRight);
        }

        private void CheckBox_Unchecked_6(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.ElbowLeft);
        }

        private void CheckBox_Unchecked_7(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.ElbowRight);
        }

        private void CheckBox_Unchecked_8(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.FootLeft);
        }

        private void CheckBox_Unchecked_9(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.FootRight);
        }

        private void CheckBox_Unchecked_10(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.HandLeft);
        }

        private void CheckBox_Unchecked_11(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.HandRight);
        }

        private void CheckBox_Unchecked_12(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.HandTipLeft);
        }

        private void CheckBox_Unchecked_13(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.HandTipRight);
        }

        private void CheckBox_Unchecked_14(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.Head);
        }

        private void CheckBox_Unchecked_15(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.HipLeft);
        }

        private void CheckBox_Unchecked_16(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.HipRight);
        }

        private void CheckBox_Unchecked_17(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.Neck);
        }

        private void CheckBox_Unchecked_18(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.KneeLeft);
        }

        private void CheckBox_Unchecked_19(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.KneeRight);
        }

        private void CheckBox_Unchecked_20(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.ShoulderLeft);
        }

        private void CheckBox_Unchecked_21(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.ShoulderRight);
        }

        private void CheckBox_Unchecked_22(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.SpineBase);
        }

        private void CheckBox_Unchecked_23(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.SpineMid);
        }

        private void CheckBox_Unchecked_24(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.SpineShoulder);
        }

        private void CheckBox_Unchecked_25(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.ThumbLeft);
        }

        private void CheckBox_Unchecked_26(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.ThumbRight);
        }

        private void CheckBox_Unchecked_27(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.WristLeft);
        }

        private void CheckBox_Unchecked_28(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotJointTypes.Remove(JointType.WristRight);
        }

        private void CheckBox_Checked_29(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Add(DataStreamType.ShiftedPosition);
        }

        private void CheckBox_Unchecked_29(object sender, RoutedEventArgs e)
        {
            ViewModel.PlotMeasuredVariables.Remove(DataStreamType.ShiftedPosition);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.WindowLoaded();
        }
    }
}
