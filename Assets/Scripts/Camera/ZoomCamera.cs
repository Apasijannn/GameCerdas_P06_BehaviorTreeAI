using UnityEngine;
using UnityEngine.InputSystem;

// Zoom kamera IN-GAME (bukan slider Scale di tab Game).
// Kamera benar-benar mendekat ke target, jadi karakter dirender ulang dengan detail penuh → tetap tajam.
//   Scroll mouse   : zoom in / out
//   Z / X (tahan)  : zoom in / out dengan keyboard
//   Tab            : ganti target (Player → Enemy → Enemy ...)
//   R              : kembali ke tampilan seluruh arena
// Saat zoom 0 = tampilan arena (posisi kamera awal), zoom 1 = close-up mengikuti target.
[RequireComponent(typeof(Camera))]
public class ZoomCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform[] targets;          // kosong = otomatis: Player + enemy aktif
    [SerializeField] private float targetHeight = 1.0f;    // titik fokus di atas kaki

    [Header("Close-up")]
    [SerializeField] private float closeDistance = 3.5f;   // jarak kamera saat zoom penuh
    [SerializeField] private float closePitch = 25f;       // sudut kamera saat zoom penuh
    [SerializeField] private float closeFov = 40f;

    [Header("Kontrol")]
    [Tooltip("Perubahan zoom per putaran scroll (0..1).")]
    [SerializeField] private float zoomStep = 0.12f;
    [SerializeField] private float smooth = 8f;

    // Dipakai HealthBar & BTDebugger untuk menyembunyikan UI di atas kepala saat close-up
    public static bool IsCloseUp { get; private set; }

    private Vector3 overviewPos;
    private Quaternion overviewRot;
    private float overviewFov;
    private Camera cam;

    private float zoomTarget;   // 0..1
    private float zoom;
    private int targetIndex;

    private void OnDisable() => IsCloseUp = false;

    private void Start()
    {
        cam = GetComponent<Camera>();
        overviewPos = transform.position;
        overviewRot = transform.rotation;
        overviewFov = cam.fieldOfView;

        if (targets == null || targets.Length == 0)
            AutoFindTargets();
    }

    private void AutoFindTargets()
    {
        var list = new System.Collections.Generic.List<Transform>();
        GameObject p = GameObject.Find("Player");
        if (p != null) list.Add(p.transform);
        foreach (var e in FindObjectsByType<EnemyAnimatorDriver>(FindObjectsSortMode.None))
            list.Add(e.transform);
        targets = list.ToArray();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)   // nilai scroll beda-beda tiap mouse → pakai arahnya saja
                zoomTarget = Mathf.Clamp01(zoomTarget + Mathf.Sign(scroll) * zoomStep);
        }

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.tabKey.wasPressedThisFrame && targets.Length > 0)
                targetIndex = (targetIndex + 1) % targets.Length;
            if (kb.rKey.wasPressedThisFrame)
                zoomTarget = 0f;
            if (kb.zKey.isPressed) zoomTarget = Mathf.Clamp01(zoomTarget + Time.deltaTime * 0.8f);
            if (kb.xKey.isPressed) zoomTarget = Mathf.Clamp01(zoomTarget - Time.deltaTime * 0.8f);
        }
    }

    private void LateUpdate()
    {
        zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        IsCloseUp = zoom > 0.35f;
        Transform t = targets.Length > 0 ? targets[targetIndex] : null;

        if (t == null || zoom < 0.001f)
        {
            transform.SetPositionAndRotation(overviewPos, overviewRot);
            cam.fieldOfView = overviewFov;
            return;
        }

        // Pose close-up: di belakang-atas target, arah pandang sama dengan kamera overview (yaw)
        Vector3 focus = t.position + Vector3.up * targetHeight;
        float yaw = overviewRot.eulerAngles.y;
        Quaternion closeRot = Quaternion.Euler(closePitch, yaw, 0f);
        Vector3 closePos = focus - closeRot * Vector3.forward * closeDistance;

        // Ease agar transisi terasa halus
        float k = zoom * zoom * (3f - 2f * zoom);
        transform.position = Vector3.Lerp(overviewPos, closePos, k);
        transform.rotation = Quaternion.Slerp(overviewRot, closeRot, k);
        cam.fieldOfView = Mathf.Lerp(overviewFov, closeFov, k);

        // Near clip kecil agar model tidak terpotong saat sangat dekat
        cam.nearClipPlane = Mathf.Lerp(0.3f, 0.05f, k);
    }
}
