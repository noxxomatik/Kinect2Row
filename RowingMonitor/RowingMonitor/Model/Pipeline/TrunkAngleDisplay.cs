using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using RowingMonitor.View;
using RowingMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Media;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// TrunkAngleDisplay visualizes the angle between the upper body and the vertical line.
    /// </summary>
    public class TrunkAngleDisplay
    {
        private ActionBlock<JointData> input;

        private TrunkAngleViewModel viewModel;
        private TrunkAngleView view;

        private double maxCatchAngle;
        private double maxFinishAngle;
        private double currentAngle;

        public TrunkAngleDisplay()
        {
            View = new TrunkAngleView();
            ViewModel = (TrunkAngleViewModel)View.DataContext;

            Input = new ActionBlock<JointData>(jointData => {
                UpdateTrunkAngle(jointData);
            });
        }

        public void Render()
        {            
            View.Dispatcher.BeginInvoke(new Action(() =>
            {
                // TODO delete all obsolte functions
                ViewModel.Render(Degrees.RadianToDegree(maxCatchAngle),
                    Degrees.RadianToDegree(maxFinishAngle), 
                    Degrees.RadianToDegree(currentAngle));
            }));
        }

        private void UpdateTrunkAngle(JointData jointData)
        {
            // SpineBase is the new origin
            Point origin = new Point();
            origin.X = jointData.Joints[JointType.SpineBase].Position.Z;
            origin.Y = jointData.Joints[JointType.SpineBase].Position.Y;

            // calculate vector shoulder from SpineShoulder with the new origin
            Point shoulder = new Point();
            shoulder.X = jointData.Joints[JointType.SpineShoulder].Position.Z - origin.X;
            shoulder.Y = jointData.Joints[JointType.SpineShoulder].Position.Y - origin.Y;

            // calculate angle between shoulder vector and vector (0, 1)
            double tmpCurrentAngle = Math.Atan2(shoulder.X, shoulder.Y);
            
            // check if current angle exceeded 0
            if (tmpCurrentAngle * currentAngle < 0) {
                if (tmpCurrentAngle < 0) {
                    maxCatchAngle = 0;
                }
                if (tmpCurrentAngle > 0) {
                    maxFinishAngle = 0;
                }
            }
            if (tmpCurrentAngle < 0) {
                maxCatchAngle = tmpCurrentAngle < maxCatchAngle ? tmpCurrentAngle : maxCatchAngle;
            }
            if (tmpCurrentAngle > 0) {
                maxFinishAngle = tmpCurrentAngle > maxFinishAngle ? tmpCurrentAngle : maxFinishAngle;
            }
            currentAngle = tmpCurrentAngle;
        }

        public ActionBlock<JointData> Input { get => input; set => input = value; }
        public TrunkAngleView View { get => view; set => view = value; }
        internal TrunkAngleViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
