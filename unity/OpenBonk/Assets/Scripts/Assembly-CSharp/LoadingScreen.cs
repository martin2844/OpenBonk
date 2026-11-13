using System.Collections;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour
{
    private static string sceneName = "GeneratedMap";
    private static string finalMapName = "FinalBossMap";

    public Transform loadingBar;
    public TextMeshProUGUI t_loading;
    public static bool isLoading;

    private void Start()
    {
        WindowManager.CloseAll();
        if (t_loading != null) t_loading.text = LocalizationUtility.GetLocalizedString("MainMenuOther", "LOADING");
        isLoading = true;
        StartCoroutine(LoadSceneAsync());
    }

    private void OnDestroy() { isLoading = false; }
    private void OnDisable() { isLoading = false; }

    private IEnumerator LoadSceneAsync()
    {
        string target = MapController.IsFinalBossStage() ? finalMapName : sceneName;
        AsyncOperation load = SceneManager.LoadSceneAsync(target);
        isLoading = true;
        load.allowSceneActivation = false;
        float timer = 0f;
        const float timeout = 30f;
        while (!load.isDone)
        {
            timer += Time.deltaTime;
            if (loadingBar != null) loadingBar.localScale = new Vector3(load.progress / .9f, 1f, 1f);
            if (load.progress >= .9f) load.allowSceneActivation = true;
            if (timer > timeout)
            {
                Debug.LogError($"Loading {target} exceeded {timeout} seconds; loading synchronously.");
                load.allowSceneActivation = false;
                SceneManager.LoadScene(target);
                yield break;
            }
            yield return null;
        }
    }

    public static void LoadInstant()
    {
        SceneManager.LoadScene(MapController.IsFinalBossStage() ? finalMapName : sceneName);
    }
}
