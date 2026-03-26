Shader "Resonance/DeathGlitch"
{
    Properties
    {
        _BaseMap        ("Base Map", 2D) = "white" {}
        _BaseColor      ("Base Color", Color) = (1, 1, 1, 1)
        _GlitchTime     ("Glitch Time (0-1)", Range(0, 1)) = 0.0
        _SliceStrength  ("Slice Strength", Float) = 0.08
        _RGBSplit       ("RGB Split", Float) = 0.02
        _ScanlineFreq   ("Scanline Frequency", Float) = 80.0
        _ScanlineSpeed  ("Scanline Speed", Float) = 8.0
        _EdgeColor      ("Edge Color", Color) = (0, 1, 1, 1)
        _EdgeEmission   ("Edge Emission", Float) = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _GlitchTime;
                float  _SliceStrength;
                float  _RGBSplit;
                float  _ScanlineFreq;
                float  _ScanlineSpeed;
                float4 _EdgeColor;
                float  _EdgeEmission;
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
                float4 positionSS  : TEXCOORD2;
            };

            // Simple hash
            float Hash(float n) { return frac(sin(n) * 43758.5453); }
            float Hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float t = _GlitchTime;

                // Horizontal slice offset — divide mesh into bands, randomly offset each
                float worldY    = TransformObjectToWorld(input.positionOS.xyz).y;
                float sliceBand = floor(worldY * 12.0);
                float sliceSeed = floor(t * 18.0);
                float sliceRng  = Hash2(float2(sliceBand, sliceSeed));

                // Only offset some bands, more aggressively toward end
                float sliceActive = step(0.6, sliceRng) * smoothstep(0.1, 0.5, t);
                float sliceOffset = (sliceRng * 2.0 - 1.0) * _SliceStrength * sliceActive;

                float3 posOS = input.positionOS.xyz;
                posOS.x += sliceOffset;

                VertexPositionInputs posInputs    = GetVertexPositionInputs(posOS);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = posInputs.positionCS;
                output.positionSS  = ComputeScreenPos(posInputs.positionCS);
                output.uv          = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS    = normalInputs.normalWS;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _GlitchTime;

                float2 uv = input.uv;

                // RGB split — offset R and B channels horizontally
                float splitAmount = _RGBSplit * smoothstep(0.0, 0.4, t);
                float splitSeed   = floor(t * 24.0);
                float splitDir    = (Hash(splitSeed) * 2.0 - 1.0);

                float2 uvR = uv + float2(splitAmount * splitDir,  0.0);
                float2 uvB = uv + float2(-splitAmount * splitDir, 0.0);

                half r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvR).r;
                half g = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).g;
                half b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB).b;
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a;

                half3 albedo = half3(r, g, b) * _BaseColor.rgb;

                // Scanlines
                float2 screenUV   = input.positionSS.xy / input.positionSS.w;
                float  scanline   = sin(screenUV.y * _ScanlineFreq + _Time.y * _ScanlineSpeed);
                float  scanMask   = lerp(1.0, saturate(scanline * 0.5 + 0.8), smoothstep(0.0, 0.3, t));
                albedo *= scanMask;

                // Edge emission glow (rim)
                Light mainLight = GetMainLight();
                float nDotL = saturate(dot(normalize(input.normalWS), mainLight.direction));
                albedo *= mainLight.color * (nDotL * 0.7 + 0.3);

                float rim = 1.0 - saturate(abs(dot(normalize(input.normalWS),
                    GetWorldSpaceNormalizeViewDir(input.positionSS.xyz))));
                albedo += _EdgeColor.rgb * pow(rim, 2.0) * _EdgeEmission * smoothstep(0.0, 0.3, t);

                // Flicker — randomly drop alpha on some frames
                float flickerSeed  = floor(t * 30.0);
                float flickerRng   = Hash(flickerSeed + 7.3);
                float flickerAlpha = step(0.25, flickerRng);

                // Dissolve out toward end
                float dissolve = smoothstep(0.5, 1.0, t);
                float finalAlpha = (1.0 - dissolve) * flickerAlpha;

                return half4(albedo, finalAlpha);
            }
            ENDHLSL
        }
    }
}
