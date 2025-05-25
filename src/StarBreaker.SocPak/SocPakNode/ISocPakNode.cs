namespace StarBreaker.SocPak;

public interface ISocPakNode
{
    ISocPakNode? Parent { get; }
    string Name { get; }
} 