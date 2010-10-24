namespace CSharpSilverlightUnitTesting
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Silverlight.Testing.UnitTesting.Metadata;
    using TickSpec;

    public class TestClass : ITestClass
    {       
        readonly IAssembly _assembly;
        readonly Feature _feature;

        public TestClass(IAssembly assembly, Feature feature)
        {
            _assembly = assembly;
            _feature = feature;
        }

        public IAssembly Assembly
        {
            get { return _assembly; }
        }

        public MethodInfo ClassCleanupMethod
        {
            get { return null; }
        }

        public MethodInfo ClassInitializeMethod
        {
            get { return null; }
        }

        public ICollection<ITestMethod> GetTestMethods()
        {
            var methods = new List<ITestMethod>();
            foreach (var scenario in _feature.Scenarios)            
                methods.Add(new TestMethod(_feature, scenario));            
            return methods;
        }      

        public bool Ignore
        {
            get { return false; }
        }

        public string Name
        {
            get { return _feature.Name; }
        }

        public MethodInfo TestCleanupMethod
        {
            get { return null; }
        }

        public MethodInfo TestInitializeMethod
        {
            get { return null; }
        }

        public Type Type
        {
            get { return typeof(object); }         
        }
    }
}
