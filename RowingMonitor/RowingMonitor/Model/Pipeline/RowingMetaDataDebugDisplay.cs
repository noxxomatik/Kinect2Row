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
    public class RowingMetadataDebugDisplay
    {
        ActionBlock<RowingMetadata> input;
        RowingMetadata tmpMetaData;

        private RowingMetadataDebugView view;
        private RowingMetadataDebugViewModel viewModel;

        public RowingMetadataDebugDisplay()
        {
            View = new RowingMetadataDebugView();
            ViewModel = (RowingMetadataDebugViewModel)View.DataContext;

            Input = new ActionBlock<RowingMetadata>(metaData =>
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

        public ActionBlock<RowingMetadata> Input { get => input; set => input = value; }
        public RowingMetadataDebugView View { get => view; set => view = value; }
        internal RowingMetadataDebugViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
