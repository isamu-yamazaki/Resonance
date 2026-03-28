Shader "Resonance/SonarPulse"
{
    Properties
    {
        _PulseTime      ("Pulse Time (0-1)", Range(0, 1)) = 0.0
        _DiscOrigin     ("Disc Origin (World)", Vector) = (0, 0, 0, 0)
        _DiscForward    ("Disc Forward (World)", Vector) = (0, 0, 1, 0)
        _CurrentRadius  ("Current Radius", Float) = 0.0
        _RingColor      ("Ring Color", Color) = (0.4, 0.8, 1.0, 1.0)
        _RingEmission   ("Ring Emission", Float) = 4.0
        _RingWidth      ("Ring Width", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent+1"
        }

        Cull Front
        ZWrite Off
        ZTest Always
        Blend SrcAlpha One

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _PulseTime;
                float4 _DiscOrigin;
                float4 _DiscForward;
                float  _CurrentRadius;
                float4 _RingColor;
                float  _RingEmission;
                float  _RingWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 positionSS  : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = posInputs.positionCS;
                output.positionSS  = ComputeScreenPos(posInputs.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV  = input.positionSS.xy / input.positionSS.w;
                float sceneDepth = SampleSceneDepth(screenUV);
                float3 worldPos  = ComputeWorldSpacePosition(screenUV, sceneDepth, UNITY_MATRIX_I_VP);

                // Cull fragments behind the wall the disc is attached to
                float3 toFragment = worldPos - _DiscOrigin.xyz;
                float forwardDot  = dot(normalize(toFragment), _DiscForward.xyz);
                clip(forwardDot);

                // Distance from disc origin to the reconstructed world position
                float distFromOrigin = distance(worldPos, _DiscOrigin.xyz);

                // Only render within a thin band at the current pulse radius
                float distToRing = abs(distFromOrigin - _CurrentRadius);
                float ringMask   = 1.0 - saturate(distToRing / max(_RingWidth, 0.001));
                ringMask = pow(ringMask, 2.0);

                clip(ringMask - 0.01);

                float fade = smoothstep(1.0, 0.7, _PulseTime);

                half3 color = _RingColor.rgb * _RingEmission;
                float alpha = ringMask * fade;

                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
