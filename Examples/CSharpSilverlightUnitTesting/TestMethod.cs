namespace CSharpSilverlightUnitTesting
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Silverlight.Testing.UnitTesting.Metadata;
    using Microsoft.Silverlight.Testing.UnitTesting.Metadata.VisualStudio;
    using TickSpec;

    public class TestMethod : ITestMethod 
    {
        readonly Feature _feature;
        readonly Scenario _scenario;

        public TestMethod(Feature feature, Scenario scenario)
        {
            _feature = feature;
            _scenario = scenario;
        }

        public string Category
        {
            get { return null; }
        }

        public void DecorateInstance(object instance)
        {           
        }

        public string Description
        {
            get { return _scenario.Description; }
        }

        public IExpectedException ExpectedException
        {
            get { return null; }
        }

        public IEnumerable<Attribute> GetDynamicAttributes()
        {
            return new Attribute[] { };
        }

        public bool Ignore
        {
            get { return false; }
        }

        public void Invoke(object instance)
        {
            _scenario.Action();
        }

        public MethodInfo Method
        {
            get { return _scenario.Action.Method; }
        }

        public string Name
        {
            get { return _scenario.ToString(); }
        }

        public string Owner
        {
            get { return _feature.Name; }
        }

        public IPriority Priority
        {
            get { return new Priority(3); }
        }

        public ICollection<ITestProperty> Properties
        {
            get { return new List<ITestProperty>(); }
        }

        public int? Timeout
        {
            get { return null; }
        }

        public ICollection<IWorkItemMetadata> WorkItems
        {
            get { return null; }
        }

        public event EventHandler<StringEventArgs> WriteLine;
    }
}
