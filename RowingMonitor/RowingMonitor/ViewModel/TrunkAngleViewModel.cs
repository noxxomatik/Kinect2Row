using RowingMonitor.Model.Pipeline;
using RowingMonitor.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RowingMonitor.ViewModel
{
    class TrunkAngleViewModel : INotifyPropertyChanged
    {
        private ImageSource imageSource;

        public TrunkAngleViewModel()
        {
        }

        public void Render(ImageSource imageSource)
        {
            ImageSource = imageSource;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageSource"));
        }

        public ImageSource ImageSource { get => imageSource; set => imageSource = value; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
