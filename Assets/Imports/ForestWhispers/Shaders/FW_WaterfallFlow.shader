// 폭포 스프라이트 위로 물줄기가 흘러내리는 효과. 원본 그림의 밝은 부분(물)에만 적용된다. Built-in, URP 모두 동작.
Shader "ForestWhispers/WaterfallFlow"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Speed ("Flow Speed", Float) = 1.1
        _Streaks ("Streak Count", Float) = 46
        _Strength ("Strength", Range(0,1)) = 0.45
        _FogColor ("Fog Color", Color) = (1,1,1,1)
        _Fog ("Fog", Range(0,1)) = 0
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
            float _Speed, _Streaks, _Strength, _Fog;
            fixed4 _FogColor;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            float hash(float n) { return frac(sin(n * 12.9898) * 43758.5453); }
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
                float t = _Time.y * _Speed;
                float column = floor(i.uv.x * _Streaks);
                float offset = hash(column);
                float speed = 0.7 + hash(column + 7.1) * 0.6;
                float wobble = sin(i.uv.y * 18.0 + t * 3.0 + offset * 6.28) * 0.004;
                fixed4 c = tex2D(_MainTex, i.uv + float2(wobble, 0)) * i.color;
                float streak = frac(i.uv.y * (1.5 + offset * 2.0) + t * speed + offset);
                streak = smoothstep(0.0, 0.18, streak) * (1.0 - smoothstep(0.18, 0.55, streak));
                float water = saturate((max(c.b, c.g) - 0.45) * 3.0);
                c.rgb += streak * _Strength * water;
                c.rgb = lerp(c.rgb, _FogColor.rgb, _Fog);
                return c;
            }
            ENDCG
        }
    }
}
