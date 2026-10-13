using UnityEngine;
using UnityEngine.AI;

// Pengembangan B — Full Utility AI Controller (Praktikum 06)
// Alternatif dari EnemyBTController: aksi dipilih berdasarkan SKOR tertinggi, bukan urutan prioritas.
// Pakai SALAH SATU controller saja di Enemy (matikan EnemyBTController saat memakai ini).
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyUtilityController : MonoBehaviour, IHealth
{
    // IHealth (dipakai HealthBar)
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private enum UtilityAction { Patrol, Chase, Attack, Flee }

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

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float fleeSpeed = 5f;

    [Header("Utility Settings")]
    [Tooltip("Skor default Patrol, agar selalu ada pilihan.")]
    [SerializeField] private float patrolBaseScore = 0.1f;
    [Tooltip("Action Commitment: lama minimal sebuah aksi dipertahankan (detik).")]
    [SerializeField] private float minActionDuration = 0.5f;
    [Tooltip("Hysteresis: aksi baru harus lebih besar dari aksi sekarang + nilai ini.")]
    [SerializeField] private float hysteresis = 0.1f;

    [Header("Debug (Read Only)")]
    [SerializeField] private string currentAction = "None";
    [SerializeField] private int currentHealth;
    [SerializeField] private bool canSeePlayer;
    [SerializeField] private float distanceToPlayer;
    [SerializeField, Range(0, 1)] private float attackScore;
    [SerializeField, Range(0, 1)] private float chaseScore;
    [SerializeField, Range(0, 1)] private float fleeScore;
    [SerializeField, Range(0, 1)] private float patrolScore;

    private NavMeshAgent agent;
    private UtilityAction activeAction = UtilityAction.Patrol;
    private float actionStartTime;
    private float nextAttackTime;
    private int currentPatrolIndex;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // 1. PERCEPTION
        canSeePlayer = CanSeePlayer();
        distanceToPlayer = player != null
            ? Vector3.Distance(transform.position, player.position)
            : Mathf.Infinity;

        // 2. HITUNG SEMUA SKOR
        CalculateScores();

        // 3. PILIH AKSI (skor tertinggi + commitment + hysteresis)
        SelectAction();

        // 4. JALANKAN AKSI
        ExecuteAction();
    }

    // ==================================================
    // SCORING
    // ==================================================

    private void CalculateScores()
    {
        float visibilityScore = canSeePlayer ? 1f : 0f;
        float healthPercent = (float)currentHealth / maxHealth;

        // ATTACK: tinggi jika player terlihat & dalam jarak serang. Makin dekat makin tinggi.
        float inRange = distanceToPlayer <= attackRange ? 1f : 0f;
        float closeness = Mathf.Clamp01(1f - distanceToPlayer / attackRange);
        attackScore = visibilityScore * inRange * (0.6f + 0.4f * closeness);

        // CHASE: tinggi jika player terlihat tapi masih di luar jarak serang.
        float distanceNeedScore = distanceToPlayer > attackRange
            ? Mathf.Lerp(0.5f, 0.8f,
                Mathf.InverseLerp(attackRange, visionRange, distanceToPlayer))
            : 0.1f;
        chaseScore = visibilityScore * distanceNeedScore;

        // FLEE: naik bertahap saat health turun; lebih tinggi bila ada ancaman (player terlihat).
        float lowHealthScore = Mathf.Clamp01((1f - healthPercent) * 1.2f);
        float threatScore = canSeePlayer ? 1f : 0.5f;
        fleeScore = (safePoint != null) ? lowHealthScore * threatScore : 0f;

        // PATROL: nilai dasar (default action).
        patrolScore = patrolBaseScore;
    }

    private float GetScore(UtilityAction a)
    {
        switch (a)
        {
            case UtilityAction.Attack: return attackScore;
            case UtilityAction.Chase:  return chaseScore;
            case UtilityAction.Flee:   return fleeScore;
            default:                   return patrolScore;
        }
    }

    private void SelectAction()
    {
        // Cari aksi dengan skor terbesar
        UtilityAction best = UtilityAction.Patrol;
        float bestScore = -1f;

        foreach (UtilityAction a in System.Enum.GetValues(typeof(UtilityAction)))
        {
            float s = GetScore(a);
            if (s > bestScore)
            {
                bestScore = s;
                best = a;
            }
        }

        if (best == activeAction)
            return;

        // Action Commitment: tahan aksi sekarang minimal sekian detik
        if (Time.time - actionStartTime < minActionDuration)
            return;

        // Hysteresis: aksi baru harus cukup jauh lebih baik
        if (bestScore < GetScore(activeAction) + hysteresis)
            return;

        activeAction = best;
        actionStartTime = Time.time;
    }

    private void ExecuteAction()
    {
        switch (activeAction)
        {
            case UtilityAction.Attack: Attack(); break;
            case UtilityAction.Chase:  Chase();  break;
            case UtilityAction.Flee:   Flee();   break;
            default:                   Patrol(); break;
        }
    }

    // ==================================================
    // PERCEPTION
    // ==================================================

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 targetPosition = player.position + Vector3.up * 1f;
        Vector3 directionToPlayer = targetPosition - eyePosition;
        float distance = directionToPlayer.magnitude;

        if (distance > visionRange)
            return false;

        if (Vector3.Angle(transform.forward, directionToPlayer) > visionAngle * 0.5f)
            return false;

        return !Physics.Raycast(eyePosition, directionToPlayer.normalized, distance, obstacleMask);
    }

    // ==================================================
    // ACTIONS
    // ==================================================

    private void Patrol()
    {
        currentAction = "PATROL";
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void Chase()
    {
        currentAction = "CHASE";
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.SetDestination(player.position);
    }

    private void Attack()
    {
        currentAction = "ATTACK";
        agent.isStopped = true;
        FacePlayer();

        // Cooldown agar tidak menyerang setiap frame
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        Debug.Log(name + " [Utility] attacks Player! Damage = " + attackDamage);

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(attackDamage);
    }

    private void Flee()
    {
        currentAction = "FLEE";
        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0.5f;
        agent.SetDestination(safePoint.position);

        if (!agent.pathPending && agent.remainingDistance <= 0.7f)
            agent.isStopped = true;
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            10f * Time.deltaTime);
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

        Vector3 left = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, left * visionRange);
        Gizmos.DrawRay(transform.position, right * visionRange);
    }
}
