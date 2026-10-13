using UnityEngine;
using UnityEngine.InputSystem;

// Versi Input System baru + CharacterController.
// - Input.GetKey (Input Manager lama) error di project ini, jadi pakai Keyboard.current.
// - transform.position += ... mengabaikan collider (bisa tembus tembok),
//   jadi gerakan memakai CharacterController.Move() yang berhenti saat menabrak collider.
[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    [Tooltip("Kecepatan lari (dipakai saat ada enemy yang CHASE).")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Kecepatan jalan (default).")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private PlayerHealth health;
    private float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<PlayerHealth>();

        // CapsuleCollider bawaan Capsule tidak dibutuhkan lagi (CharacterController sudah punya collider sendiri).
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null) capsule.enabled = false;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;

        // Saat mati, input diabaikan (tunggu respawn)
        if (health != null && health.IsDead) kb = null;

        float horizontal = 0f;
        float vertical = 0f;

        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                horizontal = -1f;

            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                horizontal = 1f;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                vertical = 1f;

            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                vertical = -1f;
        }

        Vector3 direction =
            new Vector3(horizontal, 0f, vertical);

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        // Gravitasi sederhana agar player tetap menapak di lantai
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        float speed = EnemyAnimatorDriver.AnyEnemyChasing ? moveSpeed : walkSpeed;
        Vector3 velocity = direction * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}
