// Membuat AggressiveEnemy & CowardEnemy dari Enemy yang sudah ada.
// Menu: Tools > Praktikum 06 > Create Personality Enemies
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class P06PersonalitySetup
{
    [MenuItem("Tools/Praktikum 06/Create Personality Enemies")]
    public static void Create()
    {
        GameObject src = GameObject.Find("Enemy");
        if (src == null)
        {
            EditorUtility.DisplayDialog("Personality", "GameObject 'Enemy' tidak ditemukan / tidak aktif.", "OK");
            return;
        }

        Make(src, "AggressiveEnemy", EnemyPersonality.Personality.Aggressive,
             new Vector3(9f, 1f, -6f), "Assets/Materials/M_EnemyAggressive.mat", new Color(0.9f, 0.15f, 0.1f));
        Make(src, "CowardEnemy", EnemyPersonality.Personality.Coward,
             new Vector3(-9f, 1f, 0f), "Assets/Materials/M_EnemyCoward.mat", new Color(0.3f, 0.55f, 1f));

        Undo.RecordObject(src, "Disable original Enemy");
        src.SetActive(false);   // Enemy lama dinonaktifkan (tidak dihapus)

        EditorSceneManager.MarkSceneDirty(src.scene);
        Debug.Log("[P06] AggressiveEnemy & CowardEnemy dibuat. Enemy lama dinonaktifkan.");
    }

    static void Make(GameObject src, string name, EnemyPersonality.Personality p,
                     Vector3 pos, string matPath, Color color)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject e = Object.Instantiate(src, pos, src.transform.rotation, src.transform.parent);
        e.name = name;
        Undo.RegisterCreatedObjectUndo(e, "Create " + name);

        var pers = e.GetComponent<EnemyPersonality>() ?? e.AddComponent<EnemyPersonality>();
        var so = new SerializedObject(pers);
        so.FindProperty("personality").enumValueIndex = (int)p;
        so.ApplyModifiedPropertiesWithoutUndo();
        pers.Apply();   // langsung terlihat di Inspector controller
        foreach (var c in e.GetComponents<MonoBehaviour>()) EditorUtility.SetDirty(c);

        // Material warna
        Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, matPath);
        }
        m.color = color;
        m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        e.GetComponent<Renderer>().sharedMaterial = m;
    }
}
