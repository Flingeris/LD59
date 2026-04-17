using UnityEngine;

public class ServiceMain : MonoBehaviour
{
    public static bool ServicesReady { get; private set; }


    private void Awake()
    {
        if (G.ServiceMain != null && G.ServiceMain != this)
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        G.ServiceMain = this;
        InitializeServices();
    }

    public async void InitializeServices()
    {
        try
        {
            // await AnalyticsSystem.Init();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Services] Init failed: {e}");
        }
        finally
        {
            ServicesReady = true;
        }
    }
}