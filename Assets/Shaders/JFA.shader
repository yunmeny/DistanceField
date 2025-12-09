Shader "SDF/JFA"
{
    Properties
    {
        _SDFTexture("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            sampler2D _SDFTexture;
            sampler2D _Texture;

            float4 _SDFTexture_TexelSize;
            float4 _Texture_TexelSize;
            
            int _Level;
            int _Type;
            bool _Inverse;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            float2 StepJFA(float2 uv,int level)
            {
                //2^10 = 1024 
                level = clamp(level - 1.0, 0.0, 10);
                float stepSize =  floor(exp2(10 - level) + 0.5);

                float minDistance = 999999;
                float2 bestCoord = float2(0, 0);

                for (int y = -1; y < 2; y++)
                for (int x = -1; x < 2; x++)
                {
                    float2 sampleCoord = uv + float2(x, y) * _SDFTexture_TexelSize.xy *stepSize;

                    float4 seedCoord = tex2D(_SDFTexture, sampleCoord);

                    float dst = distance(seedCoord.xy,uv);

                    if ((seedCoord.x!=0.0 || seedCoord.y!=0.0)&& (dst < minDistance))
                    {
                        minDistance = dst;

                        bestCoord = seedCoord.xy;
                    }
                }
                return bestCoord;
            }


            float4 frag(v2f i) : SV_Target
            {
                if (_Level == 0)
                {
                    int mask = 0;
                    
                    if (_Inverse)
                        mask = (tex2D(_Texture, i.uv).r < 0.1 ? 1 : 0);
                    else
                        mask = (tex2D(_Texture, i.uv).r > 0.1 ? 1 : 0);

                    return float4(i.uv.xy, 0, 0) * mask;
                }

                switch (_Type)
                {
                    case 0:
                        return float4(StepJFA(i.uv, _Level), 0, 0);
                    case 1:
                        return float4(distance(i.uv.xy, tex2D(_Texture, i.uv).xy), 0, 0, 0);
                }
                return float4(0, 0, 0, 0);
            }
            ENDCG
        }

        //EncodeFloatRGBA
        //DecodeFloatRGBA
    }
}
