using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// List all the injection points available for HDRP
    /// </summary>
    public enum CustomPassInjectionPoint
    {
        BeforeRendering,
        BeforeTransparent,
        BeforePostProcess,
        AfterPostProcess,
    }

    /// <summary>
    /// Used to select the target buffer when executing the custom pass
    /// </summary>
    public enum CustomPassTargetBuffer
    {
        Camera,
        Custom,
    }
    
    /// <summary>
    /// Render Queue filters for the DrawRenderers custom pass 
    /// </summary>
    public enum CustomPassRenderQueueType
    {
        OpaqueNoAlphaTest,
        OpaqueAlphaTest,
        AllOpaque,
        AfterPostProcessOpaque,
        PreRefraction,
        Transparent,
        LowTransparent,
        AllTransparent,
        AllTransparentWithLowRes,
        AfterPostProcessTransparent,
        All,
    }

    /// <summary>
    /// Class that holds data and logic for the pass to be executed
    /// </summary>
    [System.Serializable]
    public abstract class CustomPass
    {
        public string                   name = "Custom Pass";
        public bool                     enabled = true;
        public CustomPassTargetBuffer   targetColorBuffer;
        public CustomPassTargetBuffer   targetDepthBuffer;
        public ClearFlag                clearFlags;
        public bool                     passFoldout;

        [System.NonSerialized]
        bool    isSetup = false;

        internal void ExecuteInternal(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult, RTHandle cameraColorBuffer, RTHandle cameraDepthBuffer, RTHandle customColorBuffer, RTHandle customDepthBuffer)
        {
            if (!isSetup)
            {
                Setup(renderContext, cmd);
                isSetup = true;
            }

            SetCustomPassTarget(cmd, cameraColorBuffer, cameraDepthBuffer, customColorBuffer, customDepthBuffer);

            Execute(renderContext, cmd, camera, cullingResult);
            
            // Set back the camera color buffer is we were using a custom buffer as target
            if (targetDepthBuffer != CustomPassTargetBuffer.Camera)
                CoreUtils.SetRenderTarget(cmd, cameraColorBuffer);
        }

        internal void CleanupPassInternal() => Cleanup();

        void SetCustomPassTarget(CommandBuffer cmd, RTHandle cameraColorBuffer, RTHandle cameraDepthBuffer, RTHandle customColorBuffer, RTHandle customDepthBuffer)
        {
            RTHandle colorBuffer = (targetColorBuffer == CustomPassTargetBuffer.Custom) ? customColorBuffer : cameraColorBuffer;
            RTHandle depthBuffer = (targetDepthBuffer == CustomPassTargetBuffer.Custom) ? customDepthBuffer : cameraDepthBuffer;
            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlags);
        }

        /// <summary>
        /// Called when your pass needs to be executed by a camera
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="cmd"></param>
        /// <param name="camera"></param>
        /// <param name="cullingResult"></param>
        protected abstract void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult);

        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="cmd"></param>
        protected virtual void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {}

        /// <summary>
        /// Called when HDRP is destroyed.
        /// Allow you to free custom buffers.
        /// </summary>
        protected virtual void Cleanup() {}
        
        /// <summary>
        /// Create a custom pass to execute a fullscreen pass
        /// </summary>
        /// <param name="fullScreenMaterial"></param>
        /// <param name="targetColorBuffer"></param>
        /// <param name="targetDepthBuffer"></param>
        /// <returns></returns>
        public static CustomPass CreateFullScreenPass(Material fullScreenMaterial, CustomPassTargetBuffer targetColorBuffer = CustomPassTargetBuffer.Camera,
            CustomPassTargetBuffer targetDepthBuffer = CustomPassTargetBuffer.Camera)
        {
            return new FullScreenCustomPass()
            {
                name = "FullScreen Pass",
                targetColorBuffer = targetColorBuffer,
                targetDepthBuffer = targetDepthBuffer,
                fullscreenPassMaterial = fullScreenMaterial,
            };
        }

        /// <summary>
        /// Create a Custom Pass to render objects
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="mask"></param>
        /// <param name="overrideMaterial"></param>
        /// <param name="overrideMaterialPassIndex"></param>
        /// <param name="sorting"></param>
        /// <param name="clearFlags"></param>
        /// <param name="targetColorBuffer"></param>
        /// <param name="targetDepthBuffer"></param>
        /// <returns></returns>
        public static CustomPass CreateDrawRenderersPass(CustomPassRenderQueueType queue, LayerMask mask,
            Material overrideMaterial, int overrideMaterialPassIndex = 0, SortingCriteria sorting = SortingCriteria.CommonOpaque,
            ClearFlag clearFlags = ClearFlag.None, CustomPassTargetBuffer targetColorBuffer = CustomPassTargetBuffer.Camera,
            CustomPassTargetBuffer targetDepthBuffer = CustomPassTargetBuffer.Camera)
        {
            return new DrawRenderersCustomPass()
            {
                name = "DrawRenderers Pass",
                renderQueueType = queue,
                layerMask = mask,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassIndex = overrideMaterialPassIndex,
                sortingCriteria = sorting,
                clearFlags = clearFlags,
                targetColorBuffer = targetColorBuffer,
                targetDepthBuffer = targetDepthBuffer,
            };
        }

    }
}
