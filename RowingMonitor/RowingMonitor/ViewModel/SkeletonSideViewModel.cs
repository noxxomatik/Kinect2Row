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
    class SkeletonSideViewModel : INotifyPropertyChanged
    {
        private ImageSource skeletonImageSource;

        public SkeletonSideViewModel()
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

        public event PropertyChangedEventHandler PropertyChanged;

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
