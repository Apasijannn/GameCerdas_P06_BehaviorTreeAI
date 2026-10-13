// Setup arena & environment Praktikum 06 (Game Cerdas) — tema "Sci-Fi Training Arena".
// Menu: Tools > Praktikum 06 > Build Arena
// Hanya environment. Player, Enemy, dan semua script AI dikerjakan sendiri.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.AI.Navigation;

public static class P06ArenaSetup
{
    const string ScenePath = "Assets/Scenes/P06_BehaviorTree.unity";
    const float Half = 15f;      // Ground 30 x 30
    static int obstacleLayer;

    static Material mFloor, mGrid, mWall, mTrim, mCover, mCrate, mPillar, mGlowCyan, mGlowOrange, mGlowGreen, mPad;

    [MenuItem("Tools/Praktikum 06/Build Arena")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        obstacleLayer = EnsureLayer("Obstacle");
        EnsureLayer("Player");
        EnsureLayer("Enemy");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        CreateMaterials();

        var env = new GameObject("Environment");

        // ---------- Lantai ----------
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(env.transform);
        ground.transform.localScale = new Vector3(3, 1, 3);
        ground.GetComponent<Renderer>().sharedMaterial = mFloor;

        var grid = Group(env, "FloorGrid");
        for (int i = -14; i <= 14; i += 2)
        {
            if (i == 0) continue;
            Deco(grid, $"LineX_{i}", PrimitiveType.Cube, new Vector3(i, 0.005f, 0), new Vector3(0.05f, 0.01f, 30f), mGrid);
            Deco(grid, $"LineZ_{i}", PrimitiveType.Cube, new Vector3(0, 0.005f, i), new Vector3(30f, 0.01f, 0.05f), mGrid);
        }
        // Garis tengah & lingkaran pusat
        Deco(grid, "CenterLineX", PrimitiveType.Cube, new Vector3(0, 0.008f, 0), new Vector3(30f, 0.01f, 0.12f), mGlowCyan);
        Deco(grid, "CenterLineZ", PrimitiveType.Cube, new Vector3(0, 0.008f, 0), new Vector3(0.12f, 0.01f, 30f), mGlowCyan);
        Deco(grid, "CenterRing", PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0), new Vector3(4f, 0.004f, 4f), mPad);

        // ---------- Tembok keliling arena ----------
        var perim = Group(env, "PerimeterWalls");
        float h = 2.5f, t = 1f, len = Half * 2 + t;
        Solid(perim, "Wall_North", new Vector3(0, h / 2, Half + t / 2), new Vector3(len, h, t), mWall);
        Solid(perim, "Wall_South", new Vector3(0, h / 2, -Half - t / 2), new Vector3(len, h, t), mWall);
        Solid(perim, "Wall_East",  new Vector3(Half + t / 2, h / 2, 0), new Vector3(t, h, len), mWall);
        Solid(perim, "Wall_West",  new Vector3(-Half - t / 2, h / 2, 0), new Vector3(t, h, len), mWall);
        // List neon di atas tembok
        Deco(perim, "Trim_North", PrimitiveType.Cube, new Vector3(0, h + 0.05f, Half + t / 2), new Vector3(len, 0.1f, 1.05f), mGlowCyan);
        Deco(perim, "Trim_South", PrimitiveType.Cube, new Vector3(0, h + 0.05f, -Half - t / 2), new Vector3(len, 0.1f, 1.05f), mGlowCyan);
        Deco(perim, "Trim_East",  PrimitiveType.Cube, new Vector3(Half + t / 2, h + 0.05f, 0), new Vector3(1.05f, 0.1f, len), mGlowCyan);
        Deco(perim, "Trim_West",  PrimitiveType.Cube, new Vector3(-Half - t / 2, h + 0.05f, 0), new Vector3(1.05f, 0.1f, len), mGlowCyan);
        // Menara sudut
        int c = 0;
        foreach (var sx in new[] { -1, 1 })
        foreach (var sz in new[] { -1, 1 })
        {
            var p = new Vector3(sx * (Half + 0.5f), 0, sz * (Half + 0.5f));
            Solid(perim, $"CornerTower_{++c}", p + Vector3.up * 1.75f, new Vector3(2f, 3.5f, 2f), mTrim);
            Deco(perim, $"CornerBeacon_{c}", PrimitiveType.Cylinder, p + Vector3.up * 3.7f, new Vector3(0.8f, 0.2f, 0.8f), mGlowOrange);
            Light(perim, $"CornerLight_{c}", p + Vector3.up * 4.2f, new Color(1f, 0.55f, 0.2f), 9f, 2.5f);
        }

        // ---------- Obstacle utama sesuai modul ----------
        var obs = Group(env, "Obstacles");
        Cover(obs, "Wall01", new Vector3(0, 1, 3),   new Vector3(6, 2, 1));
        Cover(obs, "Wall02", new Vector3(-4, 1, -2), new Vector3(1, 2, 5));
        Cover(obs, "Wall03", new Vector3(4, 1, -4),  new Vector3(4, 2, 1));

        // ---------- Pilar & cover tambahan ----------
        var props = Group(env, "Props");
        Pillar(props, "Pillar01", new Vector3(-10, 0, 4));
        Pillar(props, "Pillar02", new Vector3(10, 0, -1));
        Pillar(props, "Pillar03", new Vector3(-3, 0, -10));
        Pillar(props, "Pillar04", new Vector3(3, 0, 10));

        Crate(props, "Crate01", new Vector3(10.5f, 0.6f, -10f), 1.2f, 15);
        Crate(props, "Crate02", new Vector3(11.6f, 0.45f, -9.2f), 0.9f, -10);
        Crate(props, "Crate03", new Vector3(10.9f, 1.65f, -9.9f), 0.9f, 35);
        Crate(props, "Crate04", new Vector3(-11f, 0.6f, 10.5f), 1.2f, 25);
        Crate(props, "Crate05", new Vector3(-9.8f, 0.45f, 11.3f), 0.9f, 5);
        Crate(props, "Crate07", new Vector3(8f, 0.5f, 3.5f), 1f, -20);

        Cover(props, "LowCover01", new Vector3(-8f, 0.5f, -5f), new Vector3(3f, 1f, 0.6f));
        Cover(props, "LowCover02", new Vector3(12f, 0.5f, 5f),  new Vector3(0.6f, 1f, 3f));

        // ---------- Navigation ----------
        var nav = new GameObject("Navigation");
        nav.transform.SetParent(env.transform);
        var surface = nav.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = BakeMask();

        // ---------- Patrol points ----------
        var patrol = new GameObject("PatrolPoints");
        Vector3[] wp = { new(-7, 0, -7), new(-7, 0, 7), new(7, 0, 7), new(7, 0, -7) };
        for (int i = 0; i < wp.Length; i++)
        {
            var w = new GameObject($"Waypoint0{i + 1}");
            w.transform.SetParent(patrol.transform);
            w.transform.position = wp[i];
            SetIcon(w, "sv_label_1");
            Deco(w, "Marker", PrimitiveType.Cylinder, wp[i] + Vector3.up * 0.01f, new Vector3(1.2f, 0.01f, 1.2f), mGlowCyan);
            Deco(w, "MarkerCore", PrimitiveType.Cylinder, wp[i] + Vector3.up * 0.02f, new Vector3(0.6f, 0.01f, 0.6f), mFloor);
        }

        // ---------- Safe point (bunker kecil) ----------
        var safe = new GameObject("SafePoint");
        safe.transform.position = new Vector3(-10, 0, -10);
        SetIcon(safe, "sv_label_3");
        var bunker = Group(env, "SafeZoneBunker");
        Deco(bunker, "SafePad", PrimitiveType.Cylinder, new Vector3(-10, 0.01f, -10), new Vector3(3f, 0.01f, 3f), mGlowGreen);
        Solid(bunker, "Bunker_Back", new Vector3(-12.3f, 0.9f, -10f), new Vector3(0.5f, 1.8f, 4.5f), mTrim);
        Solid(bunker, "Bunker_Side", new Vector3(-10.3f, 0.9f, -12.3f), new Vector3(4.5f, 1.8f, 0.5f), mTrim);
        Light(bunker, "SafeLight", new Vector3(-10, 2.5f, -10), new Color(0.3f, 1f, 0.5f), 6f, 2f);

        // ---------- Lighting & kamera ----------
        var sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(50, -35, 0);
            var l = sun.GetComponent<Light>();
            l.color = new Color(0.85f, 0.9f, 1f);
            l.intensity = 1.1f;
            l.shadows = LightShadows.Soft;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.25f, 0.32f, 0.45f);
        RenderSettings.ambientEquatorColor = new Color(0.15f, 0.17f, 0.22f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.06f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.07f, 0.09f, 0.13f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 80f;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 24, -22);
            cam.transform.rotation = Quaternion.Euler(52, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.13f);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);

        surface.BuildNavMesh();
        string navPath = "Assets/Scenes/P06_BehaviorTree_NavMesh.asset";
        AssetDatabase.DeleteAsset(navPath);
        AssetDatabase.CreateAsset(surface.navMeshData, navPath);
        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[P06] Arena selesai dibuat & NavMesh sudah di-bake: " + ScenePath);
    }

    // Bake ulang NavMesh tanpa membangun ulang scene (aman dipakai setelah ada Player/Enemy).
    [MenuItem("Tools/Praktikum 06/Rebake NavMesh")]
    public static void Rebake()
    {
        var surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null) { Debug.LogError("[P06] NavMeshSurface tidak ditemukan."); return; }
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = BakeMask();
        surface.BuildNavMesh();
        string navPath = "Assets/Scenes/P06_BehaviorTree_NavMesh.asset";
        AssetDatabase.DeleteAsset(navPath);
        AssetDatabase.CreateAsset(surface.navMeshData, navPath);
        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[P06] NavMesh di-bake ulang (Player & Enemy diabaikan).");
    }

    // Player & Enemy tidak boleh ikut di-bake (kalau ikut, NavMesh berlubang di posisi mereka).
    static LayerMask BakeMask()
    {
        int mask = ~0;
        int pl = LayerMask.NameToLayer("Player"), en = LayerMask.NameToLayer("Enemy");
        if (pl >= 0) mask &= ~(1 << pl);
        if (en >= 0) mask &= ~(1 << en);
        return mask;
    }

    // ================= helpers =================
    static GameObject Group(GameObject parent, string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent.transform);
        return g;
    }

    // Objek padat: punya collider, layer Obstacle, menghalangi NavMesh & penglihatan
    static GameObject Solid(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat, float yRot = 0)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name = name;
        o.transform.SetParent(parent.transform);
        o.transform.position = pos;
        o.transform.rotation = Quaternion.Euler(0, yRot, 0);
        o.transform.localScale = scale;
        o.layer = obstacleLayer;
        o.GetComponent<Renderer>().sharedMaterial = mat;
        GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
        return o;
    }

    // Dekorasi: tanpa collider, tidak memengaruhi NavMesh/raycast
    static GameObject Deco(GameObject parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
    {
        var o = GameObject.CreatePrimitive(type);
        o.name = name;
        Object.DestroyImmediate(o.GetComponent<Collider>());
        o.transform.SetParent(parent.transform);
        o.transform.position = pos;
        o.transform.localScale = scale;
        var r = o.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return o;
    }

    static void Cover(GameObject parent, string name, Vector3 pos, Vector3 scale)
    {
        var o = Solid(parent, name, pos, scale, mCover);
        // garis neon di atas cover
        Deco(o.transform.parent.gameObject, name + "_Trim", PrimitiveType.Cube,
             pos + Vector3.up * (scale.y / 2 + 0.03f), new Vector3(scale.x + 0.02f, 0.06f, scale.z + 0.02f), mGlowOrange);
    }

    static void Pillar(GameObject parent, string name, Vector3 basePos)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = name;
        p.transform.SetParent(parent.transform);
        p.transform.position = basePos + Vector3.up * 1.5f;
        p.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
        p.layer = obstacleLayer;
        p.GetComponent<Renderer>().sharedMaterial = mPillar;
        GameObjectUtility.SetStaticEditorFlags(p, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
        Deco(parent, name + "_Ring", PrimitiveType.Cylinder, basePos + Vector3.up * 2.2f, new Vector3(1.3f, 0.06f, 1.3f), mGlowCyan);
        Deco(parent, name + "_Base", PrimitiveType.Cylinder, basePos + Vector3.up * 0.1f, new Vector3(1.6f, 0.1f, 1.6f), mTrim);
    }

    static void Crate(GameObject parent, string name, Vector3 pos, float size, float yRot)
    {
        Solid(parent, name, pos, Vector3.one * size, mCrate, yRot);
    }

    static void Light(GameObject parent, string name, Vector3 pos, Color col, float range, float intensity)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent.transform);
        g.transform.position = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col;
        l.range = range;
        l.intensity = intensity;
    }

    static void CreateMaterials()
    {
        mFloor      = Mat("M_Floor",      new Color(0.13f, 0.15f, 0.19f), 0.35f, 0.2f);
        mGrid       = Mat("M_FloorGrid",  new Color(0.22f, 0.27f, 0.34f), 0.2f, 0f);
        mWall       = Mat("M_Wall",       new Color(0.30f, 0.33f, 0.40f), 0.4f, 0.3f);
        mTrim       = Mat("M_Trim",       new Color(0.18f, 0.20f, 0.25f), 0.6f, 0.6f);
        mCover      = Mat("M_Cover",      new Color(0.42f, 0.45f, 0.52f), 0.35f, 0.2f);
        mPillar     = Mat("M_Pillar",     new Color(0.25f, 0.28f, 0.34f), 0.6f, 0.5f);
        mCrate      = Mat("M_Crate",      new Color(0.62f, 0.42f, 0.22f), 0.15f, 0f);
        mPad        = Mat("M_CenterPad",  new Color(0.10f, 0.20f, 0.28f), 0.5f, 0f);
        mGlowCyan   = Mat("M_GlowCyan",   new Color(0.1f, 0.8f, 1f), 0.5f, 0f, new Color(0.1f, 0.8f, 1f) * 2.5f);
        mGlowOrange = Mat("M_GlowOrange", new Color(1f, 0.5f, 0.1f), 0.5f, 0f, new Color(1f, 0.45f, 0.1f) * 2.5f);
        mGlowGreen  = Mat("M_GlowGreen",  new Color(0.2f, 1f, 0.4f), 0.5f, 0f, new Color(0.2f, 1f, 0.4f) * 1.8f);
    }

    static Material Mat(string name, Color col, float smooth, float metal, Color? emission = null)
    {
        string path = $"Assets/Materials/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = m == null;
        if (isNew)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
        }
        m.SetColor("_BaseColor", col);
        m.color = col;
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (isNew) AssetDatabase.CreateAsset(m, path);
        else EditorUtility.SetDirty(m);
        return m;
    }

    static void SetIcon(GameObject go, string icon)
    {
        var content = EditorGUIUtility.IconContent(icon);
        if (content?.image is Texture2D tex) EditorGUIUtility.SetIconForObject(go, tex);
    }

    static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing != -1) return existing;
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var p = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(p.stringValue))
            {
                p.stringValue = name;
                tm.ApplyModifiedProperties();
                return i;
            }
        }
        Debug.LogError("Tidak ada slot layer kosong untuk " + name);
        return 0;
    }

    static void AddSceneToBuild(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == path)) return;
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
