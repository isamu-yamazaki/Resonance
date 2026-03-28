Shader "Resonance/Electrocute"
{
    Properties
    {
        _ElectrocuteTime    ("Electrocute Time (0-1)", Range(0, 1)) = 0.0

        [Header(Shell)]
        _ShellOffset        ("Shell Offset", Float) = 0.04

        [Header(Arc)]
        _ArcStrength        ("Arc Strength", Float) = 0.03
        _ArcFrequency       ("Arc Frequency", Float) = 28.0
        _ArcSpeed           ("Arc Speed", Float) = 20.0
        _ArcThinness        ("Arc Thinness", Float) = 8.0

        [Header(Glow)]
        _RimColor           ("Rim Color", Color) = (0.4, 0.8, 1.0, 1.0)
        _RimEmission        ("Rim Emission", Float) = 6.0
        _RimPower           ("Rim Power", Float) = 2.5
        _CoreColor          ("Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _CoreEmission       ("Core Emission", Float) = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent+1"
        }

        Cull Back
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _ElectrocuteTime;
                float  _ShellOffset;
                float  _ArcStrength;
                float  _ArcFrequency;
                float  _ArcSpeed;
                float  _ArcThinness;
                float4 _RimColor;
                float  _RimEmission;
                float  _RimPower;
                float4 _CoreColor;
                float  _CoreEmission;
            CBUFFER_END

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
                float3 positionWS  : TEXCOORD2;
            };

            float Hash(float n)   { return frac(sin(n) * 43758.5453); }
            float Hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float ArcNoise(float2 p, float speed)
            {
                float t = _Time.y * speed;
                float a = Hash2(floor(p * 8.0)  + floor(t * 0.5));
                float b = Hash2(floor(p * 16.0) + floor(t));
                float c = Hash2(floor(p * 32.0) + floor(t * 2.0));
                return a * 0.5 + b * 0.3 + c * 0.2;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float t = _ElectrocuteTime;

                // Expand shell along normal — pushes mesh outside the player surface
                float jitterSeed   = floor(_Time.y * 30.0);
                float jitter       = Hash2(float2(input.positionOS.y * 6.0, jitterSeed)) * _ArcStrength;
                float jitterActive = smoothstep(0.0, 0.1, t) * smoothstep(1.0, 0.6, t);
                float3 posOS       = input.positionOS.xyz + input.normalOS * (_ShellOffset + jitter * jitterActive);

                VertexPositionInputs posInputs    = GetVertexPositionInputs(posOS);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.uv          = input.uv;
                output.normalWS    = normalInputs.normalWS;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _ElectrocuteTime;

                float3 normalWS = normalize(input.normalWS);
                float3 viewDir  = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Rim mask — only show at silhouette edges, invisible in the interior
                float rim     = 1.0 - saturate(dot(normalWS, viewDir));
                float rimMask = pow(rim, _RimPower);

                // Arc lines crawling along the shell
                float arcNoise   = ArcNoise(input.uv, _ArcSpeed);
                float arcBand    = abs(sin(input.uv.x * _ArcFrequency + arcNoise * 6.28));
                float arcMask    = pow(arcBand, _ArcThinness);
                float arcPulse   = smoothstep(0.0, 0.1, t) * smoothstep(1.0, 0.65, t);
                float arcFlicker = step(Hash(floor(_Time.y * 28.0)), 0.8);

                // Combine rim and arcs — arcs appear anywhere, rim restricts base glow to edges
                half3 rimGlow = _RimColor.rgb * rimMask * _RimEmission;
                half3 arcGlow = lerp(_RimColor.rgb, _CoreColor.rgb, arcMask) * arcMask * _RimEmission * arcFlicker;

                // Core flash at impact
                float coreFlash = smoothstep(0.0, 0.05, t) * smoothstep(0.25, 0.08, t);
                half3 coreGlow  = _CoreColor.rgb * _CoreEmission * coreFlash;

                half3 color = rimGlow + arcGlow * arcPulse + coreGlow;

                // Alpha — rim-based so interior of shell is transparent
                float flickerSeed  = floor(t * 40.0);
                float flickerAlpha = lerp(0.7, 1.0, step(0.3, Hash(flickerSeed + 3.7)));
                float fadeOut      = smoothstep(1.0, 0.65, t);
                float alpha        = (rimMask + arcMask * arcPulse * arcFlicker) * fadeOut * flickerAlpha;

                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
