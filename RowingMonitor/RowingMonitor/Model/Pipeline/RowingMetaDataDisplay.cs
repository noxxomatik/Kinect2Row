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
    public class RowingMetaDataDisplay
    {
        ActionBlock<RowingMetaData> input;
        RowingMetaData tmpMetaData;

        private RowingMetaDataView view;
        private RowingMetaDataViewModel viewModel;

        public RowingMetaDataDisplay()
        {
            View = new RowingMetaDataView();
            ViewModel = (RowingMetaDataViewModel)View.DataContext;

            Input = new ActionBlock<RowingMetaData>(metaData =>
            {
                tmpMetaData = metaData;
            });
        }

        public void Render()
        {
            View.Dispatcher.BeginInvoke(new Action(() =>
            {
                ViewModel.Render(tmpMetaData);
            }));
        }

        public ActionBlock<RowingMetaData> Input { get => input; set => input = value; }
        public RowingMetaDataView View { get => view; set => view = value; }
        internal RowingMetaDataViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
