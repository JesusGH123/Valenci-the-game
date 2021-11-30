using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasScript : MonoBehaviour
{
    public GameObject gameInt;
    public GameObject pauseInt;

    private GameObject announce;

    public static bool gamePaused = false;

    void Start()
    {
        announce = gameInt.transform.Find("Announce").gameObject;
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
            announce.SetActive(true);
            announce.GetComponent<Text>().text = "Prepare... horde " + ZombieSpawner.hordesKilled + " is coming";
        } else
        {
            gameInt.transform.Find("Announce").gameObject.SetActive(false);
        }
    }
}
