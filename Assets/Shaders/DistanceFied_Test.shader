Shader "Unlit/DistanceFied_Test"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainTex1 ("Texture", 2D) = "white" {}
        _slider("slider", range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MainTex;
            sampler2D _MainTex1;
            float _slider;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                float4 col1 = tex2D(_MainTex,i.uv);
                float4 col2 = tex2D(_MainTex1, i.uv);

                float dis1 = col1.r - col1.g;
                float dis2 = col2.r - col2.g;
                return  smoothstep(0,0.02,lerp(dis1, dis2, _slider));
            }
            ENDCG
        }
    }
}
