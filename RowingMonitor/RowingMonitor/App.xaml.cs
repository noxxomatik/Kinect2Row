using MahApps.Metro;
using System;
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

            // init Logger
            log4net.Config.XmlConfigurator.Configure();
        }

        protected override void OnStartup(StartupEventArgs e)
        {    
            // change the theme and accent
            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent("Blue"),
                                        ThemeManager.GetAppTheme("BaseDark"));

            base.OnStartup(e);
        }
    }
}
