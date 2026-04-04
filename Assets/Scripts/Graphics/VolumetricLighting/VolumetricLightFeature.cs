using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resonance
{
    // Ensures the depth texture stays alive through the transparent queue for VolumetricLight
    public class VolumetricLightFeature : ScriptableRendererFeature
    {
        class DepthPrepassEnsurePass : ScriptableRenderPass
        {
            public DepthPrepassEnsurePass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
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
