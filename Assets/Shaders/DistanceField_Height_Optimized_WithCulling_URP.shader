Shader "DistanceField_Height_Optimized_WithCulling_URP"
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
        [HideInInspector] _GlobalCentroid("Global Centroid", Vector) = (0.5, 0.5, 0, 0) // 新增全局变量
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
            float2 _GlobalCentroid; // 声明全局质心变量（来自CPU）

            

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
                float2 _TextureSample0_Speed = float2(0.01, 0.01);
                float2 uvMove = _Time.y * _TextureSample0_Speed; // 随时间生成移动偏移
                // 以第二张纹理为基准计算质心
                float2 uv_TextureSample0 = (i.ase_texcoord1.xy ) * _TextureSample0_ST.xy + _TextureSample0_ST.
                    zw;
                // float2 uv_TextureSample0 = i.ase_texcoord1.xy * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
                float4 tex2DNode2 = SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, uv_TextureSample0);
                float2 uv_TextureSample1 = i.ase_texcoord1.xy * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
                float4 tex2DNode4 = SAMPLE_TEXTURE2D(_TextureSample1, sampler_TextureSample1, uv_TextureSample1);
                float distField0 = tex2DNode2.r - tex2DNode2.g;
                float distField1 = tex2DNode4.r - tex2DNode4.g;
                
                float lerpResult1 = lerp(distField0, distField1, _Lerp);
                float culling = smoothstep(
                    _CullingThreshold - _CullingSmoothness,
                    _CullingThreshold + _CullingSmoothness,
                    lerpResult1
                );
                if (culling > 0) discard;
                float delta = fwidth(lerpResult1);
                float smoothstepResult12 = smoothstep(-delta, delta, lerpResult1);
                smoothstepResult12 = pow(smoothstepResult12, 0.8);
                smoothstepResult12 = smoothstepResult12 * culling;
                return half4(smoothstepResult12, smoothstepResult12, smoothstepResult12, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}