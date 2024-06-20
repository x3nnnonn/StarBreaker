using BenchmarkDotNet.Attributes;

namespace StarBreaker.Benchmarks;

public class SerializerBenchmarks
{
    [Benchmark]
    public void MyXmlBenchmark()
    {
    }
    
    [Benchmark]
    public void XmlDocumentBenchmark()
    {
    }
    
    [Benchmark]
    public void XDocumentBenchmark()
    {
    }
}