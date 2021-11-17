using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieBehaviour : MonoBehaviour
{
    public NavMeshAgent agent;
    Animator animator;
    AudioSource zombieAudio;
    Rigidbody rb;

    GameObject player;
    [SerializeField]
    private int life = 3;
    private float zombieSpeed = 4f;
    private bool isDead = false;

    void Start()
    {
        if (player == null)
            player = GameObject.Find("Player");

        animator = GetComponent<Animator>();
        zombieAudio = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!isDead)
        {
            //Move zombie
            agent.SetDestination(player.transform.position);
            agent.speed = zombieSpeed;

            if (life <= 0)   //Zombie is killed
            {
                StartCoroutine("Dying");
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
            animator.SetBool("isAttacking", true);
    }

    private void OnCollisionExit(Collision collision)
    {
        animator.SetBool("isAttacking", false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ammo")
            ReceiveDamage(1);

    }

    IEnumerator Dying()
    {
        isDead = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        animator.SetBool("Died", true);
        zombieAudio.enabled = false;
        agent.enabled = false;

        yield return new WaitForSeconds(5.0f);
        Destroy(gameObject);
        yield return null;
    }

    public void ReceiveDamage(int damage)
    {
        life -= damage;
    }
}
