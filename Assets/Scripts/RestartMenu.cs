using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RestartMenu : MonoBehaviour
{
    [SerializeField]
    private Text scoreText;

    public void RestartButton()
    {
        SceneManager.LoadScene("Game");
    }

    void Start()
    {
        scoreText.text = "Score: " + ZombieBehaviour.score;
    }
}
