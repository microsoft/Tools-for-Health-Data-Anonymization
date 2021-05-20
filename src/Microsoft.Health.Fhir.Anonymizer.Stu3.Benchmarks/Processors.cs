using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Benchmarks
{
    /// <summary>
    /// This class is an example on how to create benchmarks for specific methods
    /// </summary> 
    [ShortRunJob(RuntimeMoniker.NetCoreApp31)]
    [ShortRunJob(RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    [CsvMeasurementsExporter]
    [RPlotExporter]
    public class Processors
    {
        [Benchmark]
        [ArgumentsSource(nameof(GetNonReferenceNodes))]
        public void CryptoHashNonReferenceNodes(ElementWrapper wrapper)
        {
            var chp = new CryptoHashProcessor("benchmark");
            chp.Process(wrapper.Node);
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetReferenceNodes))]
        public void CryptoHashReferenceNodes(ElementWrapper wrapper)
        {
            var chp = new CryptoHashProcessor("benchmark");
            chp.Process(wrapper.Node);
        }

        public static IEnumerable<ElementWrapper> GetNonReferenceNodes()
        {
            foreach (var e in GetNonReferenceElements())
            {
                yield return new ElementWrapper(ElementNode.FromElement(e.ToTypedElement()));
            }
        }

        public static IEnumerable<ElementWrapper> GetReferenceNodes()
        {
            foreach (var e in GetReferenceElements())
            {
                yield return new ElementWrapper(ElementNode.FromElement(e.ToTypedElement()));
            }
        }

        public static IEnumerable<Element> GetNonReferenceElements()
        {
            yield return new Id(string.Empty);
            yield return new Id("a");
            yield return new Id("bb6f4872-e456-42d5-a9da-a0d82cb7ea29");
            yield return new Oid("urn:oid:1.2.3.4.5");
            yield return new Uuid("urn:uuid:c757873d-ec9a-4326-a141-556f43239520");
            yield return new Date("2020-04-12");
            yield return new FhirDateTime("2017-01-01T00:00:00.000Z");
        }

        public static IEnumerable<Element> GetReferenceElements()
        {
            yield return new ResourceReference(string.Empty);
            yield return new ResourceReference("#");
            yield return new ResourceReference("#p1");
            yield return new ResourceReference("Patient/example");
            yield return new ResourceReference("http://fhir.hl7.org/svc/StructureDefinition/c8973a22-2b5b-4e76-9c66-00639c99e61b");
            yield return new ResourceReference("http://example.org/fhir/Observation/apo89654/_history/2");
            yield return new ResourceReference("urn:uuid:c757873d-ec9a-4326-a141-556f43239520");
            yield return new ResourceReference("urn:oid:1.2.3.4.5");
        }
    }
    public class ElementWrapper
    {
        public ElementWrapper(ElementNode node)
        {
            Node = node;
        }

        public ElementNode Node { get; }

        public override string ToString() => this.Node.Name.ToString();
    }
}
