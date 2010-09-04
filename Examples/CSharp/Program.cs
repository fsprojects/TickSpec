namespace CSharp
{
   using System.Reflection;
   using TickSpec;

   class Program
   {
      static void Main(string[] args)
      {
         var ass = Assembly.GetExecutingAssembly();
         var definitions = new StepDefinitions(ass);         
         var s = ass.GetManifestResourceStream(@"CSharp.Feature.txt");
         definitions.Execute(@"..\..\Feature.txt",s);
      }
   }
}
