#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using TMPro;
using Unity.Jobs;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Trace and warm up pipeline state objects (PSOs) in a GraphicsStateCollection object.
public class GraphicsStateCollectionManager : MonoBehaviour
{
    public enum Mode
    {
        Tracing,
        WarmUp,
        WarmUpCacheMissTrace
    };
    public Mode mode;

    // Create a singleton so Unity uses the script only once across all scenes.
    public static GraphicsStateCollectionManager Instance;

    public Image loadingBar;
    public TextMeshProUGUI loadingText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    // Set up the collection of PSOs, and set where to store the files in the project folder.
    public UnityEngine.Rendering.GraphicsStateCollection[] collections;
    private const string k_CollectionFolderPath = "GraphicsStateCollections/";

    // Create internal variables for the traced PSOs, and the file to output.
    private string m_OutputCollectionName;
    private UnityEngine.Rendering.GraphicsStateCollection m_GraphicsStateCollection;


    #if UNITY_EDITOR

    // Right click on the component to update the collection files list.
    [ContextMenu("Update collection list")]
    public void UpdateCollectionList()
    {
        string[] collectionGUIDs = AssetDatabase.FindAssets("t:GraphicsStateCollection", new[] {"Assets/" + k_CollectionFolderPath});
        collections = new UnityEngine.Rendering.GraphicsStateCollection[collectionGUIDs.Length];
        for (int i = 0; i < collections.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(collectionGUIDs[i]);
            collections[i] = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.GraphicsStateCollection>(path);
        }
        EditorUtility.SetDirty(this);
    }

    #endif

    // Find the available collection file that matches the current platform and quality level.
    private UnityEngine.Rendering.GraphicsStateCollection FindExistingCollection()
    {
        for (int i = 0; i < collections.Length; i++)
        {
            if (collections[i] != null)
            {
                if (collections[i].runtimePlatform == Application.platform &&
                    collections[i].graphicsDeviceType == SystemInfo.graphicsDeviceType &&
                    collections[i].qualityLevelName == QualitySettings.names[QualitySettings.GetQualityLevel()])
                {
                    return collections[i];
                }
            }
        }

        return null;
    }

    void Awake()
    {
        // Ensure there's only one instance of GraphicsStateCollectionManager.
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one instance of GraphicsStateCollectionManager is allowed!");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        switch (mode)
        {
            case Mode.Tracing:
                // Find the existing collection file based on current settings.
                m_GraphicsStateCollection = FindExistingCollection();

                if (m_GraphicsStateCollection != null)
                {
                    // Use the existing file path if found.
                    m_OutputCollectionName = k_CollectionFolderPath + m_GraphicsStateCollection.name;
                }
                else
                {
                    // Create a new file if the file isn't found.

                    // Get the name of the current quality level.
                    int qualityLevelIndex = QualitySettings.GetQualityLevel();
                    string qualityLevelName = QualitySettings.names[qualityLevelIndex];
                    qualityLevelName = qualityLevelName.Replace(" ", "");

                    // Set up the file path to use for the output collection.
                    m_OutputCollectionName = string.Concat(k_CollectionFolderPath, "GfxState_", Application.platform, "_", SystemInfo.graphicsDeviceType.ToString(), "_", qualityLevelName);

                    // Create a new GraphicsStateCollection.
                    m_GraphicsStateCollection = new UnityEngine.Rendering.GraphicsStateCollection();
                }

                // Start tracing PSOs.
                Debug.Log("Tracing started for GraphicsStateCollection'" + m_OutputCollectionName + "'.");
                m_GraphicsStateCollection.BeginTrace();
                SceneManager.LoadScene(1);
                break;
            case Mode.WarmUp:
                // Find the existing collection file based on current settings.
                m_GraphicsStateCollection = FindExistingCollection();

                // Warm up the PSOs.
                if (m_GraphicsStateCollection != null && !m_GraphicsStateCollection.isWarmedUp)
                {
                    Debug.Log("Started warming up " + m_GraphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
                    StartCoroutine(WarmCollectionAsync(m_GraphicsStateCollection));
                }
                else
                {
                    SceneManager.LoadScene(1);
                }
                break;
            case Mode.WarmUpCacheMissTrace:
                m_GraphicsStateCollection = FindExistingCollection();
                if (m_GraphicsStateCollection != null && !m_GraphicsStateCollection.isWarmedUp)
                {
                    m_OutputCollectionName = k_CollectionFolderPath + m_GraphicsStateCollection.name;
                    Debug.Log("Started warming up " + m_GraphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries and tracing cache misses.");
                    StartCoroutine(WarmCollectionAsync(m_GraphicsStateCollection, true));
                }
                else
                {
                    SceneManager.LoadScene(1);
                }
                break;
            default:
                SceneManager.LoadScene(1);
                break;
        }
    }

    IEnumerator WarmCollectionAsync(UnityEngine.Rendering.GraphicsStateCollection collection, bool traceMisses = false)
    {
        int totalCount = collection.totalGraphicsStateCount > 0 ? collection.totalGraphicsStateCount : collection.variantCount;
        loadingText.color = new Color(1, 1, 1, 1);
        while (!collection.isWarmedUp)
        {
            float progress = (float)collection.completedWarmupCount / totalCount;
            loadingBar.fillAmount = progress;
            yield return null;
            JobHandle jobHandle = collection.WarmUpProgressively(1, default(JobHandle), traceMisses);
            yield return new WaitUntil(() => jobHandle.IsCompleted);
        }
        loadingBar.fillAmount = 1f;
        Debug.Log("Finished warming up " + totalCount + " GraphicsState entries.");
        float fadeStartTime = Time.time;
        while (Time.time < fadeStartTime + 0.5f)
        {
            float fadeProgress = (Time.time - fadeStartTime) / 0.5f;
            loadingText.color = new Color(1 - fadeProgress, 1 - fadeProgress, 1 - fadeProgress, 1 - fadeProgress);
            loadingBar.color = new Color(1 - fadeProgress, 1 - fadeProgress, 1 - fadeProgress, 1 - fadeProgress);
            yield return null;
        }

        yield return null;
        SceneManager.LoadScene(1);
    }

    // For mobile platforms, data is additionally saved when focus is lost as OnDestroy() is not guaranteed to be called.
    void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            if (mode == Mode.Tracing && m_GraphicsStateCollection != null)
            {
                Debug.Log("Focus changed. Sending collection to Editor with " + m_GraphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
                m_GraphicsStateCollection.SendToEditor(m_OutputCollectionName);
            }
            if (mode == Mode.WarmUpCacheMissTrace && m_GraphicsStateCollection != null)
            {
                Debug.Log("Focus changed. Sending collection to Editor with " + m_GraphicsStateCollection.cacheMissCollection.totalGraphicsStateCount + " GraphicsState entries.");
                m_GraphicsStateCollection.cacheMissCollection.SendToEditor(m_OutputCollectionName + "_CacheMisses");
            }
        }
    }

    void OnDestroy()
    {
        if (mode == Mode.Tracing && m_GraphicsStateCollection != null)
        {
            m_GraphicsStateCollection.EndTrace();
            Debug.Log("Sending collection to Editor with " + m_GraphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
            m_GraphicsStateCollection.SendToEditor(m_OutputCollectionName);
        }
        if (mode == Mode.WarmUpCacheMissTrace && m_GraphicsStateCollection != null)
        {
            Debug.Log("Sending cache miss collection to Editor with " + m_GraphicsStateCollection.cacheMissCollection.totalGraphicsStateCount + " GraphicsState entries.");
            m_GraphicsStateCollection.cacheMissCollection.SendToEditor(m_OutputCollectionName + "_CacheMisses");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GraphicsStateCollectionManager))]
class GraphicsStateCollectionManagerEditor : Editor
{
    private const string k_Message =
        "Right click on this component to fill the collection list automatically with the files from the GraphicsStateCollections folder. \n" +
        "Collection files with irrelevant platforms will be excluded from build automatically according to current build target platform by GraphicsStateCollectionStripper.";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.HelpBox(k_Message, MessageType.Info);
    }
}
#endif