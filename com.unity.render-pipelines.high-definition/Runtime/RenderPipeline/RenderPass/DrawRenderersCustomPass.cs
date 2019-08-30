using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// DrawRenderers Custom Pass
    /// </summary>
    [System.Serializable]
    public class DrawRenderersCustomPass : CustomPass
    {
        // Used only for the UI to keep track of the toggle state
        public bool filterFoldout;
        public bool rendererFoldout;

        //Filter settings
        public CustomPassRenderQueueType renderQueueType = CustomPassRenderQueueType.AllOpaque;
        public string[] passNames = new string[1] { "Forward" };
        public LayerMask layerMask = -1;
        public SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;

        // Override material
        public Material overrideMaterial = null;
        public int overrideMaterialPassIndex = 0;
        
        static List<ShaderTagId> m_HDRPShaderTags;
        static List<ShaderTagId> hdrpShaderTags
        {
            get
            {
                if (m_HDRPShaderTags == null)
                {
                    m_HDRPShaderTags = new List<ShaderTagId>() {
                        HDShaderPassNames.s_ForwardOnlyName,        // HD Unlit shader
                        HDShaderPassNames.s_SRPDefaultUnlitName,    // Cross SRP Unlit shader
                    };
                }
                return m_HDRPShaderTags;
            }
        }

        /// <summary>
        /// Execute the DrawRenderers with parameters setup from the editor
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="cmd"></param>
        /// <param name="camera"></param>
        /// <param name="cullingResult"></param>
        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            ShaderTagId[] shaderPasses = new ShaderTagId[hdrpShaderTags.Count + ((overrideMaterial != null) ? 1 : 0)];
            System.Array.Copy(hdrpShaderTags.ToArray(), shaderPasses, hdrpShaderTags.Count);
            if (overrideMaterial != null)
            {
                shaderPasses[hdrpShaderTags.Count] = new ShaderTagId(overrideMaterial.GetPassName(overrideMaterialPassIndex));
            }

            if (shaderPasses.Length == 0)
            {
                Debug.LogWarning("Attempt to call DrawRenderers with an empty shader passes. Skipping the call to avoid errors");
                return;
            }

            var result = new RendererListDesc(shaderPasses, cullingResult, hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = GetRenderQueueRange(renderQueueType),
                sortingCriteria = sortingCriteria,
                excludeObjectMotionVectors = true,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassIndex = overrideMaterialPassIndex,
                layerMask = layerMask,
            };

            HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
        }

        /// <summary>
        /// Returns the render queue range associated with the custom render queue type
        /// </summary>
        /// <returns></returns>
        protected RenderQueueRange GetRenderQueueRange(CustomPassRenderQueueType type)
        {
            switch (type)
            {
                case CustomPassRenderQueueType.OpaqueNoAlphaTest: return HDRenderQueue.k_RenderQueue_OpaqueNoAlphaTest;
                case CustomPassRenderQueueType.OpaqueAlphaTest: return HDRenderQueue.k_RenderQueue_OpaqueAlphaTest;
                case CustomPassRenderQueueType.AllOpaque: return HDRenderQueue.k_RenderQueue_AllOpaque;
                case CustomPassRenderQueueType.AfterPostProcessOpaque: return HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque;
                case CustomPassRenderQueueType.PreRefraction: return HDRenderQueue.k_RenderQueue_PreRefraction;
                case CustomPassRenderQueueType.Transparent: return HDRenderQueue.k_RenderQueue_Transparent;
                case CustomPassRenderQueueType.LowTransparent: return HDRenderQueue.k_RenderQueue_LowTransparent;
                case CustomPassRenderQueueType.AllTransparent: return HDRenderQueue.k_RenderQueue_AllTransparent;
                case CustomPassRenderQueueType.AllTransparentWithLowRes: return HDRenderQueue.k_RenderQueue_AllTransparentWithLowRes;
                case CustomPassRenderQueueType.AfterPostProcessTransparent: return HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent;
                case CustomPassRenderQueueType.All:
                default:
                    return HDRenderQueue.k_RenderQueue_All;
            }
        }
    }
}