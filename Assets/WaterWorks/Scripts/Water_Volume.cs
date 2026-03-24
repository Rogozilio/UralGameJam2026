using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// URP 17 migration: feature uses RTHandle + Blitter API.
public class Water_Volume : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private RTHandle _source;
        private RTHandle _tempColor;
        private readonly Material _material;

        public CustomRenderPass(Material mat)
        {
            _material = mat;
        }

        public void Setup(RTHandle source)
        {
            _source = source;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Reflection)
                return;

            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref _tempColor,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_TemporaryColourTexture");
        }

#pragma warning disable 618, 672
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Reflection)
                return;

            if (_material == null || _source == null || _tempColor == null)
                return;

            CommandBuffer commandBuffer = CommandBufferPool.Get("Water_Volume");

            Blitter.BlitCameraTexture(commandBuffer, _source, _tempColor, _material, 0);
            Blitter.BlitCameraTexture(commandBuffer, _tempColor, _source);

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            CommandBufferPool.Release(commandBuffer);
        }
#pragma warning restore 618, 672

        public void Dispose()
        {
            _tempColor?.Release();
            _tempColor = null;
        }
    }

    [System.Serializable]
    public class _Settings
    {
        public Material material = null;
        public RenderPassEvent renderPass = RenderPassEvent.AfterRenderingSkybox;
    }

    public _Settings settings = new _Settings();

    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        if (settings.material == null)
            settings.material = (Material)Resources.Load("Water_Volume");

        m_ScriptablePass = new CustomRenderPass(settings.material)
        {
            renderPassEvent = settings.renderPass
        };
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (m_ScriptablePass == null)
            return;

        m_ScriptablePass.Setup(renderer.cameraColorTargetHandle);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_ScriptablePass == null || settings.material == null)
            return;

        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Dispose();
    }
}
