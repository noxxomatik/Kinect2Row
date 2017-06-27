using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;

namespace RowingMonitor.ViewModel
{
    class SkeletonFrontalViewModel : INotifyPropertyChanged
    {
        private ImageSource colorImageSource;
        private ImageSource skeletonImageSource;

        public SkeletonFrontalViewModel()
        {

        }

        public void Render()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ColorImageSource"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SkeletonImageSource"));
        }

        public void UpdateSkeletonImage(DrawingImage skeletonImageSource)
        {
            skeletonImageSource.Freeze();

            // DependencyObjects can only be assigned in the UI Thread
            if (skeletonImageSource != null) {
                SkeletonImageSource = skeletonImageSource;
            }
        }

        public void UpdateColorImage(ImageSource colorImageSource)
        {
            colorImageSource.Freeze();
            if (colorImageSource != null) {
                ColorImageSource = colorImageSource;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource ColorImageSource
        {
            get => colorImageSource;
            set {
                colorImageSource = value;
            }
        }
        public ImageSource SkeletonImageSource
        {
            get => skeletonImageSource;
            set {
                skeletonImageSource = value;
            }
        }
    }
}
