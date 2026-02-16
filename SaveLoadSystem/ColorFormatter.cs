using MessagePack;
using MessagePack.Formatters;
using Microsoft.Xna.Framework;

namespace CS4620IS;

public class ColorFormatter : IMessagePackFormatter<Color>
{
    public void Serialize(ref MessagePackWriter writer, Color value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.R);
        writer.Write(value.G);
        writer.Write(value.B);
        writer.Write(value.A); }

    public Color Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadArrayHeader();
        float R = reader.ReadSingle();
        float G = reader.ReadSingle();
        float B = reader.ReadSingle();
        float A = reader.ReadSingle();
        return new Color(R, G, B, A);
    }
}