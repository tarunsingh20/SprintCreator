using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SprintCreator.Common;

namespace SprintCreator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] commandLineArgs;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                commandLineArgs = e.Args;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ApplicationSingleInstance.Make();
            base.OnStartup(e);
        }

    }
}
