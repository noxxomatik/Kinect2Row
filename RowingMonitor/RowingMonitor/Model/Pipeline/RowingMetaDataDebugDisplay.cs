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
    public class RowingMetaDataDebugDisplay
    {
        ActionBlock<RowingMetaData> input;
        RowingMetaData tmpMetaData;

        private RowingMetaDataDebugView view;
        private RowingMetaDataDebugViewModel viewModel;

        public RowingMetaDataDebugDisplay()
        {
            View = new RowingMetaDataDebugView();
            ViewModel = (RowingMetaDataDebugViewModel)View.DataContext;

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
        public RowingMetaDataDebugView View { get => view; set => view = value; }
        internal RowingMetaDataDebugViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
