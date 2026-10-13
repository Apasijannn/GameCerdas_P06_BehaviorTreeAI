using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IHealth
{
    // IHealth (dipakai HealthBar)
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public bool IsDead => currentHealth <= 0;

    [SerializeField]
    private int maxHealth = 100;

    [Header("Respawn")]
    [Tooltip("Hidup lagi otomatis setelah mati.")]
    [SerializeField] private bool autoRespawn = true;
    [SerializeField] private float respawnDelay = 3f;

    private int currentHealth;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool respawning;

    private void Awake()
    {
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;   // sudah mati, abaikan serangan berikutnya

        currentHealth -= damage;

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0,
                maxHealth
            );

        Debug.Log(
            "Player Health = "
            + currentHealth
        );

        if (currentHealth <= 0)
        {
            Debug.Log("Player Dead");
            if (autoRespawn && !respawning)
                StartCoroutine(RespawnAfterDelay());
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
        respawning = false;
    }

    [ContextMenu("Respawn Now")]
    public void Respawn()
    {
        // CharacterController harus dimatikan sebentar agar posisi bisa dipindah
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        if (cc != null) cc.enabled = true;

        currentHealth = maxHealth;
        Debug.Log("Player Respawn");

        // Reset animasi (keluar dari pose Death)
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }
}
