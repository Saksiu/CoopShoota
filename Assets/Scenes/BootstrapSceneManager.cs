using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BootstrapSceneManager : MonoBehaviour
{
    private void Start(){
        UnityEngine.SceneManagement.SceneManager.LoadScene("PlayScene");
    }
}
