Shader "Resonance/PlayerSpotlight"
{
    Properties
    {
        _Color ("Beam Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _Intensity ("Intensity", Range(0, 1000)) = 600.0
        _FalloffPower ("Falloff Power", Range(0.1, 3)) = 0.6
        _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.15
        _TopGlow ("Top Glow Boost", Range(0, 3)) = 1.2
        _ConeTopRadius ("Cone Top Radius", Range(0, 1)) = 1.0
        _ConeBottomRadius ("Cone Bottom Radius", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+1"
        }

        // Pass 1: outside view
        Pass
        {
            Name "PlayerSpotlight_Outside"
            Blend One One
            ZWrite Off
            Cull Back
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float fogFactor    : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _FalloffPower;
                half _EdgeSoftness;
                half _TopGlow;
                half _ConeTopRadius;
                half _ConeBottomRadius;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Pinch vertices — wide at top (uv.y=1), narrow at bottom (uv.y=0)
                float coneScale = lerp(_ConeTopRadius, _ConeBottomRadius, IN.uv.y);
                float3 pos = IN.positionOS.xyz;
                pos.xz *= coneScale;

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;

                // Recalculate normal after displacement
                float3 normal = normalize(float3(IN.normalOS.x * coneScale, IN.normalOS.y, IN.normalOS.z * coneScale));
                OUT.normalWS = TransformObjectToWorldNormal(normal);

                float3 positionWS = TransformObjectToWorld(pos);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float verticalFade = pow(IN.uv.y, _FalloffPower);
                float topGlow = saturate((IN.uv.y - 0.8) / 0.2) * _TopGlow;
                verticalFade = saturate(verticalFade + topGlow);

                float edgeFade = saturate(abs(dot(normalize(IN.normalWS), normalize(IN.viewDirWS))) / _EdgeSoftness);

                half3 color = _Color.rgb * _Intensity * verticalFade * edgeFade;
                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Pass 2: inside view
        Pass
        {
            Name "PlayerSpotlight_Inside"
            Blend One One
            ZWrite Off
            Cull Front
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float fogFactor    : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _FalloffPower;
                half _EdgeSoftness;
                half _TopGlow;
                half _ConeTopRadius;
                half _ConeBottomRadius;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float coneScale = lerp(_ConeTopRadius, _ConeBottomRadius, IN.uv.y);
                float3 pos = IN.positionOS.xyz;
                pos.xz *= coneScale;

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;

                float3 normal = normalize(float3(IN.normalOS.x * coneScale, IN.normalOS.y, IN.normalOS.z * coneScale));
                OUT.normalWS = -TransformObjectToWorldNormal(normal);

                float3 positionWS = TransformObjectToWorld(pos);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float verticalFade = pow(IN.uv.y, _FalloffPower);
                float topGlow = saturate((IN.uv.y - 0.8) / 0.2) * _TopGlow;
                verticalFade = saturate(verticalFade + topGlow);

                float edgeFade = saturate(abs(dot(normalize(IN.normalWS), normalize(IN.viewDirWS))) / _EdgeSoftness);

                half3 color = _Color.rgb * _Intensity * verticalFade * edgeFade;
                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
