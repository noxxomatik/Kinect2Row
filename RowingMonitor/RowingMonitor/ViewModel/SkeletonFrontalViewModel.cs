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

        public void UpdateSkeletonImage(DrawingImage skeletonImageSource)
        {
            skeletonImageSource.Freeze();

            // DependencyObjects can only be assigned in the UI Thread
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                if (skeletonImageSource != null) {
                    SkeletonImageSource = skeletonImageSource;
                }
            }));
        }

        public void UpdateColorImage(ImageSource colorImageSource)
        {
            colorImageSource.Freeze();
            Application.Current?.Dispatcher?.Invoke(new Action(() =>
            {
                if (colorImageSource != null) {
                    ColorImageSource = colorImageSource;
                }
            }));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource ColorImageSource
        {
            get => colorImageSource;
            set {
                colorImageSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ColorImageSource"));
            }
        }
        public ImageSource SkeletonImageSource
        {
            get => skeletonImageSource;
            set {
                skeletonImageSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SkeletonImageSource"));
            }
        }
    }
}
