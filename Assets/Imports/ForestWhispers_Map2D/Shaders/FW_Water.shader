// 미리 구운 반사 이미지를 일렁이게 하는 수면. Built-in, URP 모두 동작.
Shader "ForestWhispers/Water"
{
    Properties
    {
        _MainTex ("Reflection", 2D) = "white" {}
        _Shallow ("Shallow", Color) = (0.1,0.6,0.8,1)
        _Deep ("Deep", Color) = (0.1,0.6,0.8,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Shallow, _Deep;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float depth = 1.0 - i.uv.y;
                float t = _Time.y;
                float w = sin(i.uv.y * 90.0 + t * 1.7) * 0.5 + sin(i.uv.y * 41.0 - t * 1.1 + i.uv.x * 30.0) * 0.5;
                float2 uv = i.uv + float2(w * 0.006 * (depth + 0.1), w * 0.012 * (depth + 0.1));
                fixed4 c = tex2D(_MainTex, uv);
                fixed3 tint = lerp(_Shallow.rgb, _Deep.rgb, depth);
                c.rgb = lerp(c.rgb, tint, 0.04 + 0.12 * depth);
                float glint = sin(i.uv.x * 240.0 + t * 2.0 + w * 3.0) * sin(i.uv.y * 160.0 - t * 1.4);
                c.rgb += smoothstep(0.86, 1.0, glint) * 0.22;
                c.a = 1.0;
                return c * i.color;
            }
            ENDCG
        }
    }
}
