using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Resonance
{
    public class VolumetricLightFeature : ScriptableRendererFeature
    {
        class DepthPrepassEnsurePass : ScriptableRenderPass
        {
            public DepthPrepassEnsurePass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                requiresIntermediateTexture = false;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           "VolumetricLight Depth Prepass Ensure", out _))
                {
                    if (resourceData.cameraDepthTexture.IsValid())
                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (PassData _, RasterGraphContext _) => { });
                }
            }

            class PassData { }
        }

        private DepthPrepassEnsurePass _pass;

        public override void Create()
        {
            _pass = new DepthPrepassEnsurePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Reflection) return;
            renderer.EnqueuePass(_pass);
        }
    }
}
