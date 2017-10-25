using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    public static class PlotDataHandler { }

    public struct PlotData
    {
        public PlotData(double x, double y, DataStreamType dataStreamType) : this()
        {
            X = x;
            Y = y;
            DataStreamType = dataStreamType;
        }

        public PlotData(double x, string annotation, DataStreamType dataStreamType) : this()
        {
            X = x;
            Annotation = annotation;
            DataStreamType = dataStreamType;
        }

        public PlotData(double x, string annotation, DataStreamType dataStreamType, long index) : this()
        {
            X = x;
            Annotation = annotation;
            DataStreamType = dataStreamType;
            Index = index;
        }

        public PlotData(double x, double y, DataStreamType dataStreamType, long index) : this()
        {
            X = x;
            Y = y;
            DataStreamType = dataStreamType;
            Index = index;
        }

        public PlotData(double x, double y, string annotation, DataStreamType dataStreamType, long index)
        {
            X = x;
            Y = y;
            Annotation = annotation;
            DataStreamType = dataStreamType;
            Index = index;
        }

        public readonly double X;
        public readonly double Y;
        public readonly string Annotation;
        public readonly DataStreamType DataStreamType;
        public readonly long Index;
    }
}
