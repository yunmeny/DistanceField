void CircleSDF_float(float2 UV, float Radius, out float Dist)
{
    Dist = length(UV) - Radius;
}


// 输入说明（适用于 Unity Shader Graph Custom Function）:
// Texture2D tex         - 纹理（例如 _MainTex）
// SamplerState samp     - 采样器（例如 sampler_MainTex）
// float2 uv             - 目标查找区域中心 UV（0..1）
// int radius            - 采样半径（像素级），半径为 3 则采 7x7
// float threshold       - 亮度阈值，低于该值的像素权重为 0（用来剪掉背景）
// float2 texSize        - 纹理分辨率（像素），例如 float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w)

void ApproxCentroid_float(float4 m_col, float2 uv, int radius, float threshold, float2 texSize, out float2 m_centroid)
{
    // 累加器
    float2 accum = float2(0.0, 0.0);
    float w_sum = 0.0;
    // 将 uv 转为像素坐标中心（以 texel 中心为采样点）
    float2 pixel_center = uv * texSize;

    // 常用亮度权重
    const float3 lum_weights = float3(0.2126, 0.7152, 0.0722);

    for (int y = -radius; y <= radius; y++)
    {
        for (int x = -radius; x <= radius; x++)
        {
            float2 p = pixel_center + float2(x, y) + 0.5;
            float2 sample_uv = p / texSize;
            float4 col = m_col;
            float lum = dot(col.rgb, lum_weights);
            float weight = max(0.0, lum - threshold);
            if (weight > 0.0)
            {
                accum += sample_uv * weight;
                w_sum += weight;
            }
        }
    }

    if (w_sum > 0.0)
    {
        m_centroid = accum / w_sum; // 返回 UV 空间的质心 (0..1)
    }
    else
    {
        m_centroid = uv; // 未找到任何显著像素，则返回输入中心作为退化值
    }
}
