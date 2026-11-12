// 깊이 안개와 바람 흔들림이 있는 스프라이트. Built-in, URP 모두 동작.
Shader "ForestWhispers/Sprite25D"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (1,1,1,1)
        _Fog ("Fog", Range(0,1)) = 0
        _Sway ("Wind Sway", Range(0,2)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _FogColor;
            float _Fog, _Sway;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                float tip = v.uv.y * v.uv.y;
                world.x += sin(_Time.y * 1.7 + world.x * 0.8) * 0.09 * _Sway * tip;
                world.x += sin(_Time.y * 3.1 + world.x * 2.1) * 0.03 * _Sway * tip;
                o.pos = mul(UNITY_MATRIX_VP, world);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                c.rgb = lerp(c.rgb, _FogColor.rgb, _Fog);
                return c;
            }
            ENDCG
        }
    }
}
