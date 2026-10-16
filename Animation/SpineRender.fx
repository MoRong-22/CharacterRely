// 输入为 tModLoader 已预乘透明度的纹理；Spine 导出图集时请关闭 Premultiply Alpha。
// 模型变换、视图变换和投影矩阵的乘积。
float4x4 uTransform;
// 骨架、插槽和附件相乘后的主颜色及透明度。
float4 uLight = float4(1, 1, 1, 1);
// Spine 双色着色的暗部颜色；未启用时为黑色。
float3 uDark = float3(0, 0, 0);
// 调用方传入的整体颜色与透明度。
float4 uTint = float4(1, 1, 1, 1);
texture uTexture;
sampler TextureSampler = sampler_state
{
    Texture = <uTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
    MinFilter = Linear;
    MagFilter = Linear;
};
struct VertexInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};
struct PixelInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};
// 顶点阶段：转换位置，保留附件在图集中的纹理坐标。
PixelInput SpineVS(VertexInput input)
{
    PixelInput output;
    output.Position = mul(float4(input.Position.xyz, 1), uTransform);
    output.TexCoord = input.TexCoord;
    return output;
}
// 像素阶段：叠加主色和暗部颜色，再输出预乘透明度的结果。
float4 SpinePS(PixelInput input) : COLOR0
{
    float4 sampleColor = tex2D(TextureSampler, input.TexCoord);
    float alpha = uLight.a * uTint.a;
    float3 rgb = sampleColor.rgb * uLight.rgb + (sampleColor.a - sampleColor.rgb) * uDark;
    return float4(rgb * uTint.rgb * alpha, sampleColor.a * alpha);
}
technique SpineRender
{
    pass Draw
    {
        VertexShader = compile vs_3_0 SpineVS();
        PixelShader = compile ps_3_0 SpinePS();
    }
}
