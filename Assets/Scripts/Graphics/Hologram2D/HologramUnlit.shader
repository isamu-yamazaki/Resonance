Shader "Custom/HologramUnlit"
{
    Properties
    {
        _MainTex ("Image", 2D) = "white" {}
        _HoloColor ("Tint (white = no tint)", Color) = (1.0, 1.0, 1.0, 1.0)
        _Opacity ("Base Opacity", Range(0,1)) = 0.75
        _EmissiveBoost ("Emissive Boost", Range(0,2)) = 0.3
        _FlickerStrength ("Flicker Strength", Range(0,1)) = 0.15
        _ScanlineSpeed ("Scanline Speed", Float) = 1.0
        _ScanlineDensity ("Scanline Density", Float) = 80.0
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.25
        _ChromaOffset ("Chromatic Aberration", Range(0, 0.02)) = 0.004
        _GlitchStrength ("Glitch Strength", Range(0,1)) = 0.08
        _NoiseTime ("Internal Time", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _HoloColor;
                float _Opacity;
                float _EmissiveBoost;
                float _FlickerStrength;
                float _ScanlineSpeed;
                float _ScanlineDensity;
                float _ScanlineStrength;
                float _ChromaOffset;
                float _GlitchStrength;
                float _NoiseTime;
            CBUFFER_END

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Glitch horizontal offset
                float glitchLine = step(0.98, rand(float2(floor(uv.y * 20.0), floor(_NoiseTime * 8.0))));
                float glitchOffset = (rand(float2(uv.y, _NoiseTime)) - 0.5) * _GlitchStrength * glitchLine;
                uv.x += glitchOffset;

                // Chromatic aberration
                float2 redUV   = uv + float2(_ChromaOffset, 0);
                float2 greenUV = uv;
                float2 blueUV  = uv - float2(_ChromaOffset, 0);

                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, redUV).r;
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, greenUV).g;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, blueUV).b;
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;

                half4 texColor = half4(r, g, b, a);

                // Scanlines
                float scanline = sin(uv.y * _ScanlineDensity + _NoiseTime * _ScanlineSpeed * 6.2832);
                scanline = lerp(1.0, (scanline * 0.5 + 0.5), _ScanlineStrength);

                // Flicker
                float flicker = 1.0 - _FlickerStrength * rand(float2(_NoiseTime * 0.3, 0.5));

                // Compose
                half4 holoColor = texColor * _HoloColor;
                holoColor.rgb += texColor.rgb * _EmissiveBoost;
                holoColor.rgb *= scanline;
                holoColor.a = texColor.a * _Opacity * flicker;

                return holoColor;
            }
            ENDHLSL
        }
    }
}
