using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

// Menghubungkan keputusan AI (currentAction) & NavMeshAgent ke Animator.
// Bekerja untuk EnemyBTController / EnemyUtilityController / EnemyHybridController.
// Parameter Animator: Speed (float), Running (bool, hanya saat CHASE), Attack (trigger), Hit (trigger)
public class EnemyAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Tooltip("Jeda minimal antar animasi Attack (detik).")]
    [SerializeField] private float attackAnimInterval = 0.8f;

    // Jumlah enemy yang sedang CHASE (dipakai Player untuk ikut berlari)
    private static readonly System.Collections.Generic.HashSet<EnemyAnimatorDriver> chasing =
        new System.Collections.Generic.HashSet<EnemyAnimatorDriver>();
    public static bool AnyEnemyChasing => chasing.Count > 0;

    private void OnDisable() => chasing.Remove(this);

    private NavMeshAgent agent;
    private MonoBehaviour controller;
    private FieldInfo actionField;
    private string lastAction;
    private float nextAttackAnim;
    private IHealth health;
    private int lastHealth;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        FindController();
    }

    private void LateUpdate()
    {
        if (animator == null || health == null) return;
        int hp = health.CurrentHealth;
        if (hp < lastHealth) animator.SetTrigger("Hit");
        lastHealth = hp;
    }

    private void FindController()
    {
        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (!mb.enabled) continue;
            FieldInfo f = mb.GetType().GetField("currentAction",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null)
            {
                controller = mb; actionField = f;
                health = mb as IHealth;
                lastHealth = health != null ? health.CurrentHealth : 0;
                return;
            }
        }
    }

    private void Update()
    {
        if (animator == null) return;
        if (controller == null || !controller.enabled) FindController();

        float speed = agent != null && !agent.isStopped ? agent.velocity.magnitude : 0f;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        string action = controller != null ? actionField.GetValue(controller) as string : null;

        bool isChasing = action == "CHASE";
        animator.SetBool("Running", isChasing);
        if (isChasing) chasing.Add(this); else chasing.Remove(this);

        if (action == "ATTACK" && Time.time >= nextAttackAnim)
        {
            animator.SetTrigger("Attack");
            nextAttackAnim = Time.time + attackAnimInterval;
        }
        lastAction = action;
    }
}
