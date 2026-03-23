Shader "Custom/VolumetricFog"
{
    Properties
    {
        _MaxDistance("Max distance", float) = 100
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

            float _MaxDistance;

            half4 frag(Varyings IN) : SV_Target
            {
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos -  _WorldSpaceCameraPos;
                float viewLenght = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float distLimit = min(viewLenght, _MaxDistance);
                float distTravelled = 0;
                float transmittance = 0;
                
                return float4(frac(worldPos), 1);
            }
            ENDHLSL
        }
    }
}
