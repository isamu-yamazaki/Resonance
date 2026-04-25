Shader "Resonance/BubbleShield"
{
    Properties
    {
        [HDR] _RimColor           ("Rim Color",             Color)       = (0.2, 0.8, 1.0, 1.0)
        [HDR] _HexColor           ("Hex Edge Color",        Color)       = (0.1, 0.6, 1.0, 1.0)
        [HDR] _FillColor          ("Fill Color",            Color)       = (0.05, 0.3, 0.6, 0.4)
        _RimPower                 ("Rim Power",             Float)       = 3.0
        _RimBias                  ("Rim Bias",              Range(0,1))  = 0.1
        _FresnelIntensity         ("Fresnel Intensity",     Float)       = 1.5
        _HexScale                 ("Hex Scale",             Float)       = 8.0
        _HexEdgeWidth             ("Hex Edge Width",        Range(0,0.5)) = 0.08
        _HexEdgeIntensity         ("Hex Edge Intensity",    Float)       = 2.5
        _DistortionStrength       ("Distortion Strength",   Range(0,0.05)) = 0.015
        _DistortionTiling         ("Distortion Tiling",     Float)       = 4.0
        _DistortionSpeed          ("Distortion Speed",      Float)       = 0.4
        _DepthFadeDistance        ("Depth Fade Distance",   Float)       = 0.4
        _DissolveProgress         ("Dissolve Progress",     Range(0,1))  = 0.0
        _DissolveEdgeWidth        ("Dissolve Edge Width",   Range(0,0.3)) = 0.06
        [HDR] _DissolveEdgeColor  ("Dissolve Edge Color",   Color)       = (0.4, 1.0, 1.0, 1.0)
        _DissolveNoiseScale       ("Dissolve Noise Scale",  Float)       = 2.5
        _DissolveNoiseSpeed       ("Dissolve Noise Speed",  Float)       = 0.15
        [HDR] _HitColor           ("Hit Flash Color",       Color)       = (1.0, 0.3, 0.1, 1.0)
        _HitFlash                 ("Hit Flash",             Range(0,1))  = 0.0
        _PulseSpeed               ("Pulse Speed",           Float)       = 1.2
        _PulseIntensity           ("Pulse Intensity",       Range(0,1))  = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "BubbleShieldForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // _CameraOpaqueTexture is only available when Opaque Texture is enabled
            // in the URP Renderer asset. We declare it manually so the shader doesn't
            // hard-fail if it's missing — it'll just sample black instead of the scene.
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                half4 _HexColor;
                half4 _FillColor;
                half  _RimPower;
                half  _RimBias;
                half  _FresnelIntensity;
                half  _HexScale;
                half  _HexEdgeWidth;
                half  _HexEdgeIntensity;
                half  _DistortionStrength;
                half  _DistortionTiling;
                half  _DistortionSpeed;
                half  _DepthFadeDistance;
                half  _DissolveProgress;
                half  _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                half  _DissolveNoiseScale;
                half  _DissolveNoiseSpeed;
                half4 _HitColor;
                half  _HitFlash;
                half  _PulseSpeed;
                half  _PulseIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 screenPos   : TEXCOORD3;
                float  localY      : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float  a = hash2(i).x;
                float  b = hash2(i + float2(1, 0)).x;
                float  c = hash2(i + float2(0, 1)).x;
                float  d = hash2(i + float2(1, 1)).x;
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                return vnoise(p) * 0.6 + vnoise(p * 2.1 + float2(5.3, 1.7)) * 0.4;
            }

            float hexEdge(float2 uv, float scale)
            {
                uv *= scale;
                float2 s  = float2(1.0, 1.7320508);
                float2 a  = fmod(uv,           s) - s * 0.5;
                float2 b  = fmod(uv + s * 0.5, s) - s * 0.5;
                float2 gv = (dot(a, a) < dot(b, b)) ? a : b;
                return max(abs(gv.x) * 1.1547, abs(gv.y));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = vpi.positionCS;
                OUT.normalWS    = vni.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(vpi.positionWS);
                OUT.uv          = IN.uv;
                OUT.screenPos   = ComputeScreenPos(vpi.positionCS);
                OUT.localY      = saturate(IN.positionOS.y + 0.5);

                return OUT;
            }

            half4 frag(Varyings IN, float vface : VFACE) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                // ── Dissolve ──────────────────────────────────────────────────
                float noiseVal  = fbm(IN.uv * _DissolveNoiseScale
                                    + _Time.y * _DissolveNoiseSpeed);
                float threshold = 1.0 - _DissolveProgress;
                float dissolve  = IN.localY - threshold
                                + (noiseVal - 0.5) * _DissolveEdgeWidth * 2.0;
                clip(dissolve);

                // ── Depth fade ────────────────────────────────────────────────
                float sceneRaw   = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(sceneRaw, _ZBufferParams);
                float depthFade  = saturate((sceneDepth - IN.screenPos.w)
                                           / _DepthFadeDistance);

                // ── Inner face: nearly invisible ──────────────────────────────
                if (vface < 0.0)
                {
                    half3 col   = lerp(_FillColor.rgb, _HitColor.rgb, _HitFlash);
                    half  alpha = 0.07 * depthFade;
                    return half4(col, alpha);
                }

                // ── Outer face ────────────────────────────────────────────────
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                half NdotV  = saturate(dot(N, V));
                half fresnel = pow(1.0 - NdotV + _RimBias, _RimPower);
                fresnel      = saturate(fresnel * _FresnelIntensity);
                fresnel     *= 1.0 + (0.5 + 0.5 * sin(_Time.y * _PulseSpeed)) * _PulseIntensity;

                // Distortion
                float2 distUV      = IN.uv * _DistortionTiling + _Time.y * _DistortionSpeed;
                float2 offset      = float2(fbm(distUV),
                                           fbm(distUV + float2(3.7, 1.9))) - 0.5;
                float2 distortedUV = screenUV + offset * _DistortionStrength;
                half3  sceneBehind = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, distortedUV).rgb;

                // Hex
                float hexD        = hexEdge(IN.uv, _HexScale);
                float hexEdgeMask = smoothstep(1.0 - _HexEdgeWidth, 1.0, hexD);

                // Dissolve edge glow
                float edgeMask = smoothstep(0.0, _DissolveEdgeWidth, dissolve)
                               * smoothstep(_DissolveEdgeWidth * 2.0, 0.0, dissolve);

                // Colour
                half3 col = lerp(sceneBehind, _FillColor.rgb, _FillColor.a);
                col = lerp(col, _HexColor.rgb  * _HexEdgeIntensity, hexEdgeMask);
                col = lerp(col, _RimColor.rgb  * _FresnelIntensity, fresnel);
                col += _DissolveEdgeColor.rgb * edgeMask;
                col  = lerp(col, _HitColor.rgb, _HitFlash);

                half alpha = lerp(_FillColor.a * 0.4, 1.0,
                                  saturate(fresnel + hexEdgeMask * 0.6));
                alpha = saturate(alpha) * depthFade;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
