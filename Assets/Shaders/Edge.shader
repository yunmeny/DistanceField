Shader "Image/Edge"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            float4 _TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                float2 off = _TexelSize.xy;

                float4 col0 = tex2D(_MainTex, i.uv+float2(off.x,0));
                float4 col1 = tex2D(_MainTex, i.uv+float2(-off.x, 0));

                float4 col2 = tex2D(_MainTex, i.uv+float2(0, off.y));
                float4 col3 = tex2D(_MainTex, i.uv+float2(0, -off.y));

                if (abs(col0.r - col1.r) > 0.1 || abs(col2.r - col3.r) > 0.1)
                {
                    return 1;
                }
                return 0;
            }
            ENDCG
        }
    }
}
