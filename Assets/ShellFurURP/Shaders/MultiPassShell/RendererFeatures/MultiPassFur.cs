using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using System.Reflection;

[DisallowMultipleRendererFeature("Multi-Pass Fur Forward")]
[Tooltip("Add this Renderer Feature to render fur in Forward path. (currently not rendering to GBuffer in Deferred)")]
public class MultiPassFur : ScriptableRendererFeature
{
    [System.Serializable]
    public class FilterSettings
    {
        public LayerMask LayerMask = 1;
        public string[] PassNames;

        public FilterSettings()
        {
            LayerMask = ~0;
            PassNames = new string[] { "UniversalForwardFur", "DepthOnlyFur", "DepthNormalsFur", "ShadowCasterFur", "UniversalGBufferFur" };
        }
    }

    [System.Serializable]
    public class PassSettings
    {
        [HideInInspector] public string passTag = "Fur ForwardLit";
        [Header("Keep It The Same For All")]
        [Tooltip("Controls the number of fur layers. Keep it the same in all Multi-Pass Fur Renderer Features.")]
        [Range(1, 200)]public int ShellAmount = 13;

        [Header("Advanced")]
        [Tooltip("Controls when to enqueue the fur rendering. (Before Rendering Opaques by default)")]
        [HideInInspector] public RenderPassEvent PassEvent = RenderPassEvent.BeforeRenderingOpaques;

        [HideInInspector] public FilterSettings filterSettings = new FilterSettings();
    }

    // C# Reflection
    private readonly static FieldInfo gBufferFieldInfo = typeof(UniversalRenderer).GetField("m_GBufferPass", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly int TotalLayer = Shader.PropertyToID("_TOTAL_LAYER");
    static readonly int CurrentLayer = Shader.PropertyToID("_CURRENT_LAYER");

    public class FurRenderPass : ScriptableRenderPass
    {
        string m_ProfilerTag;
        private PassSettings settings;
        public List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        private FilteringSettings filter;
        // Depth Priming needed.
        private RenderStateBlock m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

        public FurRenderPass(PassSettings setting, FilterSettings filterSettings)
        {
            m_ProfilerTag = setting.passTag;
            profilingSampler = new ProfilingSampler(m_ProfilerTag);
            string[] shaderTags = filterSettings.PassNames;
            this.settings = setting;

            RenderQueueRange queue = new RenderQueueRange();
            queue.lowerBound = 2000;
            queue.upperBound = 3000;
            filter = new FilteringSettings(queue, filterSettings.LayerMask);
            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
        }

        private class PassData
        {
            internal RendererListHandle[] rendererLists;
            internal int shellAmount;
        }

        private static bool UseDepthPriming(UniversalCameraData cameraData)
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_TVOS
            bool depthPrimingRecommended = false;
#else
            bool depthPrimingRecommended = true;
#endif

            var renderer = cameraData.renderer as UniversalRenderer;
            if (renderer == null)
                return false;

            return (depthPrimingRecommended && renderer.depthPrimingMode == DepthPrimingMode.Auto) ||
                renderer.depthPrimingMode == DepthPrimingMode.Forced;
        }

        private static RendererListHandle CreateRendererListWithRenderStateBlock(RenderGraph renderGraph, ref CullingResults cullResults, DrawingSettings drawingSettings, FilteringSettings filteringSettings, RenderStateBlock renderStateBlock)
        {
            var param = new RendererListParams(cullResults, drawingSettings, filteringSettings);
            var stateBlocks = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
            stateBlocks[0] = renderStateBlock;
            var tagValues = new NativeArray<ShaderTagId>(1, Allocator.Temp);
            tagValues[0] = ShaderTagId.none;
            param.stateBlocks = stateBlocks;
            param.tagValues = tagValues;
            param.isPassTagName = false;
            return renderGraph.CreateRendererList(param);
        }

        private void UpdateDepthPrimingState(UniversalCameraData cameraData, UniversalRenderingData renderingData)
        {
            bool useDepthPriming = UseDepthPriming(cameraData);
            bool isUsingDeferred = renderingData.renderingMode == RenderingMode.Deferred;

            if (useDepthPriming && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth) && !isUsingDeferred)
            {
                m_RenderStateBlock.depthState = new DepthState(false, CompareFunction.Equal);
                m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }
            else if (m_RenderStateBlock.depthState.compareFunction == CompareFunction.Equal)
            {
                m_RenderStateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
                m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }
        }

        // NOTE: Do NOT override Execute or OnCameraSetup in Unity 6+ when using RecordRenderGraph.
        // Removed Execute(...) which used context.DrawRenderers (old API) to avoid executing the same RendererList twice.

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            UpdateDepthPrimingState(cameraData, renderingData);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_ProfilerTag, out var passData, profilingSampler))
            {
                bool isMainCamera = cameraData.camera.CompareTag("MainCamera");

                int layersToRender = isMainCamera ? settings.ShellAmount : 1;

                passData.shellAmount = layersToRender;

                // Prevent render graph from culling this pass and allow global state modifications
                builder.AllowGlobalStateModification(true);

                SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList[0], renderingData, cameraData, lightData, sortingCriteria);

                // Allocate the array of renderer lists (one per layer)
                passData.rendererLists = new RendererListHandle[passData.shellAmount];

                // Create a separate renderer list handle for each shell layer so we can draw them individually
                for (int layer = 0; layer < passData.shellAmount; ++layer)
                {
                    // CreateRendererListWithRenderStateBlock returns a distinct handle each call
                    passData.rendererLists[layer] = CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filter, m_RenderStateBlock);

                    if (!passData.rendererLists[layer].IsValid())
                    {
                        // If a renderer list is invalid, fill remaining with invalid handles and continue (they won't be used)
                        for (int j = layer; j < passData.shellAmount; ++j)
                            passData.rendererLists[j] = new RendererListHandle();
                        break;
                    }

                    // Tell render graph we'll use each renderer list
                    builder.UseRendererList(passData.rendererLists[layer]);
                }

                // Set render attachments
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

                // declare other resources (shadows, dbuffers, ssao) as before
                TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;
                TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);

                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                TextureHandle[] dBufferHandles = resourceData.dBuffer;
                for (int i = 0; i < dBufferHandles.Length; ++i)
                {
                    TextureHandle dBuffer = dBufferHandles[i];
                    if (dBuffer.IsValid())
                        builder.UseTexture(dBuffer, AccessFlags.Read);
                }

                TextureHandle ssaoTexture = resourceData.ssaoTexture;
                if (ssaoTexture.IsValid())
                    builder.UseTexture(ssaoTexture, AccessFlags.Read);

                if (cameraData.xr.enabled)
                {
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                }

                // Render func: draw each (distinct) renderer list once while changing the layer global
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalFloat(TotalLayer, data.shellAmount);
                    for (int i = 0; i < data.shellAmount; i++)
                    {
                        // if invalid handle, skip
                        if (!data.rendererLists[i].IsValid())
                            continue;

                        context.cmd.SetGlobalFloat(CurrentLayer, i);
                        context.cmd.DrawRendererList(data.rendererLists[i]);
                    }
                });
            }
        }
    }

    public PassSettings settings = new PassSettings();
    FurRenderPass m_FurRenderPass;

    public override void Create()
    {
        FilterSettings filter = settings.filterSettings;
        m_FurRenderPass = new FurRenderPass(settings, filter);
        m_FurRenderPass.renderPassEvent = settings.PassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_FurRenderPass);
    }
}
