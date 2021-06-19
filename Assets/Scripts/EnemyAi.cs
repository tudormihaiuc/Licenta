
using UnityEngine;
using UnityEngine.AI;

public class EnemyAi : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;
    public float health;
    public Vector3 walkPoint;
    public float walkPointRange;
    public float timeBetweenAttacks;
    public GameObject projectile;
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;
    private AiManager aiManager;
    bool walkPointSet;
    bool alreadyAttacked;
    //When the script is loaded find the NavMeshAgent
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    //When the runtime of the script starts, create an aiManager object from the AiManager class
    private void Start()
    {
        aiManager = GameObject.Find("AiManager").GetComponent<AiManager>();
    }

    private void Update()
    {
        //agent.enabled = false;
        //agent.enabled = true;
        //Every frame get the Player object (the ai needs to now every frame the position of the player)
        player = GameObject.FindGameObjectWithTag("Player").transform;
        //Check for sight and attack range
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);

        //depending of the current range between the player and the ai, set the correct state
        if (playerInAttackRange && playerInSightRange) AttackPlayer();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (!playerInSightRange && !playerInAttackRange) Patroling();
    }

    //takes care of the random movement of the ai
    private void Patroling()
    {
        if (walkPointSet)
        {
            agent.SetDestination(walkPoint);
        }

        if (!walkPointSet) SearchWalkPoint();

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        //Walkpoint reached
        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }
    //function that gives the ai the destination point 
    private void SearchWalkPoint()
    {
        //Calculate random point in range
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        //set a random walking point
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);
        //if the ai can reach the walkpoint
        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
            walkPointSet = true;
    }

    //sets the current destination of the ai to the position of the player
    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
        walkPointSet = false;
    }

    //ai attacks the player
    private void AttackPlayer()
    {
        //Make sure enemy doesn't move
        agent.SetDestination(transform.position);

        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            ///instantiate the projectile so the player can see the attack
            Rigidbody rb = Instantiate(projectile, transform.position, transform.rotation).GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * 20f, ForceMode.Impulse);
            rb.AddForce(transform.up * 4f, ForceMode.Impulse);
            Destroy(rb, 1.5f);
            //if the ai already attacked, call the ResetAttack function
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }
    //changes the alreadyAttacked variable back to false
    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    //function that allows the ai to take damage
    public void AiTakeDamage(int damage)
    {
        health -= damage;
        //if the ai health reaches 0 or below, destroy the enemy
        if (health <= 0)
        {
            DestroyEnemy();
        }
    }
    //destroys the enemy gameObj and calls the spawn function again
    private void DestroyEnemy()
    {
        Destroy(gameObject);
        aiManager.SpawnAi();

    }

}
