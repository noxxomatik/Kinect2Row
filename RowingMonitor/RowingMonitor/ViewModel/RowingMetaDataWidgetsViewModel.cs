using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RowingMonitor.ViewModel
{
    class RowingMetaDataWidgetsViewModel : INotifyPropertyChanged
    {
        private Thickness catchFactorMargin;
        private Thickness rowingStyleFactorMargin;

        private String catchFactor;
        private String rowingStyleFactor;

        private Brush catchFactorColor;
        private Brush rowingStyleFactorColor;

        // pixel values for the bars
        private const double catchFactorBarUnit = 4;
        private const double catchFactorStart = -60;
        private const double catchFactorEnd = 15;
        private const double catchFactorGoodStart = -30;
        private const double catchFactorGoodEnd = -15;

        private const double rowingStyleBarUnit = 5;
        private const double rowingStyleStart = 50;
        private const double rowingStyleEnd = 110;
        private const double rowingStyleGoodStart = 85;
        private const double rowingStyleGoodEnd = 95;

        private const double tickOffset = 2;

        // rowing style factor only changes if a new segment ended
        private double lastRowingStyleFactor;

        // segment colors
        private Brush LowColor =  new SolidColorBrush(Colors.DodgerBlue);
        private Brush GoodColor = new SolidColorBrush(Colors.YellowGreen);
        private Brush HighColor = new SolidColorBrush(Colors.PaleVioletRed);

        public event PropertyChangedEventHandler PropertyChanged;

        public void Render(double catchFactor, double rowingStyleFactor)
        {
            rowingStyleFactor = rowingStyleFactor != 0 ? rowingStyleFactor : lastRowingStyleFactor;

            // text
            CatchFactor = catchFactor.ToString("0.0") + "ms";
            RowingStyleFactor = (rowingStyleFactor * 100).ToString("0.0") + "%";

            // bars
            int catchFactorOffset;
            if (catchFactor >= catchFactorEnd) {
                catchFactorOffset = (int)(catchFactorEnd * catchFactorBarUnit - tickOffset);
            }
            else if (catchFactor <= catchFactorEnd) {
                catchFactorOffset = (int)(0 - tickOffset);
            }
            else {
                catchFactorOffset = (int)((catchFactor - catchFactorStart)
                    * catchFactorBarUnit - tickOffset);
            }
            CatchFactorMargin = new Thickness(catchFactorOffset, 0, 0, 0);

            int rowingStyleOffset;
            if (rowingStyleFactor >= rowingStyleEnd) {
                rowingStyleOffset = (int)(rowingStyleEnd * rowingStyleBarUnit - tickOffset);
            }
            else if (rowingStyleFactor <= rowingStyleEnd) {
                rowingStyleOffset = (int)(0 - tickOffset);
            }
            else {
                rowingStyleOffset = (int)((rowingStyleFactor - rowingStyleStart)
                    * rowingStyleBarUnit - tickOffset);
            }
            RowingStyleFactorMargin = new Thickness(rowingStyleOffset, 0, 0, 0);

            // text colors
            if (catchFactor < catchFactorGoodStart) {
                CatchFactorColor = LowColor;
            }
            else if(catchFactor > catchFactorGoodEnd) {
                CatchFactorColor = HighColor;
            }
            else {
                CatchFactorColor = GoodColor;
            }

            if (rowingStyleFactor < rowingStyleGoodStart) {
                RowingStyleFactorColor = LowColor;
            }
            else if(rowingStyleFactor > rowingStyleGoodEnd) {
                RowingStyleFactorColor = HighColor;
            }
            else {
                RowingStyleFactorColor = GoodColor;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "None")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Thickness CatchFactorMargin
        {
            get => catchFactorMargin;
            set {
                catchFactorMargin = value;
                OnPropertyChanged();
            }
        }
        public Thickness RowingStyleFactorMargin
        {
            get => rowingStyleFactorMargin;
            set {
                rowingStyleFactorMargin = value;
                OnPropertyChanged();
            }
        }
        public string CatchFactor
        {
            get => catchFactor;
            set {
                catchFactor = value;
                OnPropertyChanged();
            }
        }
        public string RowingStyleFactor
        {
            get => rowingStyleFactor;
            set {
                rowingStyleFactor = value;
                OnPropertyChanged();
            }
        }
        public Brush CatchFactorColor
        {
            get => catchFactorColor;
            set {
                catchFactorColor = value;
                OnPropertyChanged();
            }
        }
        public Brush RowingStyleFactorColor
        {
            get => rowingStyleFactorColor;
            set {
                rowingStyleFactorColor = value;
                OnPropertyChanged();
            }
        }
    }
}
