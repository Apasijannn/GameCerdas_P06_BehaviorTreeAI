using System.Reflection;
using UnityEngine;

// Pengembangan E — Personality (Praktikum 06, bagian 80).
// Pasang di Enemy bersama controller AI-nya (BT / Utility / Hybrid).
// Saat Awake, parameter controller diganti sesuai kepribadian yang dipilih.
// Nilai preset diambil dari Eksperimen 4 (Aggressive) & Eksperimen 5 (Coward) di modul.
[DefaultExecutionOrder(-100)]   // jalan sebelum controller membangun tree-nya
public class EnemyPersonality : MonoBehaviour
{
    public enum Personality { Aggressive, Coward }

    [SerializeField] private Personality personality = Personality.Aggressive;
    [Tooltip("Warnai tubuh enemy sesuai kepribadian.")]
    [SerializeField] private bool tintBody = true;

    public Personality Type => personality;

    private void Awake() => Apply();

    [ContextMenu("Apply Personality")]
    public void Apply()
    {
        foreach (MonoBehaviour c in GetComponents<MonoBehaviour>())
        {
            if (c == this || c.GetType().GetField("currentAction", Flags) == null)
                continue;   // bukan controller AI

            if (personality == Personality.Aggressive)
            {
                // Eksperimen 4 — Enemy Aggressive
                Set(c, "visionRange", 15f);
                Set(c, "visionAngle", 150f);
                Set(c, "chaseSpeed", 5f);
                Set(c, "attackRange", 2.5f);
                Set(c, "attackCooldown", 0.8f);
                Set(c, "lowHealthThreshold", 15);       // EnemyBTController
                Set(c, "criticalHealthThreshold", 10);  // EnemyHybridController
                Set(c, "fleeSpeed", 5f);
            }
            else
            {
                // Eksperimen 5 — Enemy Coward
                Set(c, "visionRange", 10f);
                Set(c, "visionAngle", 90f);
                Set(c, "chaseSpeed", 3f);
                Set(c, "attackRange", 2f);
                Set(c, "attackCooldown", 2f);
                Set(c, "lowHealthThreshold", 70);
                Set(c, "criticalHealthThreshold", 50);
                Set(c, "fleeSpeed", 6f);
            }
        }

        if (tintBody && Application.isPlaying)
            TintModel();
    }

    // Tint halus + sedikit lebih terang pada model (kucing / capsule)
    private void TintModel()
    {
        Color tint = personality == Personality.Aggressive
            ? new Color(1.35f, 1.12f, 1.05f)    // hangat, sedikit kemerahan
            : new Color(1.10f, 1.20f, 1.40f);   // dingin, sedikit kebiruan

        Transform model = transform.Find("CatModel");
        Renderer[] renderers = model != null
            ? model.GetComponentsInChildren<Renderer>()
            : GetComponents<Renderer>();

        var block = new MaterialPropertyBlock();
        foreach (Renderer r in renderers)
        {
            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                Material m = r.sharedMaterials[i];
                if (m == null) continue;
                r.GetPropertyBlock(block, i);
                if (m.HasProperty("_BaseColor")) block.SetColor("_BaseColor", m.GetColor("_BaseColor") * tint);
                if (m.HasProperty("_Color")) block.SetColor("_Color", m.GetColor("_Color") * tint);
                r.SetPropertyBlock(block, i);
            }
        }
    }

    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    // Set field private di controller (hanya jika field-nya ada)
    private static void Set(object target, string field, object value)
    {
        FieldInfo f = target.GetType().GetField(field, Flags);
        if (f == null) return;
        if (f.FieldType == typeof(int)) f.SetValue(target, System.Convert.ToInt32(value));
        else if (f.FieldType == typeof(float)) f.SetValue(target, System.Convert.ToSingle(value));
    }
}
