using UnityEngine;
using UnityEngine.SceneManagement;

public class Control : MonoBehaviour
{
    public string scenename;
    public void NextScene()
    {
        Debug.Log("called");
        SceneManager.LoadScene(scenename);
    }
}