Shader "Resonance/VolumetricLight"
{
    Properties
    {
        // All driven via MaterialPropertyBlock at runtime — no inspector editing needed
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent+1"
        }

        Pass
        {
            Name "VolumetricLight_CullBack"
            Blend One One
            ZWrite Off
            Cull Back
            ZTest Always

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Per-light params set via MaterialPropertyBlock
            CBUFFER_START(UnityPerMaterial)
                float4 _LightColor;
                float  _LightIntensity;
                float3 _LightPosWS;
                float3 _LightDirWS;      // normalized, points away from light
                float  _ConeAngleCos;    // cos(spotAngle * 0.5)
                float  _ConeRange;
                float  _MieG;            // Mie scattering asymmetry [-1, 1]
                float  _Density;
                int    _RaymarchSteps;
                float  _JitterStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
            };

            // ------------------------------------------------------------------
            // Henyey-Greenstein phase function
            // ------------------------------------------------------------------
            float HenyeyGreenstein(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5));
            }

            // ------------------------------------------------------------------
            // Returns 1 if worldPos is inside the spotlight cone, else 0
            // ------------------------------------------------------------------
            float InsideCone(float3 worldPos)
            {
                float3 toPoint = worldPos - _LightPosWS;
                float  along   = dot(toPoint, _LightDirWS);
                if (along < 0.0 || along > _ConeRange) return 0.0;
                float3 proj    = toPoint - along * _LightDirWS;
                float  radius  = along * tan(acos(_ConeAngleCos));
                return length(proj) < radius ? 1.0 : 0.0;
            }

            // ------------------------------------------------------------------
            // Simple noise for raymarch jitter (avoids banding)
            // ------------------------------------------------------------------
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV  = IN.screenPos.xy / IN.screenPos.w;
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir    = normalize(IN.positionWS - rayOrigin);

                // Scene depth used per-sample to stop accumulating behind opaque surfaces,
                // but does NOT cut tEnd — we march the full proxy cone depth.
                float rawDepth   = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                float tStart = 0.001;
                float tEnd   = length(IN.positionWS - rayOrigin);

                if (tEnd <= tStart)
                    return half4(0, 0, 0, 0);

                float jitter   = Hash(screenUV * _ScreenParams.xy) * _JitterStrength;
                float stepSize = (tEnd - tStart) / (float)_RaymarchSteps;
                float t        = tStart + stepSize * jitter;

                float cosTheta = dot(rayDir, _LightDirWS);
                float phase    = HenyeyGreenstein(cosTheta, _MieG);

                float accumLight = 0.0;

                UNITY_LOOP
                for (int i = 0; i < _RaymarchSteps; i++)
                {
                    // Stop accumulating once we're behind opaque geometry
                    if (t > sceneDepth) break;

                    float3 samplePos = rayOrigin + rayDir * t;

                    if (InsideCone(samplePos) > 0.5)
                    {
                        float3 toSample = samplePos - _LightPosWS;
                        float  dist     = length(toSample);
                        float  atten    = saturate(1.0 - dist / _ConeRange);
                        atten *= atten;

                        float cosAngle = dot(normalize(toSample), _LightDirWS);
                        float edgeFade = saturate((cosAngle - _ConeAngleCos) / (1.0 - _ConeAngleCos));

                        accumLight += atten * edgeFade * phase * _Density * stepSize;
                    }

                    t += stepSize;
                    if (t >= tEnd) break;
                }

                accumLight = saturate(accumLight);
                half3 color = _LightColor.rgb * _LightIntensity * accumLight;
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Inside-view pass (camera inside cone)
        Pass
        {
            Name "VolumetricLight_CullFront"
            Blend One One
            ZWrite Off
            Cull Front
            ZTest Always

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _LightColor;
                float  _LightIntensity;
                float3 _LightPosWS;
                float3 _LightDirWS;
                float  _ConeAngleCos;
                float  _ConeRange;
                float  _MieG;
                float  _Density;
                int    _RaymarchSteps;
                float  _JitterStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
            };

            float HenyeyGreenstein(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5));
            }

            float InsideCone(float3 worldPos)
            {
                float3 toPoint = worldPos - _LightPosWS;
                float  along   = dot(toPoint, _LightDirWS);
                if (along < 0.0 || along > _ConeRange) return 0.0;
                float3 proj   = toPoint - along * _LightDirWS;
                float  radius = along * tan(acos(_ConeAngleCos));
                return length(proj) < radius ? 1.0 : 0.0;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV  = IN.screenPos.xy / IN.screenPos.w;
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir    = normalize(IN.positionWS - rayOrigin);

                float rawDepth   = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                float tStart = 0.001;
                float tEnd   = length(IN.positionWS - rayOrigin);

                if (tEnd <= tStart)
                    return half4(0, 0, 0, 0);

                float jitter   = Hash(screenUV * _ScreenParams.xy) * _JitterStrength;
                float stepSize = (tEnd - tStart) / (float)_RaymarchSteps;
                float t        = tStart + stepSize * jitter;

                float cosTheta = dot(rayDir, _LightDirWS);
                float phase    = HenyeyGreenstein(cosTheta, _MieG);

                float accumLight = 0.0;

                UNITY_LOOP
                for (int i = 0; i < _RaymarchSteps; i++)
                {
                    if (t > sceneDepth) break;

                    float3 samplePos = rayOrigin + rayDir * t;

                    if (InsideCone(samplePos) > 0.5)
                    {
                        float3 toSample = samplePos - _LightPosWS;
                        float  dist     = length(toSample);
                        float  atten    = saturate(1.0 - dist / _ConeRange);
                        atten *= atten;

                        float cosAngle = dot(normalize(toSample), _LightDirWS);
                        float edgeFade = saturate((cosAngle - _ConeAngleCos) / (1.0 - _ConeAngleCos));

                        accumLight += atten * edgeFade * phase * _Density * stepSize;
                    }

                    t += stepSize;
                    if (t >= tEnd) break;
                }

                accumLight = saturate(accumLight);
                half3 color = _LightColor.rgb * _LightIntensity * accumLight;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
