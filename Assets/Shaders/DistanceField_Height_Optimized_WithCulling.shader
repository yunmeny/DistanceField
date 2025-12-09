Shader "DistanceField_Height_Optimized_WithCulling"
{
    Properties
    {
        _TextureSample0("Texture Sample 0", 2D) = "white" {}
        _TextureSample1("Texture Sample 1", 2D) = "white" {}
        _Float0("Float 0", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0.1, 1.0)) = 0.2
        _CullingThreshold("Culling Threshold", Range(0.0, 0.5)) = 0.1 // 背景剔除阈值
        _CullingSmoothness("Culling Smoothness", Range(0.01, 0.2)) = 0.05 // 剔除边缘平滑度
        [HideInInspector] _texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        CGINCLUDE
        #pragma target 3.0
        ENDCG
        Blend Off
        Cull Back
        ColorMask RGBA
        ZWrite On
        ZTest LEqual
        Offset 0, 0

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

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

            uniform sampler2D _TextureSample0;
            uniform float4 _TextureSample0_ST;
            uniform sampler2D _TextureSample1;
            uniform float4 _TextureSample1_ST;
            uniform float _Float0;
            uniform float _Smoothness;
            uniform float _CullingThreshold;
            uniform float _CullingSmoothness;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.ase_texcoord1.xy = v.ase_texcoord.xy;
                o.ase_texcoord1.zw = 0;

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float2 uv_TextureSample0 = i.ase_texcoord1.xy * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
                float4 tex2DNode2 = tex2D(_TextureSample0, uv_TextureSample0);
                float2 uv_TextureSample1 = i.ase_texcoord1.xy * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
                float4 tex2DNode4 = tex2D(_TextureSample1, uv_TextureSample1);

                // 计算距离场差值
                float distField0 = tex2DNode2.r - tex2DNode2.g;
                float distField1 = tex2DNode4.r - tex2DNode4.g;
                
                // 插值计算
                float lerpResult1 = lerp(distField0, distField1, _Float0);
                
                // 背景剔除处理
                // 计算剔除边缘的平滑过渡
                float culling = smoothstep(
                    _CullingThreshold - _CullingSmoothness, 
                    _CullingThreshold + _CullingSmoothness, 
                    lerpResult1
                );
                
                // 如果完全在背景区域，直接剔除
                if (culling > 0.01)
                {
                    discard; // 剔除像素
                }
                
                // 计算屏幕空间偏导数用于抗锯齿
                float delta = fwidth(lerpResult1) * _Smoothness;
                
                // 优化的smoothstep计算
                float smoothstepResult12 = smoothstep(-delta, delta, lerpResult1);
                
                // 增强对比度
                smoothstepResult12 = pow(smoothstepResult12, 0.8);
                
                // 结合剔除效果和抗锯齿效果
                smoothstepResult12 = smoothstepResult12 * culling;
                
                float4 temp_cast_0 = (smoothstepResult12).xxxx;

                return temp_cast_0;
            }
            ENDCG
        }
    }
}