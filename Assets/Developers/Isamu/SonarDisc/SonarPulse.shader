Shader "Resonance/SonarPulse"
{
    Properties
    {
        _PulseTime      ("Pulse Time (0-1)", Range(0, 1)) = 0.0
        _RingColor      ("Ring Color", Color) = (0.4, 0.8, 1.0, 1.0)
        _RingEmission   ("Ring Emission", Float) = 4.0
        _RingWidth      ("Ring Width", Float) = 0.04
        _FadeEdge       ("Fade Edge", Float) = 3.0
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
                float  _PulseTime;
                float4 _RingColor;
                float  _RingEmission;
                float  _RingWidth;
                float  _FadeEdge;
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
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs    = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.normalWS    = normalInputs.normalWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir  = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Rim mask — thin band at the silhouette edge of the sphere
                float rim      = 1.0 - saturate(dot(normalWS, viewDir));
                float ringMask = pow(rim, 1.0 / max(_RingWidth, 0.001));

                // Fade out as pulse expands toward end
                float fade = smoothstep(1.0, 0.6, _PulseTime);

                half3 color = _RingColor.rgb * _RingEmission;
                float alpha = ringMask * fade;

                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
