using MessagePack;
using MessagePack.Formatters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS;

public class VertexPositionColorFormatter : IMessagePackFormatter<VertexPositionColor>
{
    public void Serialize(ref MessagePackWriter writer, VertexPositionColor value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(7);
        writer.Write(value.Color.R);
        writer.Write(value.Color.G);
        writer.Write(value.Color.B);
        writer.Write(value.Color.A);
        writer.Write(value.Position.X);
        writer.Write(value.Position.Y);
        writer.Write(value.Position.Z);
    }

    public VertexPositionColor Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadArrayHeader();
        float R = reader.ReadSingle();
        float G = reader.ReadSingle();
        float B = reader.ReadSingle();
        float A = reader.ReadSingle();
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();

        Color color = new Color(R, G, B, A);
        Vector3 position = new Vector3(x, y, z);

        return new VertexPositionColor(position, color);
    }
}