    //High-level shader language

// Global matrices provided by your Camera system
float4x4 View;
float4x4 Projection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color    : COLOR0;
};

struct InstanceInput
{
    float3 InstancePosition : BLENDWEIGHT0; // We use weights as generic slots
    float  InstanceRotation : BLENDWEIGHT1;
    float  InstanceScale    : BLENDWEIGHT2;
    float4 InstanceColor    : COLOR1;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
};

VertexShaderOutput MainVS(VertexShaderInput input, InstanceInput instance)
{
    VertexShaderOutput output;

    // 1. Local Scale
    float3 pos = input.Position.xyz * instance.InstanceScale;

    // 2. Y-Axis Rotation
    float s, c;
    sincos(instance.InstanceRotation, s, c);
    
    float3 rotatedPos;
    rotatedPos.x = pos.x * c - pos.z * s;
    rotatedPos.y = pos.y; // Keep Y height as is
    rotatedPos.z = pos.x * s + pos.z * c;

    // 3. World Translation
    // We add 0.5 * scale to Y so the "Position" is the bottom of the car
    float3 worldPos = rotatedPos + instance.InstancePosition;
    worldPos.y += (0.5f * instance.InstanceScale);

    // 4. View & Projection
    float4 viewPos = mul(float4(worldPos, 1.0), View);
    output.Position = mul(viewPos, Projection);

    // Combine vertex color (shading) with instance color
    output.Color = input.Color * instance.InstanceColor;

    return output;
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    return input.Color;
}

technique Instancing
{
    pass P0
    {
        VertexShader = compile vs_4_0 MainVS();
        PixelShader = compile ps_4_0 MainPS();
    }
};