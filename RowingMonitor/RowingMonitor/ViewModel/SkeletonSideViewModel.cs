using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;
using RowingMonitor.Model.Pipeline;

namespace RowingMonitor.ViewModel
{
    class SkeletonSideViewModel : INotifyPropertyChanged
    {
        private ImageSource skeletonImageSource;

        private SkeletonSideDisplay model;

        public SkeletonSideViewModel()
        {

        }

        public void Render()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SkeletonImageSource"));
        }

        public void UpdateSkeletonImage(DrawingImage skeletonImageSource)
        {
            skeletonImageSource.Freeze();
            
            if (skeletonImageSource != null) {
                SkeletonImageSource = skeletonImageSource;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource SkeletonImageSource
        {
            get => skeletonImageSource;
            set {
                skeletonImageSource = value;
            }
        }

        public SkeletonSideDisplay Model { get => model; set => model = value; }
    }
}
