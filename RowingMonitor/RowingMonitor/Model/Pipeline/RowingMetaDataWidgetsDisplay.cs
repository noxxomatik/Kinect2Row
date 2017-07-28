using RowingMonitor.Model.Util;
using RowingMonitor.View;
using RowingMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    public class RowingMetaDataWidgetsDisplay
    {
        ActionBlock<RowingMetaData> input;
        RowingMetaData tmpMetaData;

        private RowingMetaDataWidgetsView view;
        private RowingMetaDataWidgetsViewModel viewModel;

        public RowingMetaDataWidgetsDisplay()
        {
            View = new RowingMetaDataWidgetsView();
            ViewModel = (RowingMetaDataWidgetsViewModel)View.DataContext;

            Input = new ActionBlock<RowingMetaData>(metaData =>
            {
                tmpMetaData = metaData;
            });
        }

        public void Render()
        {
            View.Dispatcher.BeginInvoke(new Action(() =>
            {
                ViewModel.Render(tmpMetaData.CatchFactor, tmpMetaData.RowingStyleFactor);
            }));
        }

        public ActionBlock<RowingMetaData> Input { get => input; set => input = value; }
        public RowingMetaDataWidgetsView View { get => view; set => view = value; }
        internal RowingMetaDataWidgetsViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
