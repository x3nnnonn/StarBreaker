namespace StarBreaker.Forge;

public abstract class XmlAttribute
{
    protected readonly Database _database;
    protected DataForgeStringId _name;
    
    public abstract void WriteTo(TextWriter writer);
    
    protected XmlAttribute(DataForgeStringId name, Database database)
    {
        _name = name;
        _database = database;
    }
}

public sealed class XmlAttribute<T> : XmlAttribute
{
    public readonly T Value;

    public XmlAttribute(DataForgeStringId name, T value, Database database) : base(name, database)
    {
        Value = value;
    }

    public override void WriteTo(TextWriter writer)
    {
        writer.Write(_database.GetString(_name));
        writer.Write('=');

        writer.Write('"');
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
            case DataForgeStringId sid:
                 writer.Write(_database.GetString(sid));
                break;
            case DataForgeReference ra:
                ra.Value.WriteInto(writer);
                writer.Write('.');
                writer.Write(ra.Item1);
                break;
            case DataForgePointer p:
                //todo: remove me?
                writer.Write(p.StructIndex);
                writer.Write('.');
                writer.Write(p.InstanceIndex);
                break;
            default:
                throw new NotImplementedException();
        }

        writer.Write('"');
    }
}