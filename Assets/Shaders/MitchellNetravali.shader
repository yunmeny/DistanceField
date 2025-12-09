Shader "Hidden/MitchellNetravali"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ MITCHELL_NETRAVALI

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            // Mitchell-Netravali滤波器函数 (B=1/3, C=1/3)
            float MitchellNetravali(float x)
            {
                x = abs(x);
                if (x < 1.0)
                    return (1.0/6.0) * ((12.0 - 9.0/3.0 - 6.0/3.0) * x*x*x + 
                                          (-18.0 + 12.0/3.0 + 6.0/3.0) * x*x + 
                                          (6.0 - 2.0/3.0));
                if (x < 2.0)
                    return (1.0/6.0) * ((-1.0/3.0 - 6.0/3.0) * x*x*x + 
                        (6.0/3.0 + 30.0/3.0) * x*x + 
                        (-12.0/3.0 - 48.0/3.0) * x + 
                        (8.0/3.0 + 24.0/3.0));
                return 0.0;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 srcSize = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
                float2 destSize = float2(_ScreenParams.x, _ScreenParams.y);
                float2 scale = srcSize / destSize;
                
                // 计算采样坐标
                float2 uv = i.uv * srcSize;
                
                // 使用Mitchell-Netravali滤波器
                float4 color = float4(0, 0, 0, 0);
                float totalWeight = 0;
                
                for (int y = -2; y <= 2; y++)
                {
                    for (int x = -2; x <= 2; x++)
                    {
                        float2 sampleUV = uv + float2(x, y);
                        float2 normalizedUV = sampleUV / srcSize;
                        
                        if (normalizedUV.x >= 0 && normalizedUV.x <= 1 && 
                            normalizedUV.y >= 0 && normalizedUV.y <= 1)
                        {
                            float distX = abs(x * scale.x);
                            float distY = abs(y * scale.y);
                            
                            float weightX = MitchellNetravali(distX);
                            float weightY = MitchellNetravali(distY);
                            float weight = weightX * weightY;
                            
                            color += tex2D(_MainTex, normalizedUV) * weight;
                            totalWeight += weight;
                        }
                    }
                }
                
                if (totalWeight > 0)
                    color /= totalWeight;
                    
                return color;
            }
            ENDCG
        }
    }
}