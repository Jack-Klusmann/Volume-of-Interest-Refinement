using UnityEngine;
using UnityEngine.SceneManagement;

public class RESTART_APP : MonoBehaviour
{
    public void OnClick()
    {
        SceneManager.LoadScene(0);
    }
}
