using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerSingleton : MonoBehaviour
{
    public static SceneManagerSingleton manager;

    void Awake()
    {
        if (manager == null)
        {
            manager = this;
            DontDestroyOnLoad(this);
            Debug.Log("I am the scene manager");
        } else if (manager != this)
        {
            Debug.Log("duplicate scene manager removed");
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log("Scene manager is ready in scene " + currentScene.name + " with build index " + currentScene.buildIndex);
        if (currentScene.buildIndex == 0)
        {
            Debug.Log("Starting game...");
            goToGame();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void goToGame()
    {
        SceneManager.LoadScene(1);
    }
}
