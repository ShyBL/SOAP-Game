using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader instance;
    
    [SerializeField] Slider loadingBar;
    [SerializeField] float fillSpeed = 0.5f;
    [SerializeField] Canvas loadingCanvas;
    [SerializeField] Camera loadingCamera;
    [SerializeField] SceneGroup[] sceneGroups;
    
    float targetProgress;
    bool isLoading;
    
    public readonly SceneGroupManager manager = new SceneGroupManager();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Debugging
        manager.OnSceneLoaded += sceneName => Debug.Log("Loaded: " + sceneName);
        manager.OnSceneUnloaded += sceneName => Debug.Log("Unloaded: " + sceneName);
        manager.OnSceneGroupLoaded += () => Debug.Log("Scene group loaded");
        
        loadingBar.value = 0.1f;
        targetProgress = 1f;
    }

    void Update()
    {
        if (!isLoading) return;
        float currentFillAmount = loadingBar.value;
        float progressDifference = Mathf.Abs(currentFillAmount - targetProgress);
        float dynamicFillSpeed = progressDifference * fillSpeed;
        loadingBar.value = Mathf.Lerp(currentFillAmount, targetProgress, Time.deltaTime * dynamicFillSpeed);
    }

    public async Task LoadSceneGroup(int index)
    {
        loadingBar.value = 0.1f;
        targetProgress = 1f;
        
        if (index < 0 || index >= sceneGroups.Length)
        {
            Debug.LogError("Invalid scene group index: " + index);
            return;
        }

        LoadingProgress progress = new LoadingProgress();
        progress.Progressed += target => targetProgress = Mathf.Max(target, targetProgress);
        EnableLoadingCanvas();
        
       // await Task.Delay(5000);
       
        await manager.LoadScenes(sceneGroups[index], progress);
        EnableLoadingCanvas(false);
    }

    void EnableLoadingCanvas(bool enable = true)
    {
        isLoading = enable;
        loadingCanvas.gameObject.SetActive(enable);
        loadingCamera.gameObject.SetActive(enable);
    }
}