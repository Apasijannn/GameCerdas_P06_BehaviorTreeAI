using UnityEngine;

// Menghubungkan gerakan & health Player ke Animator.
// Parameter Animator: Speed (float), Running (bool), Hit (trigger), Dead (bool)
// Default Walk; Run hanya saat ada enemy yang sedang CHASE.
public class PlayerAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float turnSpeed = 12f;

    private CharacterController controller;
    private PlayerHealth health;
    private int lastHealth;
    private Vector3 lastPos;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<PlayerHealth>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        lastHealth = health != null ? health.CurrentHealth : 0;
        lastPos = transform.position;
    }

    private void Update()
    {
        if (animator == null) return;

        // Kecepatan horizontal
        Vector3 delta = transform.position - lastPos;
        delta.y = 0f;
        lastPos = transform.position;
        float speed = Time.deltaTime > 0 ? delta.magnitude / Time.deltaTime : 0f;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetBool("Running", EnemyAnimatorDriver.AnyEnemyChasing);

        // Model menghadap arah gerak
        if (delta.sqrMagnitude > 0.00001f)
        {
            Quaternion target = Quaternion.LookRotation(delta.normalized);
            animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // Hit & Dead
        if (health != null)
        {
            int hp = health.CurrentHealth;
            if (hp < lastHealth && hp > 0) animator.SetTrigger("Hit");
            if (hp > lastHealth) animator.ResetTrigger("Hit");   // baru respawn
            animator.SetBool("Dead", hp <= 0);
            lastHealth = hp;
        }
    }
}
