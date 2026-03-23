Shader "Custom/VolumetricFog"
{
    Properties
    { }

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

            half4 frag(Varyings IN) : SV_Target
            {
                float depth = SampleSceneDepth(IN.texcoord);
                return frac(LinearEyeDepth(depth, _ZBufferParams));
            }
            ENDHLSL
        }
    }
}
