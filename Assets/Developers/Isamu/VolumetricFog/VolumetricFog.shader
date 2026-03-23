Shader "Custom/VolumetricFog"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MaxDistance("Max distance", float) = 100
        _StepSize("Step size", Range(0.1, 20)) = 1
        _DensityMultiplier("Density multiplier", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float _MaxDistance;
            float _DensityMultiplier;
            float _StepSize;

            float get_density()
            {
                return _DensityMultiplier;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos -  _WorldSpaceCameraPos;
                float viewLenght = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float distLimit = min(viewLenght, _MaxDistance);
                float distTravelled = 0;
                float transmittance = 0;

                while (distTravelled < distLimit)
                {
                    float density = get_density();
                    if (density > 0)
                    {
                        transmittance += density * _StepSize;
                    }
                    distTravelled += _StepSize;
                }
                
                return lerp(col, _Color, saturate(transmittance));
            }
            ENDHLSL
        }
    }
}
