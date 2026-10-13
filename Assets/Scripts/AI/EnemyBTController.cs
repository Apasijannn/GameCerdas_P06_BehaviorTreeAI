using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBTController : MonoBehaviour, IHealth
{
    // IHealth (dipakai HealthBar)
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform safePoint;

    [Header("Perception")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 10;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int lowHealthThreshold = 30;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float fleeSpeed = 5f;

    [Header("Search (Pengembangan A)")]
    [SerializeField] private float searchSpeed = 3f;
    [SerializeField] private float searchLookDuration = 3f;   // lama melihat sekeliling di titik terakhir
    [SerializeField] private float searchTurnSpeed = 120f;    // derajat per detik saat melihat sekeliling
    [SerializeField] private float memoryDuration = 10f;      // lupa posisi player setelah sekian detik

    [Header("Debug")]
    [SerializeField] private string currentAction = "None";

    private NavMeshAgent agent;
    private BTNode rootNode;

    private int currentHealth;
    private int currentPatrolIndex = 0;

    // Memory (blackboard sederhana)
    private bool hasLastSeenPosition = false;
    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    private float searchLookTimer = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        BuildBehaviorTree();
    }

    private void Update()
    {
        if (rootNode != null)
        {
            rootNode.Tick();
        }
    }

    private void BuildBehaviorTree()
    {
        // ------------------------------
        // FLEE
        // ------------------------------

        BTNode fleeSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(IsHealthLow),
                    new ActionNode(Flee)
                }
            );

        // ------------------------------
        // ATTACK
        // ------------------------------

        BTNode attackAction =
            new ActionNode(AttackPlayer);

        BTNode attackWithCooldown =
            new CooldownDecorator(
                attackAction,
                attackCooldown
            );

        BTNode attackSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(CanSeePlayer),
                    new ConditionNode(IsPlayerInAttackRange),
                    attackWithCooldown
                }
            );

        // ------------------------------
        // CHASE
        // ------------------------------

        BTNode chaseSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(CanSeePlayer),
                    new ActionNode(ChasePlayer)
                }
            );

        // ------------------------------
        // SEARCH LAST SEEN POSITION
        // ------------------------------

        BTNode searchSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(HasLastSeenPosition),
                    new ActionNode(SearchLastSeenPosition)
                }
            );

        // ------------------------------
        // PATROL
        // ------------------------------

        BTNode patrolAction =
            new ActionNode(Patrol);

        // ------------------------------
        // ROOT SELECTOR
        // ------------------------------

        rootNode =
            new SelectorNode(
                new List<BTNode>
                {
                    fleeSequence,
                    attackSequence,
                    chaseSequence,
                    searchSequence,
                    patrolAction
                }
            );
    }

    // ==================================================
    // CONDITIONS
    // ==================================================

    private bool IsHealthLow()
    {
        return currentHealth <= lowHealthThreshold;
    }

    private bool IsPlayerInAttackRange()
    {
        if (player == null)
            return false;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        return distance <= attackRange;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 eyePosition =
            transform.position
            + Vector3.up * 1.5f;

        Vector3 targetPosition =
            player.position
            + Vector3.up * 1f;

        Vector3 directionToPlayer =
            targetPosition - eyePosition;

        float distanceToPlayer =
            directionToPlayer.magnitude;

        // Player terlalu jauh.
        if (distanceToPlayer > visionRange)
            return false;

        // Player berada di luar Field of View.
        float angle =
            Vector3.Angle(
                transform.forward,
                directionToPlayer
            );

        if (angle > visionAngle * 0.5f)
            return false;

        // Periksa apakah ada obstacle.
        bool blocked =
            Physics.Raycast(
                eyePosition,
                directionToPlayer.normalized,
                distanceToPlayer,
                obstacleMask
            );

        if (!blocked)
        {
            // Perception -> Memory: simpan posisi terakhir player terlihat
            hasLastSeenPosition = true;
            lastSeenPosition = player.position;
            lastSeenTime = Time.time;
            searchLookTimer = 0f;
        }

        return !blocked;
    }

    private bool HasLastSeenPosition()
    {
        if (!hasLastSeenPosition)
            return false;

        // Memori kedaluwarsa
        if (Time.time - lastSeenTime > memoryDuration)
        {
            ForgetLastSeenPosition();
            return false;
        }

        return true;
    }

    private void ForgetLastSeenPosition()
    {
        hasLastSeenPosition = false;
        searchLookTimer = 0f;
    }

    // ==================================================
    // ACTIONS
    // ==================================================

    private NodeState Patrol()
    {
        currentAction = "PATROL";

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return NodeState.Failure;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;

        Transform target =
            patrolPoints[currentPatrolIndex];

        agent.SetDestination(target.position);

        if (!agent.pathPending &&
            agent.remainingDistance <= 0.5f)
        {
            currentPatrolIndex++;

            if (currentPatrolIndex
                >= patrolPoints.Length)
            {
                currentPatrolIndex = 0;
            }
        }

        return NodeState.Running;
    }

    private NodeState SearchLastSeenPosition()
    {
        currentAction = "SEARCH";

        agent.isStopped = false;
        agent.speed = searchSpeed;
        agent.stoppingDistance = 0.3f;

        agent.SetDestination(lastSeenPosition);

        // Masih berjalan menuju posisi terakhir
        if (agent.pathPending ||
            agent.remainingDistance > 0.6f)
        {
            return NodeState.Running;
        }

        // Sudah sampai: berhenti dan melihat sekeliling
        agent.isStopped = true;
        transform.Rotate(0f, searchTurnSpeed * Time.deltaTime, 0f);
        searchLookTimer += Time.deltaTime;

        if (searchLookTimer < searchLookDuration)
            return NodeState.Running;

        // Player tidak ditemukan -> lupakan, kembali Patrol
        ForgetLastSeenPosition();
        agent.isStopped = false;
        return NodeState.Success;
    }

    private NodeState ChasePlayer()
    {
        if (player == null)
            return NodeState.Failure;

        currentAction = "CHASE";

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance =
            attackRange * 0.8f;

        agent.SetDestination(player.position);

        return NodeState.Running;
    }

    private NodeState AttackPlayer()
    {
        if (player == null)
            return NodeState.Failure;

        currentAction = "ATTACK";

        agent.isStopped = true;

        FacePlayer();

        Debug.Log(
            name
            + " attacks Player! Damage = "
            + attackDamage
        );

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                attackDamage
            );
        }

        return NodeState.Success;
    }

    private NodeState Flee()
    {
        if (safePoint == null)
            return NodeState.Failure;

        currentAction = "FLEE";

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0.5f;

        agent.SetDestination(
            safePoint.position
        );

        if (!agent.pathPending &&
            agent.remainingDistance <= 0.7f)
        {
            agent.isStopped = true;

            return NodeState.Success;
        }

        return NodeState.Running;
    }

    private void FacePlayer()
    {
        Vector3 direction =
            player.position
            - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude
            < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
    }

    // ==================================================
    // HEALTH
    // ==================================================

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0,
                maxHealth
            );

        Debug.Log(
            name
            + " Health = "
            + currentHealth
        );
    }

    [ContextMenu("Test Damage 25")]
    private void TestDamage25()
    {
        TakeDamage(25);
    }

    [ContextMenu("Reset Health")]
    private void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        // Vision Range
        Gizmos.DrawWireSphere(
            transform.position,
            visionRange
        );

        // Attack Range
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        // FOV direction
        Vector3 leftDirection =
            Quaternion.Euler(
                0,
                -visionAngle * 0.5f,
                0
            )
            * transform.forward;

        Vector3 rightDirection =
            Quaternion.Euler(
                0,
                visionAngle * 0.5f,
                0
            )
            * transform.forward;

        Gizmos.DrawRay(
            transform.position,
            leftDirection * visionRange
        );

        Gizmos.DrawRay(
            transform.position,
            rightDirection * visionRange
        );

        // Last Seen Position
        if (hasLastSeenPosition)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSeenPosition, 0.5f);
            Gizmos.DrawLine(transform.position, lastSeenPosition);
        }
    }
}