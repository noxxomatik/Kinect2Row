using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace RowingMonitor.Model.Util
{
    public class CurveFitting
    {
        private CurveFitting() { }

        public static QuadraticFunctionParameters QuadraticFunctionFit(double[] x, double[] y)
        {
            // fit the curve
            double[] param = Fit.Polynomial(x, y, 2);
            QuadraticFunctionParameters polyParam = new QuadraticFunctionParameters
            {
                A = param[2],
                B = param[1],
                C = param[0]
            };

            // find maximum
            // first derivation is 0
            polyParam.XMax = (-polyParam.B) / (2 * polyParam.A);
            // calculate y
            polyParam.YMax = QuadraticFunction(polyParam.XMax, polyParam.A, polyParam.B, polyParam.C);

            return polyParam;
        }

        public static double QuadraticFunction(double x, double a, double b, double c)
        {
            return a * Math.Pow(x, 2) + b * x + c;
        }

        public struct QuadraticFunctionParameters
        {
            double a;
            double b;
            double c;
            double xMax;
            double yMax;

            public double A { get => a; set => a = value; }
            public double B { get => b; set => b = value; }
            public double C { get => c; set => c = value; }
            public double XMax { get => xMax; set => xMax = value; }
            public double YMax { get => yMax; set => yMax = value; }
        }
    }
}
