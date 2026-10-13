// Setup Animator untuk Player (Mixamo, Humanoid) dan Enemy (Realistic Cat, Generic).
// Menu: Tools > Praktikum 06 > Setup Animators
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class P06AnimatorSetup
{
    const string PlayerDir = "Assets/Materials/Player/";
    const string AnimDir = "Assets/Animations/";
    const string CatPrefab = "Assets/Realistic Cat Demo/Prefabs/URP/HD/Cat_05.prefab";
    const string CatAnimDir = "Assets/Realistic Cat Demo/Cats/Cat_05/Animations/";

    [MenuItem("Tools/Praktikum 06/Setup Animators")]
    public static void SetupAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        SetupPlayer();
        foreach (string n in new[] { "AggressiveEnemy", "CowardEnemy", "Enemy" })
        {
            GameObject e = FindInScene(n);
            if (e != null) SetupCatEnemy(e);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[P06] Animator Player & Enemy selesai dipasang.");
    }

    // ==================================================
    // PLAYER
    // ==================================================

    static void SetupPlayer()
    {
        string idlePath = PlayerDir + "PlayerIdle.fbx";

        // 0. Ekstrak tekstur yang tertanam di PlayerIdle.fbx (skin karakter)
        ExtractPlayerTextures(idlePath);

        // 1. Import setting: Humanoid, avatar dari PlayerIdle
        var idleImp = (ModelImporter)AssetImporter.GetAtPath(idlePath);
        idleImp.animationType = ModelImporterAnimationType.Human;
        idleImp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        ConfigureClip(idleImp, true);
        idleImp.SaveAndReimport();
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(idlePath).OfType<Avatar>().First();

        foreach (string n in new[] { "PlayerWalking", "PlayerRun", "PlayerHit", "PlayerDeath" })
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(PlayerDir + n + ".fbx");
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar = avatar;
            ConfigureClip(imp, n == "PlayerWalking" || n == "PlayerRun");
            imp.SaveAndReimport();
        }

        // 2. Animator Controller
        string ctrlPath = AnimDir + "Player.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Running", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        var sm = ctrl.layers[0].stateMachine;

        var loco = BuildLocomotion(ctrl, sm,
            Clip(PlayerDir + "PlayerIdle.fbx"),
            Clip(PlayerDir + "PlayerWalking.fbx"),
            Clip(PlayerDir + "PlayerRun.fbx"));

        var hit = sm.AddState("Hit");
        hit.motion = Clip(PlayerDir + "PlayerHit.fbx");
        var t1 = sm.AddAnyStateTransition(hit);
        t1.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        t1.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
        t1.duration = 0.1f; t1.canTransitionToSelf = false;
        var back = hit.AddTransition(loco); back.hasExitTime = true; back.exitTime = 0.85f; back.duration = 0.15f;

        var death = sm.AddState("Death");
        death.motion = Clip(PlayerDir + "PlayerDeath.fbx");
        var t2 = sm.AddAnyStateTransition(death);
        t2.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        t2.duration = 0.1f; t2.canTransitionToSelf = false;

        // 3. Model di scene
        GameObject player = FindInScene("Player");
        if (player == null) { Debug.LogError("[P06] Player tidak ditemukan."); return; }

        Transform old = player.transform.Find("PlayerModel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(idlePath), player.transform);
        model.name = "PlayerModel";
        model.transform.localPosition = new Vector3(0, -1f, 0);   // kaki di lantai (Player di y = 1)
        model.transform.localRotation = Quaternion.identity;

        var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.avatar = avatar;
        anim.applyRootMotion = false;

        var mr = player.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        var driver = player.GetComponent<PlayerAnimatorDriver>() ?? player.AddComponent<PlayerAnimatorDriver>();
        SetRef(driver, "animator", anim);
    }

    static void ExtractPlayerTextures(string fbxPath)
    {
        string texDir = PlayerDir + "Textures";
        if (!AssetDatabase.IsValidFolder(texDir))
            AssetDatabase.CreateFolder(PlayerDir.TrimEnd('/'), "Textures");

        var imp = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        imp.ExtractTextures(texDir);
        AssetDatabase.Refresh();

        // Normal map harus bertipe NormalMap
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { texDir }))
        {
            string tp = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
            if (tp.Contains("Normal") && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
        }

        // Material dibuat ulang dari deskripsi FBX → otomatis URP Lit + tekstur
        imp = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        imp.SaveAndReimport();
    }

    static void ConfigureClip(ModelImporter imp, bool loop)
    {
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.loopTime = loop;
            c.lockRootRotation = true;         // Bake Into Pose
            c.lockRootHeightY = true;
            c.lockRootPositionXZ = true;
            c.keepOriginalPositionY = false;
            c.heightFromFeet = true;
        }
        imp.clipAnimations = clips;
    }

    static AnimationClip Clip(string fbx) =>
        AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));

    // ==================================================
    // ENEMY (CAT)
    // ==================================================

    static AnimatorController catCtrl;

    static AnimatorController GetCatController()
    {
        if (catCtrl != null) return catCtrl;

        string path = AnimDir + "EnemyCat.controller";
        AssetDatabase.DeleteAsset(path);
        catCtrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        catCtrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        catCtrl.AddParameter("Running", AnimatorControllerParameterType.Bool);
        catCtrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        catCtrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        var sm = catCtrl.layers[0].stateMachine;

        var loco = BuildLocomotion(catCtrl, sm,
            CatClip("Idle", true), CatClip("Walk", true), CatClip("Run", true));

        var atk = sm.AddState("Attack");
        atk.motion = CatClip("Attack_L", false);
        var ta = sm.AddAnyStateTransition(atk);
        ta.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        ta.duration = 0.1f; ta.canTransitionToSelf = false;
        var tb = atk.AddTransition(loco); tb.hasExitTime = true; tb.exitTime = 0.9f; tb.duration = 0.15f;

        var hit = sm.AddState("Hit");
        hit.motion = CatClip("Get_Hit", false);
        var th = sm.AddAnyStateTransition(hit);
        th.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        th.duration = 0.1f; th.canTransitionToSelf = false;
        var thb = hit.AddTransition(loco); thb.hasExitTime = true; thb.exitTime = 0.85f; thb.duration = 0.15f;

        return catCtrl;
    }

    // Locomotion: Idle <-> Walk (default, berdasarkan Speed). Run HANYA saat bool "Running" = true.
    static AnimatorState BuildLocomotion(AnimatorController ctrl, AnimatorStateMachine sm,
                                         AnimationClip idle, AnimationClip walk, AnimationClip run)
    {
        var loco = ctrl.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, 0.3f);
        sm.defaultState = loco;

        var runState = sm.AddState("Run");
        runState.motion = run;

        var toRun = loco.AddTransition(runState);
        toRun.hasExitTime = false; toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.If, 0, "Running");
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var back1 = runState.AddTransition(loco);
        back1.hasExitTime = false; back1.duration = 0.2f;
        back1.AddCondition(AnimatorConditionMode.IfNot, 0, "Running");

        var back2 = runState.AddTransition(loco);
        back2.hasExitTime = false; back2.duration = 0.2f;
        back2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        return loco;
    }

    static AnimationClip CatClip(string name, bool loop)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CatAnimDir + name + ".anim");
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        if (s.loopTime != loop)
        {
            s.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            EditorUtility.SetDirty(clip);
        }
        return clip;
    }

    static void SetupCatEnemy(GameObject enemy)
    {
        Transform old = enemy.transform.Find("CatModel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPrefab);
        var cat = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemy.transform);
        cat.name = "CatModel";
        cat.transform.localRotation = Quaternion.identity;
        cat.transform.localScale = Vector3.one;
        cat.transform.localPosition = Vector3.zero;

        // Skala: kucing "raksasa" sepanjang ~1.8 unit agar sebanding dengan collider Enemy
        Bounds b = WorldBounds(cat);
        float length = Mathf.Max(b.size.x, b.size.z);
        float scale = length > 0.001f ? 1.8f / length : 1f;
        cat.transform.localScale = Vector3.one * scale;

        b = WorldBounds(cat);
        float floorY = enemy.transform.position.y - 1f;     // kaki di lantai
        cat.transform.position += Vector3.up * (floorY - b.min.y);

        var anim = cat.GetComponent<Animator>() ?? cat.GetComponentInChildren<Animator>() ?? cat.AddComponent<Animator>();
        anim.runtimeAnimatorController = GetCatController();
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Komponen showcase demo tidak dipakai
        foreach (var mb in cat.GetComponentsInChildren<MonoBehaviour>())
            if (mb != null && mb.GetType().Name == "CatShowcase") Object.DestroyImmediate(mb);

        var mr = enemy.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Penanda personality: cincin warna di bawah kucing
        Transform oldRing = enemy.transform.Find("PersonalityRing");
        if (oldRing != null) Object.DestroyImmediate(oldRing.gameObject);
        string matPath = enemy.name == "AggressiveEnemy" ? "Assets/Materials/M_EnemyAggressive.mat"
                       : enemy.name == "CowardEnemy" ? "Assets/Materials/M_EnemyCoward.mat" : null;
        var mat = matPath != null ? AssetDatabase.LoadAssetAtPath<Material>(matPath) : null;
        if (mat != null)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "PersonalityRing";
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(enemy.transform, false);
            ring.transform.localPosition = new Vector3(0, -0.98f, 0);
            ring.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);
            ring.GetComponent<Renderer>().sharedMaterial = mat;
            ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Lampu sorot kecil di atas kucing (ikut bergerak bersama enemy)
        Transform oldLight = enemy.transform.Find("CatSpotLight");
        if (oldLight != null) Object.DestroyImmediate(oldLight.gameObject);
        var lgo = new GameObject("CatSpotLight");
        lgo.transform.SetParent(enemy.transform, false);
        lgo.transform.localPosition = new Vector3(0, 3.5f, -0.6f);
        lgo.transform.localRotation = Quaternion.Euler(80f, 0, 0);
        var light = lgo.AddComponent<Light>();
        light.type = LightType.Spot;
        light.range = 7f;
        light.spotAngle = 55f;
        light.innerSpotAngle = 30f;
        light.intensity = 6f;
        light.color = new Color(1f, 0.95f, 0.88f);
        light.shadows = LightShadows.None;

        // Selalu pakai LOD terbaik (kamera game cukup jauh)
        var lod = cat.GetComponentInChildren<LODGroup>();
        if (lod != null) lod.ForceLOD(0);

        var driver = enemy.GetComponent<EnemyAnimatorDriver>() ?? enemy.AddComponent<EnemyAnimatorDriver>();
        SetRef(driver, "animator", anim);
    }

    // ==================================================
    // VISUAL QUALITY
    // ==================================================

    [MenuItem("Tools/Praktikum 06/Enhance Visuals (HD Cat + Quality)")]
    public static void EnhanceVisuals()
    {
        // 1. Kucing HD + lampu sorot (setup ulang enemy)
        foreach (string n in new[] { "AggressiveEnemy", "CowardEnemy", "Enemy" })
        {
            GameObject e = FindInScene(n);
            if (e != null) SetupCatEnemy(e);
        }

        // 2. Ambient sedikit lebih terang
        RenderSettings.ambientSkyColor = new Color(0.36f, 0.43f, 0.58f);
        RenderSettings.ambientEquatorColor = new Color(0.24f, 0.26f, 0.32f);
        RenderSettings.ambientGroundColor = new Color(0.10f, 0.10f, 0.12f);

        // 3. Tekstur Player & kucing: resolusi penuh, kompresi kualitas tinggi
        TuneTextures(PlayerDir + "Textures");
        TuneTextures("Assets/Realistic Cat Demo/Shared/Textures");
        TuneTextures("Assets/Realistic Cat Demo/Cats/Cat_05/Textures");

        // 4. Skinned mesh Player: 4 bone weights
        GameObject player = FindInScene("Player");
        if (player != null)
            foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                smr.quality = SkinQuality.Bone4;

        // 5. Render pipeline: MSAA 4x, render scale 1, shadow 4096
        foreach (string rp in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(rp);
            if (asset == null) continue;
            var so = new SerializedObject(asset);
            so.FindProperty("m_MSAA").intValue = 4;
            so.FindProperty("m_RenderScale").floatValue = 1f;
            so.FindProperty("m_MainLightShadowmapResolution").intValue = 4096;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 6. Quality settings
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.lodBias = 3f;
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.skinWeights = SkinWeights.FourBones;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[P06] Visual ditingkatkan: kucing HD, lampu sorot, ambient, tekstur & kualitas render.");
    }

    static void TuneTextures(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.anisoLevel = 8;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
    }

    // ==================================================
    // HELPERS
    // ==================================================

    static Bounds WorldBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject FindInScene(string name)
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root;
            var t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
            if (t != null) return t.gameObject;
        }
        return null;
    }
}
