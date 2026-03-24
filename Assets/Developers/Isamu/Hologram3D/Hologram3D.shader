Shader "Custom/Hologram3D"
{
    Properties
    {
        _HoloColor ("Hologram Color", Color) = (0.4, 0.6, 1.0, 1.0)
        _Opacity ("Base Opacity", Range(0,1)) = 0.6
        _EmissiveBoost ("Emissive Boost", Range(0,3)) = 1.0

        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.5, 8.0)) = 2.0
        _FresnelStrength ("Fresnel Strength", Range(0,2)) = 1.0
        _FresnelOpacityBias ("Fresnel Opacity Bias", Range(0,1)) = 0.2

        [Header(Scanlines)]
        _ScanlineSpeed ("Scanline Speed", Float) = 1.0
        _ScanlineDensity ("Scanline Density", Float) = 60.0
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.3

        [Header(Flicker)]
        _FlickerStrength ("Flicker Strength", Range(0,1)) = 0.1

        [Header(Vertex Wobble)]
        _WobbleStrength ("Wobble Strength", Range(0, 0.05)) = 0.01
        _WobbleSpeed ("Wobble Speed", Float) = 2.0

        _NoiseTime ("Internal Time", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _HoloColor;
                float _Opacity;
                float _EmissiveBoost;
                float _FresnelPower;
                float _FresnelStrength;
                float _FresnelOpacityBias;
                float _ScanlineSpeed;
                float _ScanlineDensity;
                float _ScanlineStrength;
                float _FlickerStrength;
                float _WobbleStrength;
                float _WobbleSpeed;
                float _NoiseTime;
            CBUFFER_END

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Vertex wobble along normal
                float wobble = sin(_NoiseTime * _WobbleSpeed + input.positionOS.y * 10.0) * _WobbleStrength;
                input.positionOS.xyz += input.normalOS * wobble;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Fresnel
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);

                // Scanlines in world space so they scroll consistently across the mesh
                float scanline = sin(input.positionWS.y * _ScanlineDensity + _NoiseTime * _ScanlineSpeed * 6.2832);
                scanline = lerp(1.0, (scanline * 0.5 + 0.5), _ScanlineStrength);

                // Flicker
                float flicker = 1.0 - _FlickerStrength * rand(float2(_NoiseTime * 0.3, 0.5));

                // Opacity — fresnel drives edge visibility, interior fades out
                float opacity = lerp(_FresnelOpacityBias, 1.0, fresnel) * _Opacity * flicker;

                // Color
                half3 color = _HoloColor.rgb;
                color += fresnel * _FresnelStrength * _HoloColor.rgb; // rim boost
                color *= (1.0 + _EmissiveBoost);
                color *= scanline;

                return half4(color, opacity);
            }
            ENDHLSL
        }
    }
}
