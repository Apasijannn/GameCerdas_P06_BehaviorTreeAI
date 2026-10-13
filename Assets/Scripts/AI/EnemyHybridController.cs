using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Pengembangan C — Behavior Tree + Utility AI (Hybrid), modul bagian 76.
//
// Root
//  └── Selector
//       ├── Emergency Sequence      : Critical Health? → Flee
//       ├── Combat Sequence         : Can See Player?  → Utility Combat Selector
//       │                                                 ├── Attack
//       │                                                 ├── Chase
//       │                                                 └── Take Cover
//       ├── Search Sequence         : Has Last Seen?   → Search Last Seen Position
//       └── Patrol
//
// Behavior Tree mengatur struktur besar, Utility AI memilih aksi terbaik saat combat.
// Pakai SALAH SATU controller saja di Enemy (matikan EnemyBTController / EnemyUtilityController).
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyHybridController : MonoBehaviour, IHealth
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
    [Tooltip("Di bawah nilai ini Emergency Flee mengambil alih (BT).")]
    [SerializeField] private int criticalHealthThreshold = 20;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float coverSpeed = 4.5f;
    [SerializeField] private float searchSpeed = 3f;

    [Header("Take Cover")]
    [SerializeField] private float coverSearchRadius = 8f;
    [SerializeField] private float coverHoldTime = 2f;

    [Header("Search")]
    [SerializeField] private float searchLookDuration = 3f;
    [SerializeField] private float memoryDuration = 10f;

    [Header("Utility Settings")]
    [SerializeField] private float minActionDuration = 0.5f;
    [SerializeField] private float hysteresis = 0.1f;

    [Header("Debug (Read Only)")]
    [SerializeField] private string currentAction = "None";
    [SerializeField] private int currentHealth;
    [SerializeField, Range(0, 1)] private float attackScore;
    [SerializeField, Range(0, 1)] private float chaseScore;
    [SerializeField, Range(0, 1)] private float takeCoverScore;

    private NavMeshAgent agent;
    private BTNode rootNode;
    private UtilitySelectorNode combatSelector;

    private int currentPatrolIndex;
    private bool canSeeNow;

    // Memory
    private bool hasLastSeen;
    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    private float searchLookTimer;

    // Cover
    private bool hasCoverPoint;
    private Vector3 coverPoint;
    private float coverReachedTime = -1f;

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
        rootNode?.Tick();

        if (combatSelector != null)
        {
            attackScore = combatSelector.Options[0].lastScore;
            chaseScore = combatSelector.Options[1].lastScore;
            takeCoverScore = combatSelector.Options[2].lastScore;
        }
    }

    // ==================================================
    // TREE
    // ==================================================

    private void BuildBehaviorTree()
    {
        BTNode emergency = new SequenceNode(new List<BTNode>
        {
            new ConditionNode(IsHealthCritical),
            new ActionNode(Flee)
        });

        combatSelector = new UtilitySelectorNode(
            new List<UtilitySelectorNode.Option>
            {
                new UtilitySelectorNode.Option("Attack",
                    new CooldownDecorator(new ActionNode(AttackPlayer), attackCooldown),
                    ScoreAttack),
                new UtilitySelectorNode.Option("Chase",
                    new ActionNode(ChasePlayer),
                    ScoreChase),
                new UtilitySelectorNode.Option("TakeCover",
                    new ActionNode(TakeCover),
                    ScoreTakeCover)
            },
            minActionDuration,
            hysteresis);

        BTNode combat = new SequenceNode(new List<BTNode>
        {
            new ConditionNode(IsInCombat),
            combatSelector
        });

        BTNode search = new SequenceNode(new List<BTNode>
        {
            new ConditionNode(HasLastSeenPosition),
            new ActionNode(SearchLastSeenPosition)
        });

        rootNode = new SelectorNode(new List<BTNode>
        {
            emergency,
            combat,
            search,
            new ActionNode(Patrol)
        });
    }

    // ==================================================
    // UTILITY SCORES (hanya dihitung saat player terlihat)
    // ==================================================

    private float HealthPercent => (float)currentHealth / maxHealth;
    private float DistanceToPlayer => Vector3.Distance(transform.position, player.position);

    private float ScoreAttack()
    {
        if (!canSeeNow) return 0f;
        float d = DistanceToPlayer;
        if (d > attackRange) return 0f;
        float closeness = Mathf.Clamp01(1f - d / attackRange);
        // Makin sehat makin berani menyerang
        return (0.6f + 0.4f * closeness) * Mathf.Lerp(0.6f, 1f, HealthPercent);
    }

    private float ScoreChase()
    {
        if (!canSeeNow) return 0f;
        float d = DistanceToPlayer;
        if (d <= attackRange) return 0.1f;
        float far = Mathf.InverseLerp(attackRange, visionRange, d);
        return Mathf.Lerp(0.5f, 0.8f, far) * Mathf.Lerp(0.5f, 1f, HealthPercent);
    }

    private float ScoreTakeCover()
    {
        // Muncul saat health menengah ke bawah: makin terluka makin ingin berlindung
        float hurt = Mathf.Clamp01((0.7f - HealthPercent) / 0.5f);   // 0 saat ≥70%, 1 saat ≤20%
        return hurt * 0.95f;
    }

    // ==================================================
    // CONDITIONS
    // ==================================================

    // Combat aktif jika player terlihat, ATAU enemy sedang berlindung di balik cover
    // (supaya Take Cover tidak langsung batal begitu player tidak terlihat).
    private bool IsInCombat()
    {
        canSeeNow = CanSeePlayer();
        return canSeeNow || hasCoverPoint;
    }

    private bool IsHealthCritical() => currentHealth <= criticalHealthThreshold;

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 eye = transform.position + Vector3.up * 1.5f;
        Vector3 dir = (player.position + Vector3.up) - eye;
        float dist = dir.magnitude;

        if (dist > visionRange) return false;
        if (Vector3.Angle(transform.forward, dir) > visionAngle * 0.5f) return false;
        if (Physics.Raycast(eye, dir.normalized, dist, obstacleMask)) return false;

        hasLastSeen = true;
        lastSeenPosition = player.position;
        lastSeenTime = Time.time;
        searchLookTimer = 0f;
        return true;
    }

    private bool HasLastSeenPosition()
    {
        if (hasLastSeen && Time.time - lastSeenTime > memoryDuration)
            hasLastSeen = false;
        return hasLastSeen;
    }

    // ==================================================
    // ACTIONS
    // ==================================================

    private NodeState Patrol()
    {
        currentAction = "PATROL";
        hasCoverPoint = false;
        if (patrolPoints == null || patrolPoints.Length == 0) return NodeState.Failure;

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

        return NodeState.Running;
    }

    private NodeState ChasePlayer()
    {
        currentAction = "CHASE";
        hasCoverPoint = false;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.SetDestination(player.position);
        return NodeState.Running;
    }

    private NodeState AttackPlayer()
    {
        currentAction = "ATTACK";
        hasCoverPoint = false;
        agent.isStopped = true;
        FacePlayer();

        Debug.Log(name + " [Hybrid] attacks Player! Damage = " + attackDamage);
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(attackDamage);

        return NodeState.Success;
    }

    private NodeState TakeCover()
    {
        currentAction = "TAKE COVER";

        if (!hasCoverPoint && !FindCoverPoint(out coverPoint))
        {
            // Tidak ada cover di sekitar → gagal, BT mencoba cabang lain
            return NodeState.Failure;
        }
        hasCoverPoint = true;

        agent.isStopped = false;
        agent.speed = coverSpeed;
        agent.stoppingDistance = 0.2f;
        agent.SetDestination(coverPoint);

        if (agent.pathPending || agent.remainingDistance > 0.5f)
        {
            coverReachedTime = -1f;
            return NodeState.Running;
        }

        // Sudah di balik cover: tunggu sebentar lalu cari cover baru bila masih terlihat
        agent.isStopped = true;
        if (coverReachedTime < 0f) coverReachedTime = Time.time;
        if (Time.time - coverReachedTime >= coverHoldTime)
        {
            hasCoverPoint = false;
            coverReachedTime = -1f;
        }
        return NodeState.Running;
    }

    private NodeState Flee()
    {
        currentAction = "FLEE";
        hasCoverPoint = false;
        if (safePoint == null) return NodeState.Failure;

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0.5f;
        agent.SetDestination(safePoint.position);

        if (!agent.pathPending && agent.remainingDistance <= 0.7f)
        {
            agent.isStopped = true;
            return NodeState.Success;
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

        if (agent.pathPending || agent.remainingDistance > 0.6f)
            return NodeState.Running;

        agent.isStopped = true;
        transform.Rotate(0f, 120f * Time.deltaTime, 0f);
        searchLookTimer += Time.deltaTime;
        if (searchLookTimer < searchLookDuration)
            return NodeState.Running;

        hasLastSeen = false;
        agent.isStopped = false;
        return NodeState.Success;
    }

    // ==================================================
    // COVER FINDING
    // ==================================================

    // Cari titik di balik obstacle terdekat, di sisi yang berlawanan dengan player.
    private bool FindCoverPoint(out Vector3 result)
    {
        result = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(transform.position, coverSearchRadius, obstacleMask);

        float bestDist = Mathf.Infinity;
        foreach (Collider c in hits)
        {
            Vector3 center = c.bounds.center;
            Vector3 away = center - player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) continue;

            float extent = Mathf.Max(c.bounds.extents.x, c.bounds.extents.z);
            Vector3 candidate = center + away.normalized * (extent + 1f);
            candidate.y = 0f;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
                continue;

            // Pastikan titik ini benar-benar tersembunyi dari player
            Vector3 from = navHit.position + Vector3.up * 1.5f;
            Vector3 to = player.position + Vector3.up;
            if (!Physics.Linecast(from, to, obstacleMask))
                continue;

            float d = Vector3.Distance(transform.position, navHit.position);
            if (d < bestDist)
            {
                bestDist = d;
                result = navHit.position;
            }
        }
        return bestDist < Mathf.Infinity;
    }

    private void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    // ==================================================
    // HEALTH
    // ==================================================

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        Debug.Log(name + " Health = " + currentHealth);
    }

    [ContextMenu("Test Damage 25")]
    private void TestDamage25() => TakeDamage(25);

    [ContextMenu("Test Damage 10")]
    private void TestDamage10() => TakeDamage(10);

    [ContextMenu("Reset Health")]
    private void ResetHealth() => currentHealth = maxHealth;

    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 l = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward;
        Vector3 r = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, l * visionRange);
        Gizmos.DrawRay(transform.position, r * visionRange);

        if (hasCoverPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(coverPoint + Vector3.up * 0.5f, Vector3.one);
            Gizmos.DrawLine(transform.position, coverPoint);
        }
        if (hasLastSeen)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSeenPosition, 0.5f);
        }
    }
}
