namespace CSharpSilverlightUnitTesting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Silverlight.Testing;
    using Microsoft.Silverlight.Testing.UnitTesting.Metadata;
    using Microsoft.Silverlight.Testing.UnitTesting.Metadata.VisualStudio;
    using TickSpec;

    public class TestMethod : ITestMethod 
    {
        readonly Feature _feature;
        readonly Scenario _scenario;
        readonly bool _ignore;

        public TestMethod(Feature feature, Scenario scenario)
        {
            _feature = feature;
            _scenario = scenario;
            _ignore = scenario.Tags.Contains("ignore");
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
            Func<string, string> Escape = s =>
                s.Replace("(", "_")
                 .Replace(")", "_")
                 .Replace("-", "_");

            yield return new TagAttribute(Escape(_feature.Source));
            yield return new TagAttribute(Escape(_feature.Name));
            yield return new TagAttribute(Escape(_scenario.Name));
            foreach (var tag in _scenario.Tags)
                yield return new TagAttribute(Escape(tag));            
        }

        public bool Ignore
        {
            get { return _ignore; }
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
