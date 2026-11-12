// 바람에 흔들리는 조명 셰이더. 정점 색 R = 휨(밑동 0 → 끝 1), G = 잎 떨림.
// URP와 Built-in 모두 지원 (SubShader 2개). 그림자도 같이 흔들린다.
Shader "ForestWhispers/WindLit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Float) = 0.8
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.45
        _WindStrength ("Wind Strength", Float) = 0.12
        _WindSpeed ("Wind Speed", Float) = 1.3
        _LeafFlutter ("Leaf Flutter", Float) = 0.035
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.25, 0)
    }

    // ---------------- URP ----------------
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Back

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half _BumpScale, _Cutoff, _WindStrength, _WindSpeed, _LeafFlutter;
            float4 _WindDirection;
        CBUFFER_END
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

        float3 ApplyWind(float3 positionWS, float4 color)
        {
            float t = _Time.y * _WindSpeed;
            float gust = sin(t * 0.37 + positionWS.x * 0.05) * 0.5 + 0.5;
            float sway = sin(t + positionWS.x * 0.35) + sin(t * 1.9 + positionWS.x * 0.17) * 0.35;
            float3 dir = normalize(_WindDirection.xyz + 1e-4);
            positionWS += dir * sway * color.r * color.r * _WindStrength * (0.6 + gust * 0.8);
            float flutter = sin(t * 4.3 + positionWS.x * 2.1 + positionWS.y * 1.7) * cos(t * 3.1 + positionWS.y * 2.6);
            positionWS += float3(flutter, flutter * 0.6, flutter * 0.8) * color.g * _LeafFlutter * (0.7 + gust * 0.6);
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; float4 tangentWS : TEXCOORD3; float fog : TEXCOORD4; };

            Varyings vert (Attributes input)
            {
                Varyings o;
                float3 positionWS = ApplyWind(TransformObjectToWorld(input.positionOS.xyz), input.color);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.positionWS = positionWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.normalWS = n.normalWS;
                o.tangentWS = float4(n.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                clip(albedo.a - _Cutoff);
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                float3 bitangent = cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w;
                float3 normalWS = normalize(mul(nTS, float3x3(i.tangentWS.xyz, bitangent, i.normalWS)));
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(normalWS, light.direction)) * 0.8 + 0.2;
                half3 color = albedo.rgb * (light.color * ndl * light.shadowAttenuation + SampleSH(normalWS));
                color = MixFog(color, i.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert (Attributes input)
            {
                Varyings o;
                float3 positionWS = ApplyWind(TransformObjectToWorld(input.positionOS.xyz), input.color);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * _Color.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    // ---------------- Built-in ----------------
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Back
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert alphatest:_Cutoff addshadow
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap;
        fixed4 _Color;
        half _BumpScale, _WindStrength, _WindSpeed, _LeafFlutter;
        float4 _WindDirection;
        struct Input { float2 uv_MainTex; };

        void vert (inout appdata_full v)
        {
            float3 positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
            float t = _Time.y * _WindSpeed;
            float gust = sin(t * 0.37 + positionWS.x * 0.05) * 0.5 + 0.5;
            float sway = sin(t + positionWS.x * 0.35) + sin(t * 1.9 + positionWS.x * 0.17) * 0.35;
            float3 dir = normalize(_WindDirection.xyz + 1e-4);
            positionWS += dir * sway * v.color.r * v.color.r * _WindStrength * (0.6 + gust * 0.8);
            float flutter = sin(t * 4.3 + positionWS.x * 2.1 + positionWS.y * 1.7) * cos(t * 3.1 + positionWS.y * 2.6);
            positionWS += float3(flutter, flutter * 0.6, flutter * 0.8) * v.color.g * _LeafFlutter * (0.7 + gust * 0.6);
            v.vertex.xyz = mul(unity_WorldToObject, float4(positionWS, 1)).xyz;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
        }
        ENDCG
    }

    Fallback "Legacy Shaders/Transparent/Cutout/Diffuse"
}
