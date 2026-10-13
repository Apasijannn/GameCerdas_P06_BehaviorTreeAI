using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

// Bonus F — On-screen BT Debugger (Praktikum 06, bagian 75).
// Add Component "BTDebugger" ke Enemy. Saat Play, tree AI digambar di layar secara real-time:
//   RUNNING = kuning, SUCCESS = hijau, FAILURE = merah, abu-abu = tidak dievaluasi frame ini.
// Tekan F1 untuk menampilkan / menyembunyikan panel.
// Mendukung EnemyBTController, EnemyHybridController (tree) dan EnemyUtilityController (skor).
public class BTDebugger : MonoBehaviour
{
    [SerializeField] private bool showPanel = true;
    [SerializeField] private bool showWorldLabel = true;
    [Tooltip("Panel ditumpuk atas-bawah di tepi layar ini.")]
    [SerializeField] private bool anchorRight = false;
    [SerializeField] private float margin = 8f;
    [SerializeField] private float panelWidth = 230f;
    [SerializeField] private int fontSize = 10;
    [Tooltip("Compact: hanya tampilkan node yang dievaluasi frame ini (jalur aktif).")]
    [SerializeField] private bool compactTree = true;

    private MonoBehaviour controller;
    private FieldInfo rootField, actionField, combatField, canSeeField;
    private IHealth health;

    private GUIStyle boxStyle, lineStyle, titleStyle, labelStyle;
    private float lastPanelHeight;
    private readonly List<(string text, Color color)> lines = new List<(string, Color)>();

    private static readonly Color Running = new Color(1f, 0.85f, 0.2f);
    private static readonly Color Success = new Color(0.35f, 1f, 0.45f);
    private static readonly Color Failure = new Color(1f, 0.4f, 0.35f);
    private static readonly Color Idle    = new Color(0.55f, 0.55f, 0.6f);

    // Beberapa enemy dengan debugger: panel disusun berdampingan
    private static readonly List<BTDebugger> active = new List<BTDebugger>();
    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    private void Start() => FindController();

    // Ambil controller AI yang sedang aktif & baca field private-nya lewat reflection
    // (supaya controller tidak perlu diubah).
    private void FindController()
    {
        controller = null;
        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (mb == this || !mb.enabled) continue;
            System.Type t = mb.GetType();
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo action = t.GetField("currentAction", F);
            if (action == null) continue;

            controller = mb;
            actionField = action;
            rootField = t.GetField("rootNode", F);
            combatField = t.GetField("combatSelector", F);
            canSeeField = t.GetField("canSeePlayer", F) ?? t.GetField("canSeeNow", F);
            health = mb as IHealth;
            break;
        }
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame)
            showPanel = !showPanel;

        if (controller == null || !controller.enabled)
            FindController();
    }

    private void OnGUI()
    {
        if (controller == null) return;
        InitStyles();

        string action = actionField.GetValue(controller) as string;

        if (showWorldLabel && !ZoomCamera.IsCloseUp)
            DrawWorldLabel(action);

        if (!showPanel) return;

        // ---------- Susun isi panel ----------
        lines.Clear();
        lines.Add(($"{action}", ActionColor(action)));
        string hp = health != null ? $"HP {health.CurrentHealth}/{health.MaxHealth}" : "";
        string see = canSeeField != null ? $"   See: {((bool)canSeeField.GetValue(controller) ? "YES" : "no")}" : "";
        lines.Add((hp + see, Color.white));

        BTNode root = rootField?.GetValue(controller) as BTNode;
        if (root != null)
        {
            var combat = combatField?.GetValue(controller) as UtilitySelectorNode;
            AddNode(root, 0, true, combat);
        }
        else
        {
            AddUtilityScores();   // EnemyUtilityController (tanpa tree)
        }

        // ---------- Gambar (ditumpuk atas-bawah) ----------
        float lh = fontSize + 4;
        float height = lines.Count * lh + lh + 10;
        lastPanelHeight = height;

        float y0 = margin;
        int idx = active.IndexOf(this);
        for (int i = 0; i < idx; i++)
            if (active[i].showPanel && active[i].controller != null)
                y0 += active[i].lastPanelHeight + margin;

        float x0 = anchorRight ? Screen.width - panelWidth - margin : margin;
        Rect box = new Rect(x0, y0, panelWidth, height);
        GUI.Box(box, GUIContent.none, boxStyle);
        GUI.Label(new Rect(box.x + 6, box.y + 3, box.width, lh), $"{name}  <{controller.GetType().Name.Replace("Enemy", "").Replace("Controller", "")}>", titleStyle);

        float y = box.y + 3 + lh;
        foreach (var (text, color) in lines)
        {
            lineStyle.normal.textColor = color;
            GUI.Label(new Rect(box.x + 6, y, box.width - 12, lh), text, lineStyle);
            y += lh;
        }
    }

    // Rekursif: tulis node + anak-anaknya dengan indentasi
    private void AddNode(BTNode node, int depth, bool isRoot, UtilitySelectorNode combat)
    {
        bool ticked = node.WasTickedThisFrame || (isRoot && AnyChildTicked(node));
        if (compactTree && !ticked) return;
        string state = ticked ? (isRoot && !node.WasTickedThisFrame ? "" : node.LastState.ToString().ToUpper()) : "-";
        Color color = !ticked ? Idle : node.LastState switch
        {
            NodeState.Running => Running,
            NodeState.Success => Success,
            _ => Failure
        };
        if (isRoot) color = Color.white;

        string indent = new string(' ', depth * 2);
        string bullet = depth == 0 ? "" : "└ ";
        string extra = "";
        if (combat != null && combat.TryGetScore(node, out float score, out string optName))
            extra = $"  [{optName} = {score:0.00}]";

        string label = node.DebugName.Replace("Condition: ", "? ").Replace("Action: ", "> ");
        if (isRoot) label = "Root " + label;
        state = state.Replace("RUNNING", "RUN").Replace("SUCCESS", "OK").Replace("FAILURE", "FAIL");
        lines.Add(($"{indent}{bullet}{label}{extra}   {state}", color));

        foreach (BTNode child in node.DebugChildren)
            AddNode(child, depth + 1, false, combat);
    }

    private static bool AnyChildTicked(BTNode node)
    {
        foreach (BTNode c in node.DebugChildren)
            if (c.WasTickedThisFrame) return true;
        return false;
    }

    private void AddUtilityScores()
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        foreach (string f in new[] { "attackScore", "chaseScore", "fleeScore", "patrolScore" })
        {
            FieldInfo fi = controller.GetType().GetField(f, F);
            if (fi == null) continue;
            float v = (float)fi.GetValue(controller);
            string bar = new string('|', Mathf.RoundToInt(v * 12));
            lines.Add(($"{f.Replace("Score", ""),-7}{v:0.00} {bar}", Color.Lerp(Idle, Running, v)));
        }
    }

    private void DrawWorldLabel(string action)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 2.3f);
        if (sp.z < 0) return;
        labelStyle.normal.textColor = ActionColor(action);
        Vector2 size = labelStyle.CalcSize(new GUIContent(action));
        GUI.Label(new Rect(sp.x - size.x / 2, Screen.height - sp.y - size.y, size.x, size.y), action, labelStyle);
    }

    private static Color ActionColor(string action) => action switch
    {
        "ATTACK" => Failure,
        "FLEE" => new Color(0.6f, 0.8f, 1f),
        "CHASE" => Running,
        "TAKE COVER" => new Color(0.4f, 1f, 1f),
        "SEARCH" => new Color(1f, 0.6f, 1f),
        _ => Success
    };

    private void InitStyles()
    {
        if (boxStyle != null) return;
        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0.03f, 0.05f, 0.08f, 0.72f));
        bg.Apply();
        boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg } };
        lineStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = false };
        lineStyle.font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New" }, fontSize);
        titleStyle = new GUIStyle(lineStyle) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.3f, 0.85f, 1f) } };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize + 1, fontStyle = FontStyle.Bold };
    }
}
