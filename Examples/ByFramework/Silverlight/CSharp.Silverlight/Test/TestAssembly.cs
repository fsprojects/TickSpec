namespace CSharpSilverlightUnitTesting
{
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Silverlight.Testing.Harness;
    using Microsoft.Silverlight.Testing.UnitTesting.Metadata;
    using TickSpec;

    public class TestAssembly : IAssembly
    {
        readonly IUnitTestProvider _provider;
        readonly UnitTestHarness _testHarness;
        readonly Feature _feature;
        readonly ICollection<ITestClass> _classes;

        public TestAssembly(
            IUnitTestProvider provider, 
            UnitTestHarness testHarness, 
            Feature feature)
        {
            _provider = provider;
            _testHarness = testHarness;
            _feature = feature;
            _classes = new List<ITestClass>();
            _classes.Add(new TestClass(this, _feature));            
        }

        public System.Reflection.MethodInfo AssemblyCleanupMethod
        {
            get { return null; }
        }

        public MethodInfo AssemblyInitializeMethod
        {
            get { return null; }
        }

        public ICollection<ITestClass> GetTestClasses()
        {
            return _classes;
        }

        public string Name
        {
            get { return _feature.Source; }
        }

        public IUnitTestProvider Provider
        {
            get { return _provider; }
        }

        public UnitTestHarness TestHarness
        {
            get { return _testHarness; }
        }
    }
}
