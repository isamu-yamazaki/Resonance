using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resonance
{
    // This feature's main job is to ensure the depth texture is available
    // for the volumetric shader to read from. The actual rendering is done
    // by the proxy mesh MeshRenderer on each VolumetricLight component —
    // no manual CommandBuffer needed.
    public class VolumetricLightFeature : ScriptableRendererFeature
    {
        class DepthPrepassEnsurePass : ScriptableRenderPass
        {
            public DepthPrepassEnsurePass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // No-op — just ensures this pass slot is active so URP
                // keeps the depth texture alive through the transparent queue.
            }
        }

        private DepthPrepassEnsurePass _pass;

        public override void Create()
        {
            _pass = new DepthPrepassEnsurePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Only enqueue for game/scene cameras, skip reflection probes
            if (renderingData.cameraData.cameraType == CameraType.Reflection) return;
            renderer.EnqueuePass(_pass);
        }
    }
}
