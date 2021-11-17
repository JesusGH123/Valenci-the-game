using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject zombie;
    private bool nextHorde;

    private float zombieNumber = 0;

    void Start()
    {
        nextHorde = true;

    }

    void Update()
    {
        //Check if there are zombies left
        if (gameObject.transform.childCount == 0 && nextHorde)
        {
            Debug.Log("Get ready for the next horde");
            zombieNumber += 3;
            StartCoroutine("spawnZombie", zombieNumber);
        }

    }

    IEnumerator spawnZombie()
    {
        nextHorde = false;
        yield return new WaitForSeconds(5.0f);

        for (int i = 0; i < zombieNumber; i++)
            Instantiate(zombie, gameObject.transform);
        nextHorde = true;
    }
}
