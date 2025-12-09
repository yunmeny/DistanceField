Shader "DistanceField_Height_Transition_URP"
{
    Properties
    {
        _TextureSample0("Texture Sample 0", 2D) = "white" {}
        _TextureSample1("Texture Sample 1", 2D) = "white" {}
        _Lerp("Lerp", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 5)) = 0.2
        _CullingThreshold("Culling Threshold", Range(0.0, 0.5)) = 0.1
        _CullingSmoothness("Culling Smoothness", Range(0.01, 0.2)) = 0.05
        [HideInInspector] _texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 100
        HLSLINCLUDE
        #pragma target 3.0
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL
        Blend Off
        Cull Back
        ColorMask RGBA
        ZWrite On
        ZTest LEqual
        Offset 0, 0

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 ase_texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float4 ase_texcoord1 : TEXCOORD1;
            };

            TEXTURE2D(_TextureSample0);
            SAMPLER(sampler_TextureSample0);
            float4 _TextureSample0_ST;
            TEXTURE2D(_TextureSample1);
            SAMPLER(sampler_TextureSample1);
            float4 _TextureSample1_ST;
            float _Lerp;
            float _Smoothness;
            float _CullingThreshold;
            float _CullingSmoothness;

            void smooth_union_float(float a, float b, float smooth, out float Out)
            {
                float h = clamp(0.5 + 0.5 * (b - a) / smooth, 0.0, 1.0);
                Out = lerp(b, a, h) - smooth * h * (1.0 - h);
            }

            float optimized_smooth_union(float a, float b, float smooth)
            {
                float diff = b - a;
                float Out;
                if (abs(diff) < smooth)
                {
                    float h = clamp(0.5 + 0.5 * diff / smooth, 0.0, 1.0);
                    float smoothFactor = h * h * (3.0 - 2.0 * h);
                    Out = lerp(a, b, smoothFactor);
                    Out -= smooth * 0.1 * (1.0 - abs(diff) / smooth);
                }
                else
                {
                    Out = min(a, b);
                }
                return Out;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.ase_texcoord1.xy = v.ase_texcoord.xy;
                o.ase_texcoord1.zw = 0;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 uv_TextureSample0 = i.ase_texcoord1.xy * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
                float4 tex2DNode2 = SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, uv_TextureSample0);
                float2 uv_TextureSample1 = i.ase_texcoord1.xy * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
                float4 tex2DNode4 = SAMPLE_TEXTURE2D(_TextureSample1, sampler_TextureSample1, uv_TextureSample1);

                // 计算两个距离场
                float distField0 = tex2DNode2.r - tex2DNode2.g;
                float distField1 = tex2DNode4.r - tex2DNode4.g;

                // 计算平滑合并的距离场（粘黏效果）
                float union_smooth_out = optimized_smooth_union(distField0, distField1, _Smoothness);

                float finalDistanceField;
                // 过渡过程中使用平滑合并
                if (_Lerp < 0.5)
                {
                    finalDistanceField = lerp(lerp(distField0, distField1, _Lerp),
                                             lerp(union_smooth_out, distField1, 0), _Lerp);
                }
                else
                {
                    finalDistanceField = lerp(lerp(union_smooth_out, distField1, 0),
                                                                            lerp(distField0, distField1, _Lerp), _Lerp);
                }
                // finalDistanceField = lerp(union_smooth_out, distField1, _Lerp);
                // 剔除计算 - 使用最终的距离场
                float culling = smoothstep(
                    _CullingThreshold - _CullingSmoothness,
                    _CullingThreshold + _CullingSmoothness,
                    finalDistanceField
                );

                // 如果需要剔除，则丢弃像素
                if (culling > 0) discard;

                // 计算边缘平滑度
                float delta = fwidth(finalDistanceField);
                float smoothstepResult12 = smoothstep(-delta, delta, finalDistanceField);
                smoothstepResult12 = pow(smoothstepResult12, 0.8);

                // 返回最终颜色
                return half4(smoothstepResult12, smoothstepResult12, smoothstepResult12, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}