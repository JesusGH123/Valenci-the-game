using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject zombie;
    public static bool nextHorde;

    public int hordesKilled = 0;
    private int hordeLimit = 4;

    private AudioSource audioSource;
    public AudioClip alarm;

    private float zombieNumber = 0;

    void Start()
    {
        nextHorde = true;
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        //Check if there are zombies left
        if (gameObject.transform.childCount == 0 && nextHorde && hordesKilled < hordeLimit)
        {
            zombieNumber += 5;
            StartCoroutine("spawnZombie", zombieNumber);

            audioSource.PlayOneShot(alarm, 0.7f);

            hordesKilled++;
        }

        if(hordesKilled >= hordeLimit)
        {
            SceneManager.LoadScene("WinScreen");
        }

    }

    IEnumerator spawnZombie()
    {
        nextHorde = false;
        yield return new WaitForSeconds(10.0f);

        for (int i = 0; i < zombieNumber; i++)
            Instantiate(zombie, gameObject.transform);
        nextHorde = true;
    }
}
