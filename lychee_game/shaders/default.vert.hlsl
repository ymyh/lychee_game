cbuffer Camera : register(b0)
{
    float4x4 MVP;
    float4 Tint;
};

struct VSInput
{
    float2 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

struct VSOutput
{
    float4 Position  : SV_Position;
    float2 TexCoord  : TEXCOORD0;
    float4 Color     : COLOR0;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = mul(float4(input.Position, 0.0, 1.0), MVP);
    output.TexCoord = input.TexCoord;
    output.Color = input.Color * Tint;
    return output;
}
