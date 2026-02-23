// BootstrapLoader.cs
using UnityEngine;

public class BootstrapLoader
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadBootstrap()
    {
        if (GameManager._instance != null) return;

        // Load a GameManager prefab from Resources folder
        var prefab = Resources.Load<GameObject>("Bootstrap/GameManager");
        if (prefab != null)
        {
            GameObject.Instantiate(prefab);
            Debug.Log("[Bootstrap] Spawned GameManager for dev scene.");
        }
        else
        {
            Debug.LogWarning("[Bootstrap] No GameManager prefab found in Resources/Bootstrap/");
        }
    }
#endif
}