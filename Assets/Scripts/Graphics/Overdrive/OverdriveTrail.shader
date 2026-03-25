Shader "Resonance/OverdriveTrail"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.8
        _Smoothness ("Smoothness", Range(0,1)) = 0.9
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);
                
                OUT.positionCS = positionInputs.positionCS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                OUT.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Normalize inputs
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);
                
                // Fresnel effect for chrome look
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 3.0);
                
                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDir));
                
                // Simple metallic/specular
                float3 reflectDir = reflect(-viewDirWS, normalWS);
                float spec = pow(saturate(dot(reflectDir, lightDir)), 32.0 * _Smoothness);
                
                // Combine effects
                float3 finalColor = _Color.rgb;
                finalColor += fresnel * _Color.rgb * 0.5;
                finalColor += spec * _Metallic;
                finalColor *= (NdotL * 0.5 + 0.5); // Wrap lighting
                
                half4 color = half4(finalColor, _Color.a);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Unlit"
}