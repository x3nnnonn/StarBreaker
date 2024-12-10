using System.Diagnostics;
using StarBreaker.Common;

namespace StarBreaker.DataCore;

public abstract class XmlAttribute
{
    protected readonly string _name;
    
    public abstract void WriteTo(TextWriter writer);

    protected XmlAttribute(string name)
    {
        _name = name;
    }
}

[DebuggerDisplay("{_name}={Value}")]
public sealed class XmlAttribute<T> : XmlAttribute
{
    public readonly T Value;
    
    public XmlAttribute(string name, T value) : base(name)
    {
        Value = value;
    }

    public override void WriteTo(TextWriter writer)
    {
        writer.Write(_name);
        writer.Write('=');

        writer.Write('"');
        WriteValue(writer);
        writer.Write('"');
    }

    private void WriteValue(TextWriter writer)
    {
        //we do this instead of passing the value directly to the writer to avoid boxing
        switch (Value)
        {
            case sbyte sb:
                writer.Write(sb);
                break;
            case byte b:
                writer.Write(b);
                break;
            case short s:
                writer.Write(s);
                break;
            case ushort us:
                writer.Write(us);
                break;
            case int i:
                writer.Write(i);
                break;
            case uint ui:
                writer.Write(ui);
                break;
            case long l:
                writer.Write(l);
                break;
            case ulong ul:
                writer.Write(ul);
                break;
            case float f:
                writer.Write(f);
                break;
            case double d:
                writer.Write(d);
                break;
            case bool bl:
                writer.Write(bl);
                break;
            case CigGuid g:
                g.WriteInto(writer);
                break;
            case DataCoreReference ra:
                ra.RecordId.WriteInto(writer);
                writer.Write('.');
                writer.Write(ra.InstanceIndex);
                break;
            case DataCorePointer p:
                //todo: remove me?
                writer.Write(p.StructIndex);
                writer.Write('.');
                writer.Write(p.InstanceIndex);
                break;
            case string ss:
                writer.Write(ss);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}