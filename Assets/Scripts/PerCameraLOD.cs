using UnityEngine;
using UnityEngine.Rendering;

public class PerCameraLOD : MonoBehaviour {
    float customLODBias = 0.5f;
    float customLODThreshold = 11;
    float originalBias;
    float originalThreshold;

    void Start()
    {
        originalBias = QualitySettings.lodBias;
        originalThreshold = QualitySettings.meshLodThreshold;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) {
        if (camera == Camera.main) return;
        QualitySettings.lodBias = customLODBias;
        QualitySettings.meshLodThreshold = customLODThreshold;
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera) {
        if (camera == Camera.main) return;
        QualitySettings.lodBias = originalBias;
        QualitySettings.meshLodThreshold = originalThreshold;
    }

    void OnDestroy() {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }
}