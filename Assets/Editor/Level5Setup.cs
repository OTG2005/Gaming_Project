#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click setup for Level 5 (menu:  Dragon Hunt > Setup Level 5).
/// Safe to run again: it only adds what is missing and re-assigns the references.
///
/// What it does:
///  - Crossbow Hunter: Enemy layer/tag, Rigidbody2D, CrossbowHunter script, bolt prefab, fire point,
///    glint, all sounds.
///  - Warlord: Enemy layer/tag, Rigidbody2D, Warlord script, all sounds, a boss health bar on the Canvas.
///  - Freya: Freya_Controller + FreyaCallout (with a speech bubble), linked to Erik.
///  - Erik: health bar + lives UI linked. Adds a SoundManager (boss music).
///  - Creates the CrossbowArmy and WarlordArmy prefabs and places a few soldiers on the ground.
///  - Adds Level_5 to File > Build Settings.
/// </summary>
public static class Level5Setup
{
    const string ScenePath   = "Assets/Scenes/Level_5.unity";
    const string Chars       = "Assets/dragon hunt assests/characters/";
    const string CrossDir    = Chars + "crossbow_hunter/";
    const string WarDir      = Chars + "warlord/";
    const string BoltPrefab  = "Assets/prefab/CrossbowBolt.prefab";
    const string CrossArmyPrefab = "Assets/prefab/CrossbowArmy.prefab";
    const string WarArmyPrefab   = "Assets/prefab/WarlordArmy.prefab";

    static readonly List<string> log = new List<string>();

    [MenuItem("Dragon Hunt/Setup Level 5")]
    public static void Run()
    {
        log.Clear();

        // ---- open Level 5 (asks to save the current scene first if needed)
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        LayerMask ground = LayerMask.GetMask("Ground");
        LayerMask enemies = LayerMask.GetMask("Enemy");
        if (enemyLayer < 0) { Debug.LogError("Level5Setup: there is no 'Enemy' layer."); return; }

        // ---- find the objects in the scene (including switched-off ones)
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToList();
        GameObject erik    = all.FirstOrDefault(g => g.GetComponent<PlayerController>() != null);
        GameObject freya   = all.FirstOrDefault(g => g.name.ToLower().StartsWith("freya") && g.GetComponent<SpriteRenderer>() != null);
        GameObject hunter  = all.FirstOrDefault(g => g.name.StartsWith("crossbow_hunter_spritesheet"));
        GameObject warlord = all.FirstOrDefault(g => g.name.StartsWith("warlord_spritesheet"));
        Canvas canvas      = all.Select(g => g.GetComponent<Canvas>()).FirstOrDefault(c => c != null && c.isRootCanvas);

        if (erik == null) { Debug.LogError("Level5Setup: Erik (PlayerController) not found in Level 5."); return; }

        // ---- Erik: link his UI
        var erikHealth = erik.GetComponent<PlayerHealth>();
        if (erikHealth != null && canvas != null)
        {
            var bar = canvas.GetComponentsInChildren<HealthBarUI>(true).FirstOrDefault(h => h.name != "BossBar");
            if (bar != null) erikHealth.healthBar = bar;
            var lives = canvas.GetComponentInChildren<LivesUI>(true);
            if (lives != null) erikHealth.livesUI = lives;
            var gameOver = Object.FindObjectOfType<GameOverUI>(true);
            if (gameOver != null) erikHealth.gameOver = gameOver;
            Dirty(erikHealth);
            log.Add("Erik: health bar " + (bar != null) + ", lives " + (lives != null) + ", game over " + (gameOver != null));
        }

        // ---- shared assets
        var bolt = AssetDatabase.LoadAssetAtPath<GameObject>(BoltPrefab);
        var crossCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CrossDir + "crossbow_hunter_spritesheet_0.controller");
        var warCtrl   = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WarDir + "warlord_spritesheet_0.controller");
        var crossArmyCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CrossDir + "Army/crossbow_army.overrideController");
        var warArmyCtrl   = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WarDir + "Army/warlord_army.overrideController");
        var freyaCallout = (FreyaCallout)null;

        // ---- Freya
        if (freya != null)
        {
            SetupBody(freya, 0, ground, 1f);
            var fc = GetOrAdd<Freya_Controller>(freya);
            fc.player = erik.transform;
            fc.groundLayer = ground;
            fc.enemyLayers = enemies;

            freyaCallout = GetOrAdd<FreyaCallout>(freya);
            var bubble = freya.transform.Find("Bubble");
            if (bubble == null)
            {
                bubble = new GameObject("Bubble", typeof(RectTransform)).transform;
                bubble.SetParent(freya.transform, false);
            }
            bubble.localPosition = new Vector3(0f, 1.6f, 0f);
            var tmp = bubble.GetComponent<TextMeshPro>();
            if (tmp == null) tmp = bubble.gameObject.AddComponent<TextMeshPro>();
            tmp.text = "Freya";
            tmp.fontSize = 3.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.95f, 0.7f);
            tmp.rectTransform.sizeDelta = new Vector2(7f, 1.5f);
            var mr = bubble.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 50;
            freyaCallout.bubble = bubble.gameObject;
            freyaCallout.text = tmp;
            fc.callout = freyaCallout;
            Dirty(fc); Dirty(freyaCallout); Dirty(tmp);
            log.Add("Freya: Freya_Controller + speech bubble linked to Erik");
        }
        else log.Add("Freya NOT found (add the freya prefab to the scene and run again)");

        // ---- Crossbow Hunter
        if (hunter != null)
        {
            SetupBody(hunter, enemyLayer, ground, 3f);
            SetController(hunter, crossCtrl);
            var ch = GetOrAdd<CrossbowHunter>(hunter);
            ConfigureHunter(ch, bolt, ground, freyaCallout, false);
            ch.firePoint = GetOrCreateChild(hunter.transform, "FirePoint", new Vector3(0.5f, 0.2f, 0f));
            ch.glint = CreateGlint(hunter.transform, new Vector3(0.5f, 0.2f, 0f));
            Dirty(ch);
            log.Add("Crossbow Hunter: script, bolt, fire point, glint, sounds");
        }
        else log.Add("Crossbow Hunter NOT found");

        // ---- Warlord + boss bar
        if (warlord != null)
        {
            SetupBody(warlord, enemyLayer, ground, 10f);   // heavy: Erik can't push him around
            SetController(warlord, warCtrl);
            var w = GetOrAdd<Warlord>(warlord);
            ConfigureWarlord(w, ground, freyaCallout, true);
            if (canvas != null)
            {
                var bossBar = GetOrCreateBossBar(canvas);
                if (bossBar != null) { w.bossBar = bossBar; w.bossBarRoot = bossBar.gameObject; }
            }
            Dirty(w);
            log.Add("Warlord: script, sounds, boss bar " + (w.bossBar != null));
        }
        else log.Add("Warlord NOT found");

        // ---- army prefabs + a few soldiers in the level
        var crossArmy = BuildArmyPrefab(CrossArmyPrefab, "CrossbowArmy", Chars + "crossbow_hunter/crossbow_hunter_army_spritesheet.png",
                                        crossArmyCtrl, enemyLayer, ground, 1.4f, new Vector2(0.8f, 1.5f), Vector2.zero, isCrossbow: true, bolt: bolt);
        var warArmy   = BuildArmyPrefab(WarArmyPrefab, "WarlordArmy", Chars + "warlord/warlord_army_spritesheet.png",
                                        warArmyCtrl, enemyLayer, ground, 1.9f, new Vector2(0.7f, 1.4f), new Vector2(0f, -0.15f), isCrossbow: false, bolt: bolt);
        int sorting = hunter != null ? hunter.GetComponent<SpriteRenderer>().sortingOrder : 0;
        Place(crossArmy, "CrossbowArmy (1)", 33f, all, ground, sorting);
        Place(crossArmy, "CrossbowArmy (2)", 47f, all, ground, sorting);
        Place(warArmy,   "WarlordArmy (1)",  57f, all, ground, sorting);
        Place(warArmy,   "WarlordArmy (2)",  70f, all, ground, sorting);

        // ---- music
        var sm = Object.FindObjectOfType<SoundManager>(true);
        if (sm == null)
        {
            sm = new GameObject("SoundManager").AddComponent<SoundManager>();
            SceneManager.MoveGameObjectToScene(sm.gameObject, scene);
        }
        sm.musicName = "bgm_boss_warlord";
        sm.ambienceNames = new[] { "amb_wind_loop" };
        Dirty(sm);
        log.Add("SoundManager: music bgm_boss_warlord");

        // ---- build settings
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            log.Add("Level_5 added to Build Settings");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>Level 5 setup done</b>\n - " + string.Join("\n - ", log));
    }

    // ============================================================== characters
    static void ConfigureHunter(CrossbowHunter ch, GameObject bolt, LayerMask ground, FreyaCallout freya, bool army)
    {
        ch.boltPrefab = bolt;
        ch.groundLayer = ground;
        ch.freya = freya;
        ch.spriteFacesRight = true;
        ch.shootClip  = Clip(CrossDir + "Crossbow_Attack.wav");
        ch.damageClip = Clip(CrossDir + "Crossbow_Damage.wav");
        ch.tauntClip  = Clip(CrossDir + "Crossbow_taunt.wav");
        if (army)
        {
            ch.maxHealth = 25;            // one axe hit
            ch.noticeRange = 10f;
            ch.reloadTime = 1.8f;
            ch.introClip = null;          // only the named hunter gives the long intro / defeat lines
            ch.defeatClip = null;
        }
        else
        {
            ch.introClip  = Clip(CrossDir + "CrossBow_Hunter_Intro.wav");
            ch.defeatClip = Clip(CrossDir + "Crossbow_Defeat.wav");
        }
    }

    static void ConfigureWarlord(Warlord w, LayerMask ground, FreyaCallout freya, bool boss)
    {
        w.groundLayer = ground;
        w.freya = freya;
        w.spriteFacesRight = true;
        w.isBoss = boss;
        w.attackClip = Clip(WarDir + "Warlord_Mid_Attack.wav");
        w.damageClip = Clip(WarDir + "Warlord_Damage.wav");
        if (boss)
        {
            w.maxHealth = 250;            // 10 axe hits
            w.blockOutsideOpening = true;
            w.fadeOutWhenDefeated = false;
            w.introClip       = Clip(WarDir + "Warlord_Intro.wav");
            w.startAttackClip = Clip(WarDir + "Warlord_Initiate_Attck.wav");
            w.defeatClip      = Clip(WarDir + "Warlord_Defeat.wav");
        }
        else
        {
            // a spear soldier of the Warlord's army: weaker, slower, no blocking, no boss bar
            w.maxHealth = 75;
            w.blockOutsideOpening = false;
            w.fadeOutWhenDefeated = true;
            w.activateRange = 8f;
            w.runSpeed = 2.8f;
            w.canJump = false;
            w.strikeDamage = 15; w.lungeDamage = 10; w.sweepDamage = 15;
            w.strikeWindup = 0.8f; w.lungeWindup = 0.7f; w.sweepWindup = 0.9f;
            w.recoverTime = 1.2f;
            w.introClip = null; w.startAttackClip = null; w.defeatClip = null;
            w.damageClip = null;          // the boss voice would sound odd on every soldier
        }
    }

    static void SetupBody(GameObject go, int layer, LayerMask ground, float mass)
    {
        go.layer = layer;
        if (layer != 0) go.tag = "Enemy";
        var rb = GetOrAdd<Rigidbody2D>(go);
        rb.freezeRotation = true;
        rb.mass = mass;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (go.GetComponent<Collider2D>() == null) go.AddComponent<BoxCollider2D>();
        Dirty(rb);
    }

    static void SetController(GameObject go, RuntimeAnimatorController ctrl)
    {
        var anim = GetOrAdd<Animator>(go);
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        Dirty(anim);
    }

    // ============================================================== army
    static GameObject BuildArmyPrefab(string path, string name, string sheetPath, RuntimeAnimatorController ctrl,
                                      int layer, LayerMask ground, float scale, Vector2 colliderSize, Vector2 colliderOffset,
                                      bool isCrossbow, GameObject bolt)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().FirstOrDefault(s => s.name.EndsWith("_0"));
        if (sprite == null || ctrl == null) { log.Add(name + " prefab NOT made (sprite or controller missing)"); return null; }

        var go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = colliderSize;
        box.offset = colliderOffset;
        SetupBody(go, layer, ground, isCrossbow ? 3f : 5f);
        SetController(go, ctrl);

        if (isCrossbow)
        {
            var ch = go.AddComponent<CrossbowHunter>();
            ConfigureHunter(ch, bolt, ground, null, true);
            ch.firePoint = GetOrCreateChild(go.transform, "FirePoint", new Vector3(0.45f, 0.15f, 0f));
            ch.glint = CreateGlint(go.transform, new Vector3(0.45f, 0.15f, 0f));
        }
        else
        {
            var w = go.AddComponent<Warlord>();
            ConfigureWarlord(w, ground, null, false);
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        log.Add(name + " prefab saved: " + path);
        return prefab;
    }

    static void Place(GameObject prefab, string name, float x, List<GameObject> all, LayerMask ground, int sorting)
    {
        if (prefab == null || all.Any(g => g.name == name)) return;     // already placed
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.GetComponent<SpriteRenderer>().sortingOrder = sorting;

        // stand on the ground below x
        Physics2D.SyncTransforms();
        var hit = Physics2D.Raycast(new Vector2(x, 30f), Vector2.down, 100f, ground);
        float groundY = hit.collider != null ? hit.point.y : -3.45f;
        var box = go.GetComponent<BoxCollider2D>();
        float feet = (box.offset.y - box.size.y * 0.5f) * go.transform.localScale.y;
        go.transform.position = new Vector3(x, groundY - feet + 0.05f, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<SpriteRenderer>());
        log.Add("Placed " + name + " at x=" + x);
    }

    // ============================================================== helpers
    static HealthBarUI GetOrCreateBossBar(Canvas canvas)
    {
        var existing = canvas.GetComponentsInChildren<HealthBarUI>(true).FirstOrDefault(h => h.name == "BossBar");
        if (existing != null) return existing;

        // copy Erik's health bar so it gets the same frame / fill pictures
        var playerBar = canvas.GetComponentsInChildren<HealthBarUI>(true).FirstOrDefault();
        if (playerBar == null) return null;
        var copy = Object.Instantiate(playerBar.gameObject, canvas.transform);
        copy.name = "BossBar";
        var rt = copy.GetComponent<RectTransform>();
        Vector2 size = rt.rect.size;                 // real size before changing the anchors
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(Mathf.Max(size.x * 1.6f, 500f), Mathf.Max(size.y, 30f));

        // a red-black fill for the boss
        var bar = copy.GetComponent<HealthBarUI>();
        if (bar.fill != null) bar.fill.color = new Color(0.75f, 0.1f, 0.1f);

        // name label above the bar
        var label = new GameObject("BossName", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(copy.transform, false);
        label.text = "THE WARLORD";
        label.fontSize = 28;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.85f, 0.6f);
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(1f, 1f);
        lrt.pivot = new Vector2(0.5f, 0f);
        lrt.anchoredPosition = new Vector2(0f, 4f);
        lrt.sizeDelta = new Vector2(0f, 36f);

        copy.SetActive(false);          // the Warlord shows it when the fight starts
        log.Add("Boss bar created on the Canvas");
        return bar;
    }

    static Transform GetOrCreateChild(Transform parent, string name, Vector3 localPos)
    {
        var t = parent.Find(name);
        if (t == null) { t = new GameObject(name).transform; t.SetParent(parent, false); }
        t.localPosition = localPos;
        return t;
    }

    static SpriteRenderer CreateGlint(Transform parent, Vector3 localPos)
    {
        var t = GetOrCreateChild(parent, "Glint", localPos);
        t.localScale = Vector3.one * 0.35f;
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        sr.color = new Color(1f, 0.95f, 0.6f, 0.9f);
        var parentSr = parent.GetComponent<SpriteRenderer>();
        sr.sortingOrder = (parentSr != null ? parentSr.sortingOrder : 0) + 1;
        sr.enabled = false;
        return sr;
    }

    static AudioClip Clip(string path)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (c == null) log.Add("Sound not found: " + path);
        return c;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static void Dirty(Object o)
    {
        if (o == null) return;
        EditorUtility.SetDirty(o);
        if (PrefabUtility.IsPartOfPrefabInstance(o)) PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }
}
#endif
