using System.Reflection;
using System.Windows.Controls;
using TickSpec;

namespace CSharpSilverlight
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            RunTests(outputBlock);
        }

        private static void RunTests(TextBlock outputBlock)
        {
            var ass = Assembly.GetExecutingAssembly();
            var definitions = new StepDefinitions(ass);
            var s = ass.GetManifestResourceStream(@"CSharpSilverlight.Feature.txt");
            var feature = definitions.GenerateFeature(@"..\..\Feature.txt", s);
            outputBlock.Text += feature.Name + "\r\n";
            foreach (var scenario in feature.Scenarios)
            {
                outputBlock.Text += scenario.Name + "\r\n";
                scenario.Action();
            }
        }
    }
}
