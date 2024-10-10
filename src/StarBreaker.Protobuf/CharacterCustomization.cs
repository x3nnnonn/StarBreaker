using ProtoBuf;

namespace StarBreaker.Protobuf;

public enum Gender
{
    Unspecified = 0,
    Male = 1,
    Female = 2,
}

public enum Race {
    Unspecified = 0,
    Human = 1,
}

[ProtoContract]
public class EntityClassLoadout
{
    [ProtoContract]
    public class EntityClassLoadoutParams
    {
        [ProtoMember(1)]
        public string PortName { get; set; }
        
        [ProtoMember(2)]
        public string EntityClassGuid { get; set; }
        
        [ProtoMember(3)]
        public EntityClassLoadoutParams[] Params { get; set; }
    }

    [ProtoMember(1)] 
    public List<EntityClassLoadoutParams> LoadoutParams { get; set; }
}

[ProtoContract]
public class CharacterCustomization
{
    [ProtoMember(1)]
    public Gender Gender { get; set; }
    
    [ProtoMember(2)]
    public Race Race { get; set; }
    
    [ProtoMember(3)]
    public string Generation { get; set; }
    
    [ProtoMember(4)]
    public byte[] DnaMatrix { get; set; }
    
    [ProtoMember(5)]
    public EntityClassLoadout Customizations { get; set; }
    
    [ProtoMember(6)]
    public byte[] CustomMaterialParams { get; set; }
}
