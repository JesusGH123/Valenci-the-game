using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasScript : MonoBehaviour
{
    public GameObject gameInt;
    public GameObject pauseInt;

    public static bool gamePaused = false;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gamePaused = !gamePaused;
        }

        if (gamePaused)
        {
            Time.timeScale = 0;
            pauseInt.SetActive(true);
            gameInt.SetActive(false);
        }
        else
        {
            Time.timeScale = 1;
            pauseInt.SetActive(false);
            gameInt.SetActive(true);
        }

        if(!ZombieSpawner.nextHorde)
        {
            gameInt.transform.Find("Announce").gameObject.SetActive(true);
        } else
        {
            gameInt.transform.Find("Announce").gameObject.SetActive(false);
        }
    }
}
