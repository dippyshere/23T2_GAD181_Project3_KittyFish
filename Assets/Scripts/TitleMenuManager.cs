using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class TitleMenuManager : MonoBehaviour
{
    void Start()
    {
        if (Application.isMobilePlatform)
        {
            Application.targetFrameRate = Mathf.CeilToInt((float)Screen.currentResolution.refreshRateRatio.value);
        }
        else
        {
            Application.targetFrameRate = -1;
        }

        InputSystem.settings.SetInternalFeatureFlag("USE_OPTIMIZED_CONTROLS", true);
        InputSystem.settings.SetInternalFeatureFlag("USE_READ_VALUE_CACHING", true);
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("Level1");
    }
}
