// 배경판 이음 띠. 카메라 진행도(_Blend: 왼쪽 구간 중앙 0 → 오른쪽 구간 중앙 1)에 따라 이웃 배경판으로 넘어간다.
// 띠의 왼쪽 끝은 항상 왼쪽 배경판, 오른쪽 끝은 항상 오른쪽 배경판과 같아서 어느 위치에서도 끊긴 선이 생기지 않는다.
// Built-in, URP 모두 동작.
Shader "ForestWhispers/PlateBlend"
{
    Properties
    {
        _MainTex ("Plate", 2D) = "white" {}
        _Blend ("Blend", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-99" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Blend;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 ramp : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float ramp : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.ramp = v.ramp.x;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                // 0~0.5: 넘어가는 구간이 오른쪽 끝에서 왼쪽으로 넓어짐, 0.5~1: 왼쪽 끝으로 좁아짐
                float lo = _Blend <= 0.5 ? 1 - 2 * _Blend : 0;
                float hi = _Blend <= 0.5 ? 1 : 2 * (1 - _Blend);
                c.a = smoothstep(lo, max(hi, lo + 0.001), i.ramp);
                return c;
            }
            ENDCG
        }
    }
}
