cbuffer Camera : register(b0)
{
    float4x4 ViewProjection;
};

struct VSInput
{
    float2 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;

    float4 World0   : TEXCOORD1;
    float4 World1   : TEXCOORD2;
    float4 World2   : TEXCOORD3;
    float4 World3   : TEXCOORD4;
    float4 UvRect   : TEXCOORD5;
    float4 Tint     : COLOR1;
};

struct VSOutput
{
    float4 Position  : SV_Position;
    float2 TexCoord  : TEXCOORD0;
    float4 Color     : COLOR0;
};

VSOutput main(VSInput input)
{
    float4x4 world = float4x4(input.World0, input.World1, input.World2, input.World3);

    VSOutput output;
    output.Position = mul(float4(input.Position, 0.0, 1.0), mul(world, ViewProjection));
    output.TexCoord = lerp(input.UvRect.xy, input.UvRect.zw, input.TexCoord);
    output.Color = input.Color * input.Tint;
    return output;
}
