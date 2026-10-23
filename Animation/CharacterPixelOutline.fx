// 输入是已合成全部 Spine 部件、预乘透明度的角色 RT。
// 默认每格一个游戏像素；通过点采样保留清晰像素边缘，uPixelSize > 1 才合并像素格。
float2 uTextureSize;
float uPixelSize = 1.0;
float4 uOutlineColor = float4(0.109804, 0.078431, 0.094118, 1.0);
// C# 将游戏像素宽度换算为当前轮廓 RT 的像素数。
float uOutlineWidth = 2.0;
sampler CharacterTexture : register(s0);

// 每个游戏像素从两倍精度 RT 的四个子像素取样，减少细节漏采样；不混合相邻游戏像素。
float4 SampleBlock(float2 texCoord)
{
    float2 block = max(uPixelSize, 1.0) / uTextureSize;
    float2 start = floor(texCoord / block) * block;
    float4 color = tex2Dlod(CharacterTexture, float4(start + block * float2(0.25, 0.25), 0.0, 0.0));
    color += tex2Dlod(CharacterTexture, float4(start + block * float2(0.75, 0.25), 0.0, 0.0));
    color += tex2Dlod(CharacterTexture, float4(start + block * float2(0.25, 0.75), 0.0, 0.0));
    color += tex2Dlod(CharacterTexture, float4(start + block * float2(0.75, 0.75), 0.0, 0.0));
    return color * 0.25;
}

float4 PixelOutlinePS(float2 texCoord : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float4 body = SampleBlock(texCoord);
    float nearby = body.a;
    float gridWidth = uOutlineWidth / max(uPixelSize, 1.0);
    int radius = (int)ceil(gridWidth);
    [loop]
    for (int y = -radius; y <= radius; y++)
    {
        [loop]
        for (int x = -radius; x <= radius; x++)
        {
            float2 offset = float2(x, y);
            float coverage = saturate(gridWidth + 0.5 - length(offset));
            if (coverage > 0.0)
                nearby = max(nearby, SampleBlock(texCoord + offset * uPixelSize / uTextureSize).a * coverage);
        }
    }
    float outlineAlpha = max(nearby - body.a, 0.0) * uOutlineColor.a;
    return (body + float4(uOutlineColor.rgb * outlineAlpha, outlineAlpha)) * tint;
}

// 保留的细描边技术只输出轮廓；默认角色绘制使用下面的 CharacterPixelOutline。
// 轮廓先画在本体下方，透明边缘的遮挡由正常预乘混合完成。
float4 FineOutlinePS(float2 texCoord : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float bodyAlpha = tex2D(CharacterTexture, texCoord).a;
    float nearby = bodyAlpha;
    int radius = (int)ceil(uOutlineWidth);
    // 采样整个圆形邻域，保证加粗后的轮廓连续，不形成空心边或方形角。
    // 显式读取第 0 层纹理，兼容 ps_3_0 在动态循环内的纹理采样。
    [loop]
    for (int y = -radius; y <= radius; y++)
    {
        [loop]
        for (int x = -radius; x <= radius; x++)
        {
            float2 offset = float2(x, y);
            float coverage = saturate(uOutlineWidth + 0.5 - length(offset));
            if (coverage > 0.0)
            {
                float alpha = tex2Dlod(CharacterTexture, float4(texCoord + offset / uTextureSize, 0.0, 0.0)).a;
                nearby = max(nearby, alpha * coverage);
            }
        }
    }
    float alpha = max(nearby - bodyAlpha, 0.0) * uOutlineColor.a;
    return float4(uOutlineColor.rgb * alpha, alpha) * tint;
}

technique CharacterFineOutline
{
    pass Draw
    {
        PixelShader = compile ps_3_0 FineOutlinePS();
    }
}

technique CharacterPixelOutline
{
    pass Draw
    {
        PixelShader = compile ps_3_0 PixelOutlinePS();
    }
}
