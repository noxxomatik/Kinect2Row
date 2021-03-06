﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace RowingMonitor.Model.Util
{
    /// <summary> 
    /// Another implementation of the 1€ Filter to test the implementation of OneEuroSmoothingFilter.
    /// Source: http://cristal.univ-lille.fr/~casiez/1euro/OneEuroFilter.cs
    /// </summary>
    public class OneEuroFilter
    {
        public OneEuroFilter(double minCutoff, double beta)
        {
            firstTime = true;
            this.minCutoff = minCutoff;
            this.beta = beta;

            xFilt = new LowpassFilter();
            dxFilt = new LowpassFilter();
            dcutoff = 1;
        }

        protected bool firstTime;
        protected double minCutoff;
        protected double beta;
        protected LowpassFilter xFilt;
        protected LowpassFilter dxFilt;
        protected double dcutoff;

        public double MinCutoff
        {
            get { return minCutoff; }
            set { minCutoff = value; }
        }

        public double Beta
        {
            get { return beta; }
            set { beta = value; }
        }

        public double Filter(double x, double rate)
        {
            double hatXPrev = xFilt.Last();
            double dx = firstTime ? 0 : (x - hatXPrev) * rate;
            if (firstTime)
            {
                firstTime = false;
            }

            var edx = dxFilt.Filter(dx, Alpha(rate, dcutoff));
            var cutoff = minCutoff + beta * Math.Abs(edx);

            return xFilt.Filter(x, Alpha(rate, cutoff));
        }

        protected double Alpha(double rate, double cutoff)
        {
            var tau = 1.0 / (2 * Math.PI * cutoff);
            var te = 1.0 / rate;
            return 1.0 / (1.0 + tau / te);
        }
    }

    public class LowpassFilter
    {
        public LowpassFilter()
        {
            firstTime = true;
        }

        protected bool firstTime;
        protected double hatXPrev;

        public double Last()
        {
            return hatXPrev;
        }

        public double Filter(double x, double alpha)
        {
            double hatX = 0;
            if (firstTime)
            {
                firstTime = false;
                hatX = x;
            }
            else
                hatX = alpha * x + (1 - alpha) * hatXPrev;

            hatXPrev = hatX;

            return hatX;
        }
    }
}
