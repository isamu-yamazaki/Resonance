Shader "Resonance/SonarReveal"
{
    Properties
    {
        _OutlineColor   ("Outline Color", Color) = (1.0, 0.0, 0.8, 1.0)
        _OutlineWidth   ("Outline Width", Float) = 0.03
        _EmissionStrength ("Emission Strength", Float) = 3.0
        _RevealTime     ("Reveal Time (0-1)", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent+10"
        }

        Cull Back
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

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _EmissionStrength;
                float  _RevealTime;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
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

                // Expand shell outward along normal
                float3 posOS = input.positionOS.xyz + input.normalOS * _OutlineWidth;

                VertexPositionInputs posInputs    = GetVertexPositionInputs(posOS);
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

                // Rim mask for edge glow
                float rim     = 1.0 - saturate(dot(normalWS, viewDir));
                float rimMask = pow(rim, 2.0);

                // Flash in on reveal, then settle to rim glow
                float flash = smoothstep(0.0, 0.1, _RevealTime) * smoothstep(0.3, 0.15, _RevealTime);
                float fade  = smoothstep(1.0, 0.8, _RevealTime);

                half3 color = _OutlineColor.rgb * _EmissionStrength;
                float alpha = (rimMask + flash * 0.5) * fade;

                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
