namespace CSharpSilverlightUnitTesting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Microsoft.Silverlight.Testing;
    using Microsoft.Silverlight.Testing.Harness;
    using TickSpec;

    public partial class App : Application
    {
        public App()
        {
            this.Startup += this.Application_Startup;
            this.Exit += this.Application_Exit;
            this.UnhandledException += this.Application_UnhandledException;

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var harness = new UnitTestHarness();            
            var settings = new UnitTestSettings();
            settings.TestHarness = harness;
            harness.Settings = settings;
            harness.Initialize();          
            var provider = new TestProvider();            
            var filter = new TagTestRunFilter(settings, harness);

            harness.TestRunStarting += (senderx, ex) =>
                {
                    var features = FeatureFactory.GetFeatures(typeof(App).Assembly);
                    foreach (var feature in features)
                    {
                        provider.RegisterFeature(feature);
                        var ass = provider.GetUnitTestAssembly(harness, feature.Assembly);
                        harness.EnqueueTestAssembly(ass, filter);
                    }
                };
 
            this.RootVisual = UnitTestSystem.CreateTestPage(settings);            
        }

        

        private void Application_Exit(object sender, EventArgs e)
        {

        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
        }

        private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

                System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
            }
            catch (Exception)
            {
            }
        }
    }
}
