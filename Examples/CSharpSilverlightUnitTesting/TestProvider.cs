namespace CSharpSilverlightUnitTesting
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Silverlight.Testing.UnitTesting.Metadata;
    using UnitTestHarness = Microsoft.Silverlight.Testing.Harness.UnitTestHarness;
    using VS = Microsoft.VisualStudio.TestTools.UnitTesting;
    using TickSpec;

    public class TestProvider : IUnitTestProvider
    {
        private const UnitTestProviderCapabilities MyCapabilities =            
            UnitTestProviderCapabilities.MethodCanDescribe;

        public UnitTestProviderCapabilities Capabilities
        {
            get { return MyCapabilities; }
        }

        readonly IDictionary<Assembly, Feature> _featureLookup =
            new Dictionary<Assembly, Feature>();

        public void RegisterFeature(Feature feature)
        {
            _featureLookup.Add(feature.Assembly, feature);
        }

        readonly IDictionary<Assembly, IAssembly> _assemblyLookup = 
            new Dictionary<Assembly, IAssembly>();       

        public IAssembly GetUnitTestAssembly(
            UnitTestHarness testHarness, 
            Assembly assemblyReference)
        {
            if (_assemblyLookup.ContainsKey(assemblyReference))
                return _assemblyLookup[assemblyReference];           
            var feature = _featureLookup[assemblyReference];
            var ass = new TestAssembly(this,testHarness,feature);
            _assemblyLookup[assemblyReference] = ass;
            return ass;
        }

        public bool HasCapability(UnitTestProviderCapabilities capability)
        {
            return ((capability & MyCapabilities) == capability);
        }

        public bool IsFailedAssert(Exception exception)
        {
            Type et = exception.GetType();
            Type vsttAsserts = typeof(VS.AssertFailedException);
            return (et == vsttAsserts || et.IsSubclassOf(vsttAsserts));
        }

        public string Name
        {
            get { return "TickSpec"; }
        }
    }
}
