using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* stores info for loss condition */ 
public class LossData : MonoBehaviour
{    
    public int lives = 3;


    public string scenename;

        [SerializeField] public Image lives1;
        [SerializeField] public Image lives2;
        [SerializeField] public Image lives3;


    public void CheckLoss()
    {
         if(lives == 2) {
            lives1.gameObject.SetActive(false);
            }
        else if(lives == 1) {
            lives2.gameObject.SetActive(false);
        }
        else if(lives == 0) {
            lives3.gameObject.SetActive(false);
            SceneManager.LoadScene(scenename);
        }
        return;
    }
}
