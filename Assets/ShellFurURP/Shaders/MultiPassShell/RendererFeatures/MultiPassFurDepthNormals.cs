using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using System.Reflection;

[DisallowMultipleRendererFeature("Multi-Pass Fur DepthNormals")]
[Tooltip("Add this Renderer Feature to automatically support DepthNormalPrepass for Multi-Pass Fur.")]
public class MultiPassFurDepthNormals : ScriptableRendererFeature
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

    public enum DecalMode
    {
        [InspectorName("None")]
        Invalid,
        Automatic,
        [InspectorName("DBuffer")]
        DBuffer,
        ScreenSpace,
        //GBuffer, // Multi-Pass Fur does not render to GBuffer by now.
    };

    [System.Serializable]
    public class PassSettings
    {
        [HideInInspector] public string passTag = "Fur DepthNormals";
        [Header("Keep It The Same For All")]
        [Tooltip("Controls the number of fur layers. Keep it the same in all Multi-Pass Fur Renderer Features.")]
        // Increase the range if you need more layers.
        [Range(1, 200)]public int ShellAmount = 13;

        [Header("Advanced")]
        [Tooltip("Please specify the current Decal Technique if enabling Decal Renderer Feature. Has no effect when Decal is disabled.")]
        public DecalMode decalMode = DecalMode.Invalid;

        // Remove the "[HideInInspector]" if you want to change the RenderPassEvent.
        [Tooltip("Controls when to enqueue the fur DepthNormalPrepass rendering. (After Rendering Pre Passes by default)")]
        [HideInInspector] public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingPrePasses;

        [HideInInspector] public FilterSettings filterSettings = new FilterSettings();
    }

    // C# Reflection
    private readonly static FieldInfo gBufferFieldInfo = typeof(UniversalRenderer).GetField("m_GBufferPass", BindingFlags.NonPublic | BindingFlags.Instance);
    private readonly static FieldInfo activeRenderPassQueueFieldInfo = typeof(ScriptableRenderer).GetField("m_ActiveRenderPassQueue", BindingFlags.NonPublic | BindingFlags.Instance);
    private readonly static FieldInfo activeRendererFeatureFieldInfo = typeof(ScriptableRenderer).GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
    // For setting RenderTarget use. (unnecessary in URP 12 and below)
#if UNITY_2022_1_OR_NEWER
    private readonly static FieldInfo normalsTextureFieldInfo = typeof(UniversalRenderer).GetField("m_NormalsTexture", BindingFlags.NonPublic | BindingFlags.Instance);
    private readonly static FieldInfo depthTextureFieldInfo = typeof(UniversalRenderer).GetField("m_DepthTexture", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
    static readonly int TotalLayer = Shader.PropertyToID("_TOTAL_LAYER");
    static readonly int CurrentLayer = Shader.PropertyToID("_CURRENT_LAYER");
    // From "DecalRendererFeature.cs".
    public bool IsAutomaticDBuffer()
    {
        // As WebGL uses gles here we should not use DBuffer
#if UNITY_EDITOR
        if (UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup == UnityEditor.BuildTargetGroup.WebGL)
            return false;
#else
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            return false;
#endif
        return !GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.SHADER_API_MOBILE);
    }

    public class FurRenderPass : ScriptableRenderPass
    {
        string m_ProfilerTag;
        private PassSettings settings;
        public List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        private FilteringSettings filter;

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

        private static TextureHandle GetDepthTarget(UniversalResourceData resourceData, UniversalCameraData cameraData)
        {
            TextureHandle depthTarget = resourceData.cameraDepthTexture;
            if (UseDepthPriming(cameraData) && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth))
                depthTarget = resourceData.activeDepthTexture;

            if (!depthTarget.IsValid())
                depthTarget = resourceData.activeDepthTexture;

            return depthTarget;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            TextureHandle depthTarget = GetDepthTarget(resourceData, cameraData);
            TextureHandle normalsTexture = resourceData.cameraNormalsTexture;
            if (!depthTarget.IsValid() || !normalsTexture.IsValid())
                return;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_ProfilerTag, out var passData, profilingSampler))
            {
                bool isMainCamera = cameraData.camera.CompareTag("MainCamera");

                int layersToRender = isMainCamera ? settings.ShellAmount : 1;

                passData.shellAmount = layersToRender;

                SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList[2], renderingData, cameraData, lightData, sortingCriteria);
                drawingSettings.perObjectData = PerObjectData.None;

                passData.rendererLists = new RendererListHandle[passData.shellAmount];

                var param = new RendererListParams(renderingData.cullResults, drawingSettings, filter);

                for (int layer = 0; layer < passData.shellAmount; ++layer)
                {
                    passData.rendererLists[layer] = renderGraph.CreateRendererList(param);

                    if (!passData.rendererLists[layer].IsValid())
                    {
                        for (int j = layer; j < passData.shellAmount; ++j)
                            passData.rendererLists[j] = new RendererListHandle();
                        break;
                    }

                    builder.UseRendererList(passData.rendererLists[layer]);
                }

                builder.SetRenderAttachment(normalsTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.ReadWrite);
                builder.AllowGlobalStateModification(true);
                if (cameraData.xr.enabled)
                {
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                }

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalFloat(TotalLayer, data.shellAmount);
                    for (int i = 0; i < data.shellAmount; i++)
                    {
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
        // No need to enqueue DepthNormals pass when in deferred path.
        // If GBuffer exists, URP is in deferred path.
        bool isUsingDeferred = gBufferFieldInfo.GetValue(renderer) != null;

        // C# Reflection
        var activeRenderPassQueue = activeRenderPassQueueFieldInfo.GetValue(renderer) as List<ScriptableRenderPass>;
        bool rendererFeatureNeedsNormals = false;
        for (int i = 0; i < activeRenderPassQueue.Count; ++i)
        {
            ScriptableRenderPass pass = activeRenderPassQueue[i];
            rendererFeatureNeedsNormals |= (pass.input & ScriptableRenderPassInput.Normal) != ScriptableRenderPassInput.None;
            // Keep this line for reference when adding fur motionVector pass.
            //rendererFeatureNeedsMotion |= (pass.input & ScriptableRenderPassInput.Motion) != ScriptableRenderPassInput.None;
        }

        // Decal Renderer Feature is not a Render Pass, and it does not have a public method to return what it needs for rendering. (e.g. DepthNormal required?)
        // 
        // If Decal Renderer Feature (DBuffer mode) enabled, enqueue the Fur DepthNormalPrepass.
        // C# Reflection
        var activeRendererFeatures = activeRendererFeatureFieldInfo.GetValue(renderer) as List<ScriptableRendererFeature>;
        for (int i = 0; i < activeRendererFeatures.Count; ++i)
        {
            ScriptableRendererFeature feature = activeRendererFeatures[i];
            // Get the Decal Renderer Feature mode, if it exists.
            if (feature.isActive && feature.name == "DecalRendererFeature")
            {
                // How can we automatically get the current Decal Renderer Feature mode?

                //bool decalNeedsNormals = DBuffer : ScreenSpace?;
                //rendererFeatureNeedsNormals |= decalNeedsNormals;

                // Need to enqueue a DepthNormalPrepass for fur when using DBuffer Decal.
                if (settings.decalMode == DecalMode.DBuffer || (settings.decalMode == DecalMode.Automatic) && IsAutomaticDBuffer())
                {
                    rendererFeatureNeedsNormals |= true;
                }

            }
        }

        // When should we enqueue DepthNormalPrepass pass:
        // 1. Any Renderer Feature requires DepthNormals. (such as SSAO using "DepthNormals" source)
        // 
        // When shouldn't we enqueue DepthNormalPrepass pass:
        // 1. We are in deferred path. (GBuffer pass will output normal)

        // Enqueue this pass if requires DepthNormals.
        if (rendererFeatureNeedsNormals && !isUsingDeferred)
        {
            renderer.EnqueuePass(m_FurRenderPass);
        }
    }
}
