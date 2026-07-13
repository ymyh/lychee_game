Texture2D<float4> tex : register(t0);
SamplerState texSampler : register(s0);

struct PSInput
{
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

float4 main(PSInput input) : SV_Target0
{
    return tex.Sample(texSampler, input.TexCoord) * input.Color;
}
