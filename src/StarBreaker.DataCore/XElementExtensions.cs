using System.Xml.Linq;

namespace StarBreaker.DataCore;

public static class XObjectExtensions
{
    public static XElement WithAttribute(this XElement xObject, string name, string value, bool write = true)
    {
        if (write)
            xObject.Add(new XAttribute(name, value));

        return xObject;
    }
}