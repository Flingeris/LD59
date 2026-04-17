using UnityEngine;

public class ServiceMain : MonoBehaviour
{
    public static bool isInitialized { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void GameEntryPoint()
    {
        if (isInitialized) return;


        GameObject servicesGo = new GameObject("===Services===");
        DontDestroyOnLoad(servicesGo);
        servicesGo.AddComponent<ServiceMain>();
        isInitialized = true;
    }


    private void Awake()
    {
        if (G.ServiceMain != null && G.ServiceMain != this)
        {
            Destroy(gameObject);
            return;
        }

        G.ServiceMain = this;

        Debug.Log("=========================");
        Debug.Log("Game EntryPoint");

        G.audioSystem = gameObject.AddComponent<AudioSystem>();

        AnalyticsSystem.Init();
        CMS.Init();
    }
}