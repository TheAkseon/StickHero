using UnityEngine;

public class StateManager : MonoBehaviour
{
    public static StateManager instance;

    public bool hasSceneStarted;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            hasSceneStarted = false;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
