struct Camera
{
    // row_major: Slang reads the raw row-major words uploaded by System.Numerics
    // so that mul(v, mul(world, viewProjection)) applies world first, then the camera.
    row_major float4x4 viewProjection;
};

// SDL3 SPIR-V convention: vertex uniforms in set 1, fragment samplers in set 2.
// The D3D register/space mirrors it for the DXIL target (b0/space1, t0/space2).
[[vk::binding(0, 1)]]
ConstantBuffer<Camera> camera : register(b0, space1);

[[vk::binding(0, 2)]]
Sampler2D texSampler : register(t0, space2);

struct VertexInput
{
    [[vk::location(0)]] float2 position : TEXCOORD0;
    [[vk::location(1)]] float2 texCoord : TEXCOORD1;
    [[vk::location(2)]] float4 color : TEXCOORD2;
    [[vk::location(3)]] float4 world0 : TEXCOORD3;
    [[vk::location(4)]] float4 world1 : TEXCOORD4;
    [[vk::location(5)]] float4 world2 : TEXCOORD5;
    [[vk::location(6)]] float4 world3 : TEXCOORD6;
    [[vk::location(7)]] float4 uvRect : TEXCOORD7;
    [[vk::location(8)]] float4 tint : TEXCOORD8;
};

struct VertexOutput
{
    float4 position : SV_Position;
    [[vk::location(0)]] float2 texCoord : TEXCOORD0;
    [[vk::location(1)]] float4 color : TEXCOORD1;
};

[shader("vertex")]
VertexOutput vertexMain(VertexInput input)
{
    float4x4 world = float4x4(input.world0, input.world1, input.world2, input.world3);

    VertexOutput output;
    output.position = mul(float4(input.position, 0.0, 1.0), mul(world, camera.viewProjection));
    output.texCoord = lerp(input.uvRect.xy, input.uvRect.zw, input.texCoord);
    output.color = input.color * input.tint;
    return output;
}

[shader("fragment")]
float4 fragmentMain(VertexOutput input) : SV_Target
{
    return texSampler.Sample(input.texCoord) * input.color;
}
