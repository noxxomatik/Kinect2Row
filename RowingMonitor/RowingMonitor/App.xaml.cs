﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RowingMonitor
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            string culture = "de-DE";
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            RowingMonitor.Properties.Resources.Culture = new CultureInfo(culture);
        }
    }
}
