using UnityEngine;

// Pengembangan E — Health Bar (world space, melayang di atas kepala).
// Cukup Add Component "HealthBar" ke Player / Enemy. Bar dibuat otomatis saat Play,
// tidak perlu membuat Canvas atau UI manual.
public class HealthBar : MonoBehaviour
{
    [Header("Posisi & Ukuran")]
    [SerializeField] private float heightOffset = 1.7f;
    [SerializeField] private float width = 1.8f;
    [SerializeField] private float height = 0.3f;

    [Header("Warna")]
    [SerializeField] private Color fullColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField] private Color midColor = new Color(1f, 0.8f, 0.1f);
    [SerializeField] private Color lowColor = new Color(1f, 0.2f, 0.15f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color delayedColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Animasi")]
    [Tooltip("Kecepatan bar putih (damage terakhir) menyusul bar utama.")]
    [SerializeField] private float delayedSpeed = 0.6f;

    private IHealth health;
    private Transform root;
    private Transform fill, delayed;
    private Material fillMat;
    private float shownRatio = 1f;
    private float delayedRatio = 1f;

    private void Start()
    {
        health = FindActiveHealth();
        if (health == null)
        {
            Debug.LogWarning(name + ": HealthBar tidak menemukan komponen IHealth yang aktif.");
            enabled = false;
            return;
        }
        BuildBar();
    }

    // Enemy bisa punya beberapa controller; ambil yang sedang aktif (enabled).
    private IHealth FindActiveHealth()
    {
        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
            if (mb is IHealth h && mb.enabled)
                return h;
        return null;
    }

    private void BuildBar()
    {
        root = new GameObject("HealthBar").transform;
        root.SetParent(transform, false);
        root.localPosition = Vector3.up * heightOffset;

        CreateQuad("Background", backgroundColor, new Vector3(width + 0.06f, height + 0.06f, 1f), 0.002f);
        delayed = CreateQuad("Delayed", delayedColor, new Vector3(width, height, 1f), 0.001f).transform;
        var f = CreateQuad("Fill", fullColor, new Vector3(width, height, 1f), 0f);
        fill = f.transform;
        fillMat = f.GetComponent<Renderer>().material;
    }

    private GameObject CreateQuad(string n, Color c, Vector3 scale, float zOffset)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = n;
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(root, false);
        q.transform.localPosition = new Vector3(0, 0, zOffset);
        q.transform.localScale = scale;

        Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material m = new Material(s);
        m.SetColor("_BaseColor", c);
        m.color = c;
        if (c.a < 1f)
        {
            // Transparan untuk background
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        Renderer r = q.GetComponent<Renderer>();
        r.material = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return q;
    }

    private void LateUpdate()
    {
        if (root == null) return;

        // Sembunyikan saat kamera close-up agar tidak menutupi karakter
        bool show = !ZoomCamera.IsCloseUp;
        if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
        if (!show) return;

        float target = health.MaxHealth > 0 ? (float)health.CurrentHealth / health.MaxHealth : 0f;

        // Bar utama turun cepat, bar putih menyusul pelan (efek "damage chip")
        shownRatio = Mathf.MoveTowards(shownRatio, target, Time.deltaTime * 3f);
        if (delayedRatio < shownRatio) delayedRatio = shownRatio;
        else delayedRatio = Mathf.MoveTowards(delayedRatio, shownRatio, Time.deltaTime * delayedSpeed);

        SetBar(fill, shownRatio);
        SetBar(delayed, delayedRatio);

        Color c = shownRatio > 0.5f
            ? Color.Lerp(midColor, fullColor, (shownRatio - 0.5f) * 2f)
            : Color.Lerp(lowColor, midColor, shownRatio * 2f);
        fillMat.SetColor("_BaseColor", c);
        fillMat.color = c;

        // Billboard: selalu menghadap kamera
        Camera cam = Camera.main;
        if (cam != null)
            root.rotation = cam.transform.rotation;
    }

    // Skala bar dari kiri ke kanan (pivot di sisi kiri)
    private void SetBar(Transform bar, float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        Vector3 s = bar.localScale;
        s.x = width * ratio;
        bar.localScale = s;
        Vector3 p = bar.localPosition;
        p.x = -width * (1f - ratio) * 0.5f;
        bar.localPosition = p;
    }
}
