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
    /// <summary>
    /// RowingMetadataWidgetsDisplay visualizes the Rowing style factor and Catch factor in segmented bars.
    /// </summary>
    public class RowingMetadataWidgetsDisplay
    {
        ActionBlock<RowingMetadata> input;
        RowingMetadata tmpMetaData;

        private RowingMetadataWidgetsView view;
        private RowingMetadataWidgetsViewModel viewModel;

        public RowingMetadataWidgetsDisplay()
        {
            View = new RowingMetadataWidgetsView();
            ViewModel = (RowingMetadataWidgetsViewModel)View.DataContext;

            Input = new ActionBlock<RowingMetadata>(metaData =>
            {
                tmpMetaData = metaData;
            });
        }

        public void Render()
        {
            if (tmpMetaData != null) {
                View.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ViewModel.Render(tmpMetaData.CatchFactor, tmpMetaData.RowingStyleFactor);
                }));
            }
        }

        public ActionBlock<RowingMetadata> Input { get => input; set => input = value; }
        public RowingMetadataWidgetsView View { get => view; set => view = value; }
        internal RowingMetadataWidgetsViewModel ViewModel { get => viewModel; set => viewModel = value; }
    }
}
