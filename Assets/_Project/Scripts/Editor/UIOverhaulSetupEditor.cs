#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIOverhaulSetupEditor
{
    private const string FONT = "Assets/_Project/Fonts/Maplestory Bold SDF.asset";
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
    private static readonly Color Ink = new(0.08f, 0.065f, 0.055f, 0.96f);
    private static readonly Color Cream = new(1f, 0.91f, 0.67f);
    private static readonly Color Gold = new(1f, 0.62f, 0.12f);
    private static readonly Color Red = new(0.55f, 0.07f, 0.08f);
    private static readonly Color Active = new(0.43f, 0.20f, 0.20f);
    private static readonly Color Passive = new(0.18f, 0.43f, 0.44f);

    [MenuItem("PurpleCow/Setup/UI Overhaul")]
    public static void Run()
    {
        foreach (string name in new[] { "Canvas_HUD", "Canvas_Panel", "Canvas_Popup" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        Canvas hud = CanvasRoot("Canvas_HUD", 10);
        Canvas panel = CanvasRoot("Canvas_Panel", 20);
        Canvas popup = CanvasRoot("Canvas_Popup", 30);
        AlignLaunchPoint();
        AssignPassiveSkillIcons();
        GameObject safe = UI("SafeAreaPanel", hud.transform, Vector2.zero, Vector2.zero);
        Stretch(safe.GetComponent<RectTransform>());
        safe.AddComponent<SafeAreaFitter>();

        PausePanel pause = BuildPause(panel.transform);
        HUDPanel hudPanel = BuildHud(safe.transform, pause);
        SkillSelectionPanel selection = BuildLevelUp(panel.transform, hudPanel);
        ResultPanel result = BuildResult(popup.transform);
        BuildCharacterHp();
        PrewarmFontGlyphs();

        UIManager manager = Object.FindFirstObjectByType<UIManager>();
        if (manager != null)
        {
            SerializedObject so = new(manager);
            Ref(so, "_hudPanel", hudPanel);
            Ref(so, "_skillSelectionPanel", selection);
            Ref(so, "_resultPanel", result);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[UIOverhaul] Original-style UI generated.");
    }

    private static void AlignLaunchPoint()
    {
        WallFitter fitter = Object.FindFirstObjectByType<WallFitter>();
        if (fitter == null)
        {
            Debug.LogWarning("[UIOverhaul] WallFitter not found; LaunchPoint position was not changed.");
            return;
        }

        SerializedObject so = new(fitter);
        SerializedProperty bottomY = so.FindProperty("_nativeBottomY");
        SerializedProperty launchPointY = so.FindProperty("_nativeLaunchPointY");
        if (bottomY == null || launchPointY == null)
        {
            Debug.LogWarning("[UIOverhaul] WallFitter bottom/launch-point setting was not found.");
            return;
        }

        Transform launchPoint = so.FindProperty("_launchPoint")?.objectReferenceValue as Transform;
        bottomY.floatValue = -7.5f;
        launchPointY.floatValue = -6.7f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fitter);

        Transform character = launchPoint != null ? launchPoint.Find("Character") : null;
        if (character == null)
        {
            Debug.LogWarning("[UIOverhaul] Character below LaunchPoint was not found.");
            return;
        }

        character.localPosition = new Vector3(0f, -0.4f, 0f);
        EditorUtility.SetDirty(character);
    }

    private static void AssignPassiveSkillIcons()
    {
        (string dataPath, string iconPath, string displayName)[] map =
        {
            ("Assets/_Project/Data/SkillData_Passive_WarmTinHeart.asset", "Assets/_Project/Sprites/Passive/icon_passive_3000.png", "따뜻한 양철 심장"),
            ("Assets/_Project/Data/SkillData_Passive_MagicMirror.asset", "Assets/_Project/Sprites/Passive/icon_passive_3002.png", "마법 거울"),
            ("Assets/_Project/Data/SkillData_Passive_AmethystDagger.asset", "Assets/_Project/Sprites/Passive/icon_passive_3003.png", "자수정 단검"),
            ("Assets/_Project/Data/SkillData_Passive_EmeraldDagger.asset", "Assets/_Project/Sprites/Passive/icon_passive_3006.png", "에메랄드 단검"),
            ("Assets/_Project/Data/SkillData_Passive_LastMatch.asset", "Assets/_Project/Sprites/Passive/icon_passive_3007.png", "마지막 성냥"),
        };

        foreach ((string dataPath, string iconPath, string displayName) in map)
        {
            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(dataPath);
            Sprite best = null;
            float bestArea = -1f;
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(iconPath))
            {
                if (asset is not Sprite sprite) continue;
                float area = sprite.rect.width * sprite.rect.height;
                if (area <= bestArea) continue;
                best = sprite;
                bestArea = area;
            }

            if (data == null || best == null)
            {
                Debug.LogWarning($"[UIOverhaul] Passive icon missing: {dataPath} <- {iconPath}");
                continue;
            }

            SerializedObject so = new(data);
            Ref(so, "_icon", best);
            so.FindProperty("_skillName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        (string path, string name)[] activeNames =
        {
            ("Assets/_Project/Data/SkillData_Fire.asset", "파이어 볼"),
            ("Assets/_Project/Data/SkillData_Ice.asset", "아이스 볼"),
            ("Assets/_Project/Data/SkillData_Laser.asset", "레이저 볼"),
            ("Assets/_Project/Data/SkillData_Ghost.asset", "고스트 볼"),
            ("Assets/_Project/Data/SkillData_Cluster.asset", "클러스터 볼"),
        };
        foreach ((string path, string name) in activeNames)
        {
            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (data == null) continue;
            SerializedObject so = new(data);
            so.FindProperty("_skillName").stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }
    }

    private static HUDPanel BuildHud(Transform parent, PausePanel pause)
    {
        GameObject root = UI("HUDPanel", parent, Vector2.zero, Vector2.zero);
        Stretch(root.GetComponent<RectTransform>());
        HUDPanel hud = root.AddComponent<HUDPanel>();
        CanvasGroup cg = root.AddComponent<CanvasGroup>();

        TMP_Text title = Text("StageTitle", root.transform, "1. 깊은 숲", 52, Cream, new(0, -72), new(650, 72), TextAlignmentOptions.Center);
        Slider stage = Bar("StageProgress", root.transform, new(0, -138), new(360, 24), Red, Ink);
        TMP_Text percent = Text("StageProgressText", root.transform, "0%", 25, Cream, new(0, -142), new(120, 35), TextAlignmentOptions.Center);
        Slider xp = Bar("CharacterXP", root.transform, new(-28, -220), new(660, 46), new(1f, .68f, .18f), Ink);
        Image badge = ImageUi("CharacterLevelBadge", root.transform, new(348, -211), new(82, 82), new(.16f, .13f, .12f, .98f));
        TMP_Text level = Text("CharacterLevel", badge.transform, "1", 48, Cream, Vector2.zero, new(82, 82), TextAlignmentOptions.Center);
        Button pauseButton = ButtonUi("PauseButton", root.transform, "Ⅱ", new(430, -80), new(88, 88), Ink);
        Button successTestButton = ButtonUi("SuccessTestButton", root.transform, "S", new(-430, -80), new(88, 88), Ink);
        Button failureTestButton = ButtonUi("FailureTestButton", root.transform, "F", new(-330, -80), new(88, 88), Ink);

        GameObject skills = UI("PlayerActiveSkillBar", root.transform, new(-65, 210), new(130, 286));
        RectTransform sr = skills.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(1, 0);
        VerticalLayoutGroup layout = skills.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16; layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlHeight = layout.childControlWidth = false;
        PlayerSkillButton(skills.transform, "berserk", 0,
            "Assets/_Project/Data/PlayerActiveSkillData_Berserk.asset", new(.48f, .12f, .09f));
        PlayerSkillButton(skills.transform, "illusion", 1,
            "Assets/_Project/Data/PlayerActiveSkillData_Clone.asset", new(.08f, .34f, .45f));

        SerializedObject so = new(hud);
        Ref(so, "_stageTitleText", title); Ref(so, "_stageProgressSlider", stage);
        Ref(so, "_stageProgressText", percent); Ref(so, "_xpSlider", xp);
        Ref(so, "_levelText", level); Ref(so, "_pauseButton", pauseButton);
        Ref(so, "_successTestButton", successTestButton); Ref(so, "_failureTestButton", failureTestButton);
        Ref(so, "_pausePanel", pause); Ref(so, "_canvasGroup", cg);
        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    private static PausePanel BuildPause(Transform parent)
    {
        GameObject root = UI("PausePanel", parent, Vector2.zero, Vector2.zero); Stretch(root.GetComponent<RectTransform>());
        Image dim = root.AddComponent<Image>(); dim.color = new(0, 0, 0, .82f);
        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        PausePanel pause = root.AddComponent<PausePanel>();
        Text("Title", root.transform, "일시정지", 82, Cream, new(0, -400), new(800, 120), TextAlignmentOptions.Center);
        ImageUi("TitleLineLeft", root.transform, new(-300, -458), new(220, 7), Cream);
        ImageUi("TitleLineRight", root.transform, new(300, -458), new(220, 7), Cream);
        TMP_Text stage = Text("StageInfo", root.transform, "Stage 1  (Normal)", 44, Gold, new(0, -590), new(800, 80), TextAlignmentOptions.Center);
        SkillSlotGroup active = SlotGroup(root.transform, "PauseActiveSkillGroup", "Active Skill", 4, new(-165, -790), Active);
        SkillSlotGroup passive = SlotGroup(root.transform, "PausePassiveSkillGroup", "Passive Skill", 2, new(300, -790), Passive);
        Image drops = ImageUi("StageDrops", root.transform, new(0, -1130), new(850, 350), new(.06f, .07f, .10f, .96f));
        Text("DropTitle", drops.transform, "+ 현재 스테이지 드랍", 36, Color.white, new(-125, -28), new(620, 60), TextAlignmentOptions.Left);
        string[] dropSymbols = { "G", "SC", "SK", "HP", "ATK" };
        string[] dropAmounts = { "120", "40", "1", "1", "1" };
        for (int i = 0; i < 5; i++)
            DropTile(drops.transform, i, dropSymbols[i], dropAmounts[i]);
        Button cont = ButtonUi("ContinueButton", root.transform, "이어하기", new(0, 180), new(430, 126), Gold);
        RectTransform cr = cont.GetComponent<RectTransform>(); cr.anchorMin = cr.anchorMax = new(.5f, 0);
        SerializedObject so = new(pause);
        Ref(so, "_canvasGroup", cg); Ref(so, "_stageText", stage); Ref(so, "_continueButton", cont);
        Ref(so, "_activeSlotGroup", active); Ref(so, "_passiveSlotGroup", passive);
        so.ApplyModifiedPropertiesWithoutUndo();
        return pause;
    }

    private static SkillSelectionPanel BuildLevelUp(Transform parent, HUDPanel hudPanel)
    {
        GameObject root = UI("LevelUpPanel", parent, Vector2.zero, Vector2.zero); Stretch(root.GetComponent<RectTransform>());
        root.AddComponent<Image>().color = new(0, 0, 0, .82f);
        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        SkillSelectionPanel panel = root.AddComponent<SkillSelectionPanel>();
        Text("Title", root.transform, "레벨 업", 82, Cream, new(0, -220), new(700, 120), TextAlignmentOptions.Center);
        Slider levelXp = Bar("LevelXp", root.transform, new(-28, -350), new(660, 46), Gold, Ink);
        levelXp.value = 1f;
        Image levelBadge = ImageUi("LevelBadge", root.transform, new(348, -341), new(82, 82), new(.16f, .13f, .12f, .98f));
        TMP_Text levelText = Text("LevelText", levelBadge.transform, "2", 48, Cream, Vector2.zero, new(82, 82), TextAlignmentOptions.Center);
        SkillSlotGroup active = SlotGroup(root.transform, "ActiveSkillGroup", "Active Skill", 4, new(-155, -590), Active);
        SkillSlotGroup passive = SlotGroup(root.transform, "PassiveSkillGroup", "Passive Skill", 2, new(310, -590), Passive);
        List<SkillCardUI> cards = new();
        for (int i = 0; i < 3; i++) cards.Add(Card(root.transform, new((i - 1) * 340, -800)));
        List<SkillData> data = new();
        foreach (string guid in AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/_Project/Data" }))
        {
            SkillData value = AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(guid));
            if (value != null) data.Add(value);
        }
        SerializedObject so = new(panel);
        ArrayRefs(so, "_skillCards", cards); ArrayRefs(so, "_allSkillDatas", data);
        Ref(so, "_activeSlotGroup", active); Ref(so, "_passiveSlotGroup", passive); Ref(so, "_canvasGroup", cg);
        Ref(so, "_levelText", levelText);
        Ref(so, "_hudPanel", hudPanel);
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static SkillCardUI Card(Transform parent, Vector2 pos)
    {
        GameObject root = UI("SkillCard", parent, pos, new(310, 770));
        Image bg = root.AddComponent<Image>(); bg.color = Active;
        bg.sprite = BuiltinSprite(); bg.type = Image.Type.Sliced;
        Image inner = ImageUi("CardInner", root.transform, new(0, -8), new(282, 734), new(.07f, .065f, .06f, .72f));
        inner.transform.SetAsFirstSibling();
        Button button = root.AddComponent<Button>(); button.transition = Selectable.Transition.None; root.AddComponent<UIButton>();
        CanvasGroup cg = root.AddComponent<CanvasGroup>(); SkillCardUI card = root.AddComponent<SkillCardUI>();
        TMP_Text fresh = Text("NewText", root.transform, "New!", 34, Gold, new(0, -20), new(260, 48), TextAlignmentOptions.Center);
        TMP_Text type = Text("TypeText", root.transform, "액티브", 24, Cream, new(0, -66), new(240, 36), TextAlignmentOptions.Center);
        TMP_Text name = Text("NameText", root.transform, "스킬", 34, Color.white, new(0, -112), new(280, 70), TextAlignmentOptions.Center);
        ImageUi("IconPlate", root.transform, new(0, -196), new(164, 164), new(.035f, .03f, .025f, .92f));
        Image icon = ImageUi("Icon", root.transform, new(0, -205), new(145, 145), Color.white);
        icon.preserveAspect = true;
        Image damageBadge = ImageUi("DamageBadge", root.transform, new(0, -356), new(104, 44), Ink);
        TMP_Text damage = Text("DamageText", damageBadge.transform, "0", 28, Cream, Vector2.zero, new(96, 40), TextAlignmentOptions.Center);
        TMP_Text desc = Text("DescText", root.transform, "효과 설명", 26, Color.white, new(0, -414), new(250, 205), TextAlignmentOptions.TopLeft);
        TMP_Text level = Text("LevelText", root.transform, "Lv.1", 27, Gold, new(0, -650), new(180, 42), TextAlignmentOptions.Center);
        for (int i = 0; i < 3; i++)
            ImageUi($"LevelMark{i + 1}", root.transform, new((i - 1) * 42, -710), new(28, 28), i == 0 ? Gold : Ink);
        SerializedObject so = new(card);
        Ref(so, "_iconImage", icon); Ref(so, "_nameText", name); Ref(so, "_descriptionText", desc);
        Ref(so, "_typeText", type); Ref(so, "_damageText", damage); Ref(so, "_selectButton", button);
        Ref(so, "_canvasGroup", cg); Ref(so, "_background", bg); Ref(so, "_newText", fresh); Ref(so, "_levelText", level);
        Ref(so, "_damageRoot", damageBadge.gameObject);
        so.ApplyModifiedPropertiesWithoutUndo();
        return card;
    }

    private static SkillSlotGroup SlotGroup(Transform parent, string name, string label, int count, Vector2 pos, Color color)
    {
        GameObject root = UI(name, parent, pos, new(count * 112 + 20, 150)); SkillSlotGroup group = root.AddComponent<SkillSlotGroup>();
        Image groupFrame = root.AddComponent<Image>(); groupFrame.color = new(color.r, color.g, color.b, .88f); groupFrame.sprite = BuiltinSprite(); groupFrame.type = Image.Type.Sliced;
        Text("Label", root.transform, label, 28, color == Active ? new(1, .65f, .65f) : new(.65f, 1, 1), new(0, 65), new(root.GetComponent<RectTransform>().sizeDelta.x, 40), TextAlignmentOptions.Left);
        List<SkillSlotIcon> slots = new();
        for (int i = 0; i < count; i++)
        {
            GameObject slot = UI($"Slot{i + 1}", root.transform, new((i - (count - 1) / 2f) * 110, 0), new(100, 100));
            Image slotFrame = slot.AddComponent<Image>(); slotFrame.color = new(.05f, .045f, .04f, 1f); slotFrame.sprite = BuiltinSprite(); slotFrame.type = Image.Type.Sliced;
            Image empty = ImageUi("Empty", slot.transform, Vector2.zero, new(100, 100), Ink);
            GameObject filled = UI("Filled", slot.transform, Vector2.zero, new(100, 100));
            Image icon = ImageUi("Icon", filled.transform, Vector2.zero, new(92, 92), Color.white);
            icon.preserveAspect = true;
            Image levelBadge = ImageUi("LevelBadge", filled.transform, new(24, -66), new(56, 30), new(.03f, .025f, .02f, .96f));
            TMP_Text level = Text("LevelText", levelBadge.transform, string.Empty, 20, Cream, Vector2.zero, new(52, 28), TextAlignmentOptions.Center);
            SkillSlotIcon component = slot.AddComponent<SkillSlotIcon>(); SerializedObject so = new(component);
            Ref(so, "_iconImage", icon); Ref(so, "_levelText", level); Ref(so, "_filledRoot", filled); Ref(so, "_emptyRoot", empty.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo(); slots.Add(component);
        }
        SerializedObject gso = new(group); ArrayRefs(gso, "_slots", slots); gso.ApplyModifiedPropertiesWithoutUndo();
        return group;
    }

    private static ResultPanel BuildResult(Transform parent)
    {
        GameObject root = UI("ResultPopup", parent, Vector2.zero, Vector2.zero); Stretch(root.GetComponent<RectTransform>());
        root.AddComponent<Image>().color = new(0, 0, 0, .82f); CanvasGroup cg = root.AddComponent<CanvasGroup>();
        ResultPanel result = root.AddComponent<ResultPanel>();
        Image panel = ImageUi("Panel", root.transform, new(0, -360), new(800, 1100), new(.16f, .12f, .09f, .98f));
        TMP_Text title = Text("TitleText", panel.transform, "SUCCESS", 88, Gold, new(0, -70), new(700, 120), TextAlignmentOptions.Center);
        Text("StageText", panel.transform, "1. 깊은 숲", 42, Cream, new(0, -210), new(650, 60), TextAlignmentOptions.Center);
        TMP_Text wave = Text("WaveText", panel.transform, "도달 웨이브", 34, Color.white, new(0, -330), new(650, 55), TextAlignmentOptions.Center);
        TMP_Text score = Text("ScoreText", panel.transform, "처치 수", 34, Color.white, new(0, -420), new(650, 55), TextAlignmentOptions.Center);
        Button restart = ButtonUi("RestartButton", panel.transform, "다시 시작", new(0, -690), new(430, 130), Gold);
        SerializedObject so = new(result);
        Ref(so, "_resultTitleText", title); Ref(so, "_finalScoreText", score); Ref(so, "_waveText", wave);
        Ref(so, "_restartButton", restart); Ref(so, "_canvasGroup", cg); so.ApplyModifiedPropertiesWithoutUndo();
        return result;
    }

    private static void BuildCharacterHp()
    {
        const string path = "Assets/_Project/Prefabs/Character/Character.prefab";
        using PrefabUtility.EditPrefabContentsScope scope = new(path);
        Transform root = scope.prefabContentsRoot.transform;
        Transform old = root.Find("CharacterHpCanvas"); if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject canvasObj = new("CharacterHpCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObj.transform.SetParent(root, false); canvasObj.transform.localPosition = new(0, -.72f, 0); canvasObj.transform.localScale = Vector3.one * .01f;
        Canvas canvas = canvasObj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 20;
        Slider slider = Bar("CharacterHP", canvasObj.transform, Vector2.zero, new(104, 24), new(.1f, .9f, .35f), Ink);
        TMP_Text hp = Text("HpText", slider.transform, "10/10", 14, Ink, Vector2.zero, new(98, 26), TextAlignmentOptions.Center);
        hp.outlineColor = Cream;
        hp.outlineWidth = .2f;
        CharacterHpBar bar = slider.gameObject.AddComponent<CharacterHpBar>(); SerializedObject so = new(bar);
        Ref(so, "_slider", slider); Ref(so, "_hpText", hp); Ref(so, "_orientationRoot", canvasObj.transform);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Canvas CanvasRoot(string name, int order)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
        CanvasScaler s = go.GetComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new(1080, 2340); s.matchWidthOrHeight = 0;
        return c;
    }

    private static GameObject UI(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new(.5f, 1); r.pivot = new(.5f, 1); r.anchoredPosition = pos; r.sizeDelta = size; return go;
    }
    private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    private static Image ImageUi(string n, Transform p, Vector2 pos, Vector2 size, Color c) { GameObject g = UI(n, p, pos, size); Image i = g.AddComponent<Image>(); i.sprite = BuiltinSprite(); i.type = Image.Type.Sliced; i.color = c; i.raycastTarget = false; return i; }
    private static TMP_Text Text(string n, Transform p, string value, float size, Color c, Vector2 pos, Vector2 rect, TextAlignmentOptions align)
    { GameObject g = UI(n, p, pos, rect); TMP_Text t = g.AddComponent<TextMeshProUGUI>(); t.font = Font; t.text = value; t.fontSize = size; t.color = c; t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.outlineWidth = .18f; t.outlineColor = Color.black; return t; }
    private static Button ButtonUi(string n, Transform p, string value, Vector2 pos, Vector2 size, Color c)
    { GameObject g = UI(n, p, pos, size); Image i = g.AddComponent<Image>(); i.sprite = BuiltinSprite(); i.type = Image.Type.Sliced; i.color = c; Button b = g.AddComponent<Button>(); b.targetGraphic = i; b.transition = Selectable.Transition.None; g.AddComponent<UIButton>(); ImageUi("Inner", g.transform, new(0, -6), size - new Vector2(14, 18), new(Mathf.Min(c.r + .1f, 1), Mathf.Min(c.g + .08f, 1), Mathf.Min(c.b + .04f, 1), 1)); Text("Text", g.transform, value, 42, Cream, Vector2.zero, size, TextAlignmentOptions.Center); return b; }
    private static Slider Bar(string n, Transform p, Vector2 pos, Vector2 size, Color fill, Color back)
    {
        GameObject g = UI(n, p, pos, size);
        Slider s = g.AddComponent<Slider>();
        s.interactable = false;
        ImageUi("Outline", g.transform, new(0, 4), size + new Vector2(12, 12), new(.03f, .025f, .02f, 1));
        Image background = ImageUi("Background", g.transform, Vector2.zero, size, back);
        GameObject fillArea = UI("Fill Area", g.transform, Vector2.zero, Vector2.zero);
        RectTransform areaRect = fillArea.GetComponent<RectTransform>();
        Stretch(areaRect);
        float padding = Mathf.Min(7f, size.y * .18f);
        areaRect.offsetMin = new Vector2(padding, padding);
        areaRect.offsetMax = new Vector2(-padding, -padding);
        Image f = ImageUi("Fill", fillArea.transform, Vector2.zero, Vector2.zero, fill);
        Stretch(f.rectTransform);
        s.fillRect = f.rectTransform;
        s.targetGraphic = background;
        s.direction = Slider.Direction.LeftToRight;
        s.value = 0;
        return s;
    }

    private static void DropTile(Transform parent, int index, string symbol, string amount)
    {
        Color[] colors = { new(.55f, .55f, .58f), new(.25f, .7f, .25f), new(.2f, .5f, .85f), new(.25f, .7f, .25f), new(.25f, .7f, .25f) };
        Image tile = ImageUi($"Drop{index + 1}", parent, new(-275 + index * 138, -145), new(112, 112), colors[index]);
        TMP_Text symbolText = Text("Symbol", tile.transform, symbol, 50, Cream, Vector2.zero, new(95, 75), TextAlignmentOptions.Center);
        symbolText.textWrappingMode = TextWrappingModes.NoWrap;
        symbolText.enableAutoSizing = true;
        symbolText.fontSizeMin = 24;
        symbolText.fontSizeMax = 50;
        Text("Amount", tile.transform, amount, 25, Color.white, new(0, -76), new(95, 32), TextAlignmentOptions.Center);
    }

    private static void PrewarmFontGlyphs()
    {
        if (Font == null) return;

        StringBuilder characters = new();
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            characters.Append(text.text);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/_Project/Data" }))
        {
            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null) continue;
            characters.Append(data.SkillName);
            characters.Append(data.Description);
        }

        if (!Font.TryAddCharacters(characters.ToString(), out string missingCharacters))
            Debug.LogWarning($"[UIOverhaul] Font is missing glyphs: {missingCharacters}");
        EditorUtility.SetDirty(Font);
    }

    private static Sprite BuiltinSprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    private static void PlayerSkillButton(Transform parent, string name, int index, string dataPath, Color backgroundColor)
    {
        PlayerActiveSkillData data = AssetDatabase.LoadAssetAtPath<PlayerActiveSkillData>(dataPath);
        GameObject root = UI(name, parent, Vector2.zero, new(120, 120));
        Image background = root.AddComponent<Image>();
        background.sprite = BuiltinSprite();
        background.type = Image.Type.Sliced;
        background.color = Color.clear;
        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        root.AddComponent<UIButton>();

        Image icon = ImageUi("Icon", root.transform, Vector2.zero, new(94, 94), Color.white);
        icon.sprite = data != null ? data.Icon : null;
        icon.preserveAspect = true;

        Image overlay = ImageUi("CooldownOverlay", root.transform, Vector2.zero, new(94, 94), new(0, 0, 0, .72f));
        overlay.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillClockwise = true;

        TMP_Text cooldown = Text("CooldownText", root.transform, string.Empty, 40, Color.white,
            Vector2.zero, new(120, 120), TextAlignmentOptions.Center);
        PlayerActiveSkillButton component = root.AddComponent<PlayerActiveSkillButton>();
        SerializedObject so = new(component);
        so.FindProperty("_skillIndex").intValue = index;
        Ref(so, "_button", button);
        Ref(so, "_icon", icon);
        Ref(so, "_cooldownOverlay", overlay);
        Ref(so, "_cooldownText", cooldown);
        Ref(so, "_nameText", null);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Ref(SerializedObject so, string name, Object value) { SerializedProperty p = so.FindProperty(name); if (p != null) p.objectReferenceValue = value; }
    private static void ArrayRefs<T>(SerializedObject so, string name, IList<T> values) where T : Object { SerializedProperty p = so.FindProperty(name); p.arraySize = values.Count; for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
}
#endif
