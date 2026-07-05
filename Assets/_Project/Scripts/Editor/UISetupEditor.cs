#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public static class UISetupEditor
{
    [MenuItem("PurpleCow/Setup/Connect Player Active Skill Buttons")]
    private static void ConnectPlayerActiveSkillButtons()
    {
        EnsureEventSystem();
        SkillSetupEditor.CreatePlayerActiveSkillDataAssets();
        Step14_SetupPlayerActiveSkills();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("PurpleCow/Setup/UI Setup")]
    private static void SetupUI()
    {
        EnsurePrefabFolders();
        EnsureEventSystem();

        Canvas hudCanvas    = Step1_CreateCanvas("Canvas_HUD",   10);
        Canvas panelCanvas  = Step1_CreateCanvas("Canvas_Panel", 20);
        Canvas popupCanvas  = Step1_CreateCanvas("Canvas_Popup", 30);

        Step2_SetupHUDCanvas(hudCanvas);
        Step3_SetupPanelCanvas(panelCanvas);
        Step4_SetupManagers();
        Step5_ConnectUIManagerRefs();
        Step6_CreateSkillCardPrefab();
        Step7_CreateDamageTextFxPrefab();
        Step8_ConnectDamageTextManagerRefs();
        Step9_SetupHUDPanelContent();
        Step10_SetupResultPanelContent();
        Step11_SetupSkillSelectionPanelContent();
        Step12_CreateSkillSlotPrefab();
        Step13_SetupSkillSlotGroups();
        Step14_SetupPlayerActiveSkills();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[UISetupEditor] UI Setup 완료.");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<InputSystemUIInputModule>();
        Debug.Log("[UISetupEditor] EventSystem with InputSystemUIInputModule created.");
    }

    private static void EnsurePrefabFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
    }

    // ──────────────────────────────────────────
    // Step 1. Canvas 3개 생성
    // ──────────────────────────────────────────

    private static Canvas Step1_CreateCanvas(string name, int sortOrder)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Debug.Log($"[UISetupEditor] {name} 이미 존재, 스킵.");
            return existing.GetComponent<Canvas>();
        }

        GameObject go = new GameObject(name);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1080f, 1920f);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0f;

        go.AddComponent<GraphicRaycaster>();

        Debug.Log($"[UISetupEditor] {name} (Sort Order {sortOrder}) 생성 완료.");
        return canvas;
    }

    // ──────────────────────────────────────────
    // Step 2. Canvas_HUD 내부 구성
    // ──────────────────────────────────────────

    private static void Step2_SetupHUDCanvas(Canvas hudCanvas)
    {
        if (hudCanvas == null) return;
        Transform hudRoot = hudCanvas.transform;

        // SafeAreaPanel
        GameObject safeAreaPanel = EnsureChildObject(hudRoot, "SafeAreaPanel");
        StretchFill(EnsureRectTransform(safeAreaPanel));
        if (safeAreaPanel.GetComponent<SafeAreaFitter>() == null)
            safeAreaPanel.AddComponent<SafeAreaFitter>();

        // HUDPanel (상단 HUD 정보)
        GameObject hudPanelObj = EnsureChildObject(hudRoot, "HUDPanel");
        StretchFill(EnsureRectTransform(hudPanelObj));
        EnsureComponent<HUDPanel>(hudPanelObj);
        EnsureComponent<CanvasGroup>(hudPanelObj);

        // ResultPanel
        GameObject resultPanelObj = EnsureChildObject(hudRoot, "ResultPanel");
        StretchFill(EnsureRectTransform(resultPanelObj));
        EnsureComponent<ResultPanel>(resultPanelObj);
        EnsureComponent<CanvasGroup>(resultPanelObj);

        // SkillSelectionPanel
        GameObject skillPanelObj = EnsureChildObject(hudRoot, "SkillSelectionPanel");
        StretchFill(EnsureRectTransform(skillPanelObj));
        EnsureComponent<SkillSelectionPanel>(skillPanelObj);
        EnsureComponent<CanvasGroup>(skillPanelObj);

        // ── CharacterHP 바 (하단)
        GameObject charHpObj = EnsureChildObject(hudRoot, "CharacterHP");
        RectTransform charHpRect = EnsureRectTransform(charHpObj);
        charHpRect.anchorMin = new Vector2(0f, 0f);
        charHpRect.anchorMax = new Vector2(1f, 0f);
        charHpRect.pivot     = new Vector2(0.5f, 0f);
        charHpRect.anchoredPosition = new Vector2(0f, 40f);
        charHpRect.sizeDelta        = new Vector2(0f, 30f);
        if (charHpObj.GetComponent<Slider>() == null) charHpObj.AddComponent<Slider>();
        EnsureComponent<CharacterHpBar>(charHpObj);

        // TMP_Text (현재/최대 HP 숫자 표시)
        GameObject hpTextObj = EnsureChildObject(charHpObj.transform, "HpText");
        EnsureRectTransform(hpTextObj);
        if (hpTextObj.GetComponent<TextMeshProUGUI>() == null)
            hpTextObj.AddComponent<TextMeshProUGUI>();

        CharacterHpBar hpBar = charHpObj.GetComponent<CharacterHpBar>();
        SerializedObject hpBarSo = new SerializedObject(hpBar);
        hpBarSo.FindProperty("_slider").objectReferenceValue = charHpObj.GetComponent<Slider>();
        hpBarSo.FindProperty("_hpText").objectReferenceValue = hpTextObj.GetComponent<TextMeshProUGUI>();
        hpBarSo.ApplyModifiedPropertiesWithoutUndo();

        // CharacterXP(경험치 바)는 더 이상 여기(하단)에 생성하지 않는다.
        // TopBar 하위(상단)로 재배치되며, Step9_SetupHUDPanelContent에서 생성/재배치를 담당한다.

        Debug.Log("[UISetupEditor] HUD Canvas 구성 완료.");
    }

    // ──────────────────────────────────────────
    // Step 3. Canvas_Panel 내부 구성 (빈 패널 오브젝트)
    // ──────────────────────────────────────────

    private static void Step3_SetupPanelCanvas(Canvas panelCanvas)
    {
        if (panelCanvas == null) return;
        Transform panelRoot = panelCanvas.transform;

        string[] panelNames = { "LevelUpPanel", "PausePanel", "BallLevelUpPanel" };
        foreach (string pname in panelNames)
        {
            GameObject obj = EnsureChildObject(panelRoot, pname);
            StretchFill(EnsureRectTransform(obj));
            EnsureComponent<CanvasGroup>(obj);
        }

        Debug.Log("[UISetupEditor] Panel Canvas 구성 완료.");
    }

    // ──────────────────────────────────────────
    // Step 4. DamageTextManager / CharacterManager 씬 배치
    // ──────────────────────────────────────────

    private static void Step4_SetupManagers()
    {
        // DamageTextManager
        if (GameObject.Find("DamageTextManager") == null)
        {
            GameObject go = new GameObject("DamageTextManager");
            DamageTextManager dtm = go.AddComponent<DamageTextManager>();
            // DamageText 풀 루트
            GameObject poolRoot = new GameObject("DamageTextPool");
            poolRoot.transform.SetParent(go.transform);
            // _poolParent 즉시 연결
            SerializedObject dtmSo = new SerializedObject(dtm);
            dtmSo.FindProperty("_poolParent").objectReferenceValue = poolRoot.transform;
            dtmSo.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[UISetupEditor] DamageTextManager 배치 완료. _prefab은 Step8에서 연결됩니다.");
        }
        else
        {
            Debug.Log("[UISetupEditor] DamageTextManager 이미 존재, 스킵.");
        }

        // CharacterManager
        if (GameObject.Find("CharacterManager") == null)
        {
            GameObject go = new GameObject("CharacterManager");
            go.AddComponent<CharacterManager>();
            Debug.Log("[UISetupEditor] CharacterManager 배치 완료.");
        }
        else
        {
            Debug.Log("[UISetupEditor] CharacterManager 이미 존재, 스킵.");
        }
    }

    // ──────────────────────────────────────────
    // Step 5. UIManager 참조 연결
    // ──────────────────────────────────────────

    private static void Step5_ConnectUIManagerRefs()
    {
        GameObject uiManagerObj = GameObject.Find("UIManager");
        if (uiManagerObj == null)
        {
            Debug.LogWarning("[UISetupEditor] UIManager 오브젝트를 찾을 수 없어 참조 연결을 건너뜁니다.");
            return;
        }

        UIManager uiManager = uiManagerObj.GetComponent<UIManager>();
        if (uiManager == null) { Debug.LogWarning("[UISetupEditor] UIManager 컴포넌트 없음."); return; }

        SerializedObject so = new SerializedObject(uiManager);

        HUDPanel hudPanel = GameObject.Find("HUDPanel")?.GetComponent<HUDPanel>();
        ResultPanel resultPanel = GameObject.Find("ResultPanel")?.GetComponent<ResultPanel>();
        SkillSelectionPanel skillPanel = GameObject.Find("SkillSelectionPanel")?.GetComponent<SkillSelectionPanel>();

        if (hudPanel    != null) so.FindProperty("_hudPanel").objectReferenceValue            = hudPanel;
        if (resultPanel != null) so.FindProperty("_resultPanel").objectReferenceValue         = resultPanel;
        if (skillPanel  != null) so.FindProperty("_skillSelectionPanel").objectReferenceValue = skillPanel;

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UISetupEditor] UIManager 참조 연결 완료.");
    }

    // ──────────────────────────────────────────
    // Step 6. SkillCardUI 프리팹 생성
    // ──────────────────────────────────────────

    private static void Step6_CreateSkillCardPrefab()
    {
        const string path = "Assets/_Project/Prefabs/UI/SkillCard.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            GameObject go = new GameObject("SkillCard");
            go.AddComponent<RectTransform>();
            EnsureComponent<CanvasGroup>(go);

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(go.transform);
            iconObj.AddComponent<RectTransform>();
            iconObj.AddComponent<Image>();

            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(go.transform);
            nameObj.AddComponent<RectTransform>();
            nameObj.AddComponent<TextMeshProUGUI>();

            GameObject descObj = new GameObject("DescText");
            descObj.transform.SetParent(go.transform);
            descObj.AddComponent<RectTransform>();
            descObj.AddComponent<TextMeshProUGUI>();

            GameObject typeObj = new GameObject("TypeText");
            typeObj.transform.SetParent(go.transform);
            typeObj.AddComponent<RectTransform>();
            typeObj.AddComponent<TextMeshProUGUI>();

            GameObject damageObj = new GameObject("DamageText");
            damageObj.transform.SetParent(go.transform);
            damageObj.AddComponent<RectTransform>();
            damageObj.AddComponent<TextMeshProUGUI>();

            GameObject newLabelObj = new GameObject("NewLabel");
            newLabelObj.transform.SetParent(go.transform);
            newLabelObj.AddComponent<RectTransform>();
            TextMeshProUGUI newLabelTmp = newLabelObj.AddComponent<TextMeshProUGUI>();
            newLabelTmp.text = "New!";

            GameObject btnObj = new GameObject("SelectButton");
            btnObj.transform.SetParent(go.transform);
            btnObj.AddComponent<RectTransform>();
            Button btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btnObj.AddComponent<UIButton>();

            go.AddComponent<SkillCardUI>();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[UISetupEditor] SkillCard.prefab 생성 완료.");
        }

        // Always connect internal refs
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;
            SkillCardUI card = root.GetComponent<SkillCardUI>();
            if (card == null) { Debug.LogWarning("[UISetupEditor] SkillCardUI 컴포넌트 없음."); return; }

            // 기존 프리팹에 DamageText 자식이 없는 경우 보정 생성
            if (root.transform.Find("DamageText") == null)
            {
                GameObject damageObj = new GameObject("DamageText");
                damageObj.transform.SetParent(root.transform);
                damageObj.AddComponent<RectTransform>();
                damageObj.AddComponent<TextMeshProUGUI>();
            }

            // 기존 프리팹에 NewLabel 자식이 없는 경우 보정 생성
            if (root.transform.Find("NewLabel") == null)
            {
                GameObject newLabelObj = new GameObject("NewLabel");
                newLabelObj.transform.SetParent(root.transform);
                newLabelObj.AddComponent<RectTransform>();
                TextMeshProUGUI newLabelTmp = newLabelObj.AddComponent<TextMeshProUGUI>();
                newLabelTmp.text = "New!";
            }

            SerializedObject so = new SerializedObject(card);

            Image iconImg         = root.transform.Find("Icon")?.GetComponent<Image>();
            TextMeshProUGUI nameTmp = root.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descTmp = root.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI typeTmp = root.transform.Find("TypeText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI damageTmp = root.transform.Find("DamageText")?.GetComponent<TextMeshProUGUI>();
            Button selectBtn      = root.transform.Find("SelectButton")?.GetComponent<Button>();
            CanvasGroup cg        = root.GetComponent<CanvasGroup>();
            GameObject newLabelGo = root.transform.Find("NewLabel")?.gameObject;

            if (iconImg != null)   so.FindProperty("_iconImage").objectReferenceValue       = iconImg;
            if (nameTmp != null)   so.FindProperty("_nameText").objectReferenceValue         = nameTmp;
            if (descTmp != null)   so.FindProperty("_descriptionText").objectReferenceValue  = descTmp;
            if (typeTmp != null)   so.FindProperty("_typeText").objectReferenceValue         = typeTmp;
            if (damageTmp != null) so.FindProperty("_damageText").objectReferenceValue       = damageTmp;
            if (selectBtn != null) so.FindProperty("_selectButton").objectReferenceValue     = selectBtn;
            if (cg != null)        so.FindProperty("_canvasGroup").objectReferenceValue      = cg;
            if (newLabelGo != null) so.FindProperty("_newLabelObject").objectReferenceValue  = newLabelGo;

            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Debug.Log("[UISetupEditor] SkillCard.prefab 내부 참조 연결 완료.");
    }

    // ──────────────────────────────────────────
    // Step 7. DamageTextFx 프리팹 생성
    // ──────────────────────────────────────────

    private static void Step7_CreateDamageTextFxPrefab()
    {
        const string path = "Assets/_Project/Prefabs/UI/DamageTextFx.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("[UISetupEditor] DamageTextFx.prefab 이미 존재, 스킵.");
            return;
        }

        GameObject go = new GameObject("DamageTextFx");

        // World Space 3D TMP 텍스트 자식
        GameObject textObj = new GameObject("DamageText");
        textObj.transform.SetParent(go.transform, false);
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = 3f;

        // DamageTextFx 컴포넌트 + _text 연결
        DamageTextFx fx = go.AddComponent<DamageTextFx>();
        SerializedObject so = new SerializedObject(fx);
        so.FindProperty("_text").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[UISetupEditor] DamageTextFx.prefab 생성 완료.");
    }

    // ──────────────────────────────────────────
    // Step 8. DamageTextManager 참조 연결
    // ──────────────────────────────────────────

    private static void Step8_ConnectDamageTextManagerRefs()
    {
        GameObject dtmObj = GameObject.Find("DamageTextManager");
        if (dtmObj == null)
        {
            Debug.LogWarning("[UISetupEditor] DamageTextManager 씬 오브젝트 없음.");
            return;
        }

        DamageTextManager dtm = dtmObj.GetComponent<DamageTextManager>();
        if (dtm == null)
        {
            Debug.LogWarning("[UISetupEditor] DamageTextManager 컴포넌트 없음.");
            return;
        }

        const string prefabPath = "Assets/_Project/Prefabs/UI/DamageTextFx.prefab";
        DamageTextFx fxPrefab = AssetDatabase.LoadAssetAtPath<DamageTextFx>(prefabPath);
        Transform poolParent = dtmObj.transform.Find("DamageTextPool");
        if (poolParent == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            poolObj.transform.SetParent(dtmObj.transform);
            poolParent = poolObj.transform;
            Debug.Log("[UISetupEditor] DamageTextPool 생성 완료.");
        }

        SerializedObject so = new SerializedObject(dtm);
        if (fxPrefab != null)
            so.FindProperty("_prefab").objectReferenceValue = fxPrefab;
        else
            Debug.LogWarning("[UISetupEditor] DamageTextFx.prefab 없음.");

        so.FindProperty("_poolParent").objectReferenceValue = poolParent;

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UISetupEditor] DamageTextManager 참조 연결 완료.");
    }

    // ──────────────────────────────────────────
    // Step 9. HUDPanel 내부 UI 구성 및 참조 연결
    // ──────────────────────────────────────────

    private static void Step9_SetupHUDPanelContent()
    {
        GameObject hudPanelObj = GameObject.Find("HUDPanel");
        if (hudPanelObj == null) { Debug.LogWarning("[UISetupEditor] HUDPanel 없음."); return; }

        HUDPanel hudPanel = hudPanelObj.GetComponent<HUDPanel>();
        if (hudPanel == null) { Debug.LogWarning("[UISetupEditor] HUDPanel 컴포넌트 없음."); return; }

        // ── TopBar (스테이지명 + 스테이지 전체 누적 처치율 진행바 + 보스 아이콘(장식) + 경험치 바)
        GameObject topBarObj = EnsureChildObject(hudPanelObj.transform, "TopBar");
        RectTransform topBarRect = EnsureRectTransform(topBarObj);
        topBarRect.anchorMin        = new Vector2(0f, 1f);
        topBarRect.anchorMax        = new Vector2(1f, 1f);
        topBarRect.pivot            = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = new Vector2(0f, -40f);
        topBarRect.sizeDelta        = new Vector2(0f, 110f);

        // StageNameText ("1. 깊은 숲")
        GameObject stageNameObj = EnsureChildObject(topBarObj.transform, "StageNameText");
        RectTransform stageNameRect = EnsureRectTransform(stageNameObj);
        stageNameRect.anchorMin        = new Vector2(0f, 1f);
        stageNameRect.anchorMax        = new Vector2(0.7f, 1f);
        stageNameRect.pivot            = new Vector2(0f, 1f);
        stageNameRect.anchoredPosition = new Vector2(20f, 0f);
        stageNameRect.sizeDelta        = new Vector2(0f, 40f);
        if (stageNameObj.GetComponent<TextMeshProUGUI>() == null) stageNameObj.AddComponent<TextMeshProUGUI>();

        // StageProgressBackground + StageProgressFillImage (Filled/Horizontal, 스테이지 전체 누적 처치율 %)
        GameObject progressBgObj = EnsureChildObject(topBarObj.transform, "StageProgressBackground");
        RectTransform progressBgRect = EnsureRectTransform(progressBgObj);
        progressBgRect.anchorMin        = new Vector2(0f, 1f);
        progressBgRect.anchorMax        = new Vector2(1f, 1f);
        progressBgRect.pivot            = new Vector2(0.5f, 1f);
        progressBgRect.anchoredPosition = new Vector2(0f, -40f);
        progressBgRect.sizeDelta        = new Vector2(-40f, 24f);
        Image progressBgImg = progressBgObj.GetComponent<Image>();
        if (progressBgImg == null) progressBgImg = progressBgObj.AddComponent<Image>();
        progressBgImg.color = new Color(0f, 0f, 0f, 0.3f);

        GameObject progressFillObj = EnsureChildObject(progressBgObj.transform, "StageProgressFillImage");
        RectTransform progressFillRect = EnsureRectTransform(progressFillObj);
        StretchFill(progressFillRect);
        Image progressFillImg = progressFillObj.GetComponent<Image>();
        if (progressFillImg == null) progressFillImg = progressFillObj.AddComponent<Image>();
        progressFillImg.color       = Color.red;
        progressFillImg.type        = Image.Type.Filled;
        progressFillImg.fillMethod  = Image.FillMethod.Horizontal;
        progressFillImg.fillOrigin  = (int)Image.OriginHorizontal.Left;
        progressFillImg.fillAmount  = 0f;

        // ProgressText (진행률 % 텍스트, 진행바 위에 오버레이)
        GameObject progressTextObj = EnsureChildObject(progressBgObj.transform, "ProgressText");
        RectTransform progressTextRect = EnsureRectTransform(progressTextObj);
        StretchFill(progressTextRect);
        if (progressTextObj.GetComponent<TextMeshProUGUI>() == null) progressTextObj.AddComponent<TextMeshProUGUI>();

        // BossIcon (TopBar 우측 장식용 아이콘, 기능 없음 - PDF상 보스 미구현)
        GameObject bossIconObj = EnsureChildObject(topBarObj.transform, "BossIcon");
        RectTransform bossIconRect = EnsureRectTransform(bossIconObj);
        bossIconRect.anchorMin        = new Vector2(1f, 1f);
        bossIconRect.anchorMax        = new Vector2(1f, 1f);
        bossIconRect.pivot            = new Vector2(1f, 1f);
        bossIconRect.anchoredPosition = new Vector2(-10f, -4f);
        bossIconRect.sizeDelta        = new Vector2(36f, 36f);
        if (bossIconObj.GetComponent<Image>() == null) bossIconObj.AddComponent<Image>();

        // CharacterXP(경험치 바): 기존 오브젝트가 씬 어딘가에 있으면 TopBar 하위로 재배치(참조 유지),
        // 없으면 TopBar 하위에 신규 생성한다.
        GameObject charXpObj = GameObject.Find("CharacterXP");
        bool charXpIsNew = charXpObj == null;
        if (charXpIsNew)
            charXpObj = EnsureChildObject(topBarObj.transform, "CharacterXP");
        else
            charXpObj.transform.SetParent(topBarObj.transform, false);

        RectTransform charXpRect = EnsureRectTransform(charXpObj);
        charXpRect.anchorMin        = new Vector2(0f, 1f);
        charXpRect.anchorMax        = new Vector2(1f, 1f);
        charXpRect.pivot            = new Vector2(0.5f, 1f);
        charXpRect.anchoredPosition = new Vector2(0f, -74f);
        charXpRect.sizeDelta        = new Vector2(-40f, 24f);
        if (charXpObj.GetComponent<Slider>() == null) charXpObj.AddComponent<Slider>();

        GameObject levelTextObj = EnsureChildObject(charXpObj.transform, "LevelText");
        EnsureRectTransform(levelTextObj);
        if (levelTextObj.GetComponent<TextMeshProUGUI>() == null)
            levelTextObj.AddComponent<TextMeshProUGUI>();
        EnsureComponent<CharacterXpBar>(charXpObj);

        CharacterXpBar xpBar = charXpObj.GetComponent<CharacterXpBar>();
        SerializedObject xpBarSo = new SerializedObject(xpBar);
        xpBarSo.FindProperty("_slider").objectReferenceValue = charXpObj.GetComponent<Slider>();
        xpBarSo.FindProperty("_levelText").objectReferenceValue = levelTextObj.GetComponent<TextMeshProUGUI>();
        xpBarSo.ApplyModifiedPropertiesWithoutUndo();

        // HUDPanel 참조 연결 (자식 오브젝트 생성/재배치 직후 곧바로 연결)
        SerializedObject so = new SerializedObject(hudPanel);
        so.FindProperty("_stageNameText").objectReferenceValue          = stageNameObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_stageProgressFillImage").objectReferenceValue = progressFillImg;
        so.FindProperty("_progressText").objectReferenceValue           = progressTextObj.GetComponent<TextMeshProUGUI>();

        CanvasGroup hudCg = hudPanelObj.GetComponent<CanvasGroup>();
        if (hudCg != null) so.FindProperty("_canvasGroup").objectReferenceValue = hudCg;

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UISetupEditor] HUDPanel 내부 구성 완료.");
    }

    // ──────────────────────────────────────────
    // Step 10. ResultPanel 내부 UI 구성 및 참조 연결
    // ──────────────────────────────────────────

    private static void Step10_SetupResultPanelContent()
    {
        GameObject resultPanelObj = GameObject.Find("ResultPanel");
        if (resultPanelObj == null) { Debug.LogWarning("[UISetupEditor] ResultPanel 없음."); return; }

        ResultPanel resultPanel = resultPanelObj.GetComponent<ResultPanel>();
        if (resultPanel == null) { Debug.LogWarning("[UISetupEditor] ResultPanel 컴포넌트 없음."); return; }

        GameObject titleTextObj = EnsureChildObject(resultPanelObj.transform, "TitleText");
        if (titleTextObj.GetComponent<TextMeshProUGUI>() == null) titleTextObj.AddComponent<TextMeshProUGUI>();

        GameObject restartBtnObj = EnsureChildObject(resultPanelObj.transform, "RestartButton");
        if (restartBtnObj.GetComponent<Button>() == null) restartBtnObj.AddComponent<Button>();

        SerializedObject so = new SerializedObject(resultPanel);
        so.FindProperty("_resultTitleText").objectReferenceValue = titleTextObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_restartButton").objectReferenceValue   = restartBtnObj.GetComponent<Button>();

        CanvasGroup resultCg = resultPanelObj.GetComponent<CanvasGroup>();
        if (resultCg != null) so.FindProperty("_canvasGroup").objectReferenceValue = resultCg;

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UISetupEditor] ResultPanel 내부 구성 완료.");
    }

    // ──────────────────────────────────────────
    // Step 11. SkillSelectionPanel 내부 구성 및 참조 연결
    // ──────────────────────────────────────────

    private static void Step11_SetupSkillSelectionPanelContent()
    {
        GameObject skillPanelObj = GameObject.Find("SkillSelectionPanel");
        if (skillPanelObj == null) { Debug.LogWarning("[UISetupEditor] SkillSelectionPanel 없음."); return; }

        SkillSelectionPanel skillPanel = skillPanelObj.GetComponent<SkillSelectionPanel>();
        if (skillPanel == null) { Debug.LogWarning("[UISetupEditor] SkillSelectionPanel 컴포넌트 없음."); return; }

        const string skillCardPath = "Assets/_Project/Prefabs/UI/SkillCard.prefab";
        GameObject skillCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(skillCardPath);
        if (skillCardPrefab == null)
        {
            Debug.LogWarning("[UISetupEditor] SkillCard.prefab 없음. Step6을 먼저 실행하세요.");
            return;
        }

        // 기존 SkillCardUI 자식 수집
        System.Collections.Generic.List<SkillCardUI> cardList =
            new System.Collections.Generic.List<SkillCardUI>();
        foreach (Transform child in skillPanelObj.transform)
        {
            SkillCardUI existing = child.GetComponent<SkillCardUI>();
            if (existing != null) cardList.Add(existing);
        }

        // 3개가 될 때까지 프리팹 인스턴스 추가
        int needed = 3 - cardList.Count;
        for (int i = 0; i < needed; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                skillCardPrefab, skillPanelObj.transform);
            SkillCardUI card = instance.GetComponent<SkillCardUI>();
            if (card != null) cardList.Add(card);
        }

        // SkillData 에셋 로드
        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/_Project/Data" });
        System.Collections.Generic.List<SkillData> skillDatas =
            new System.Collections.Generic.List<SkillData>();
        foreach (string guid in guids)
        {
            SkillData sd = AssetDatabase.LoadAssetAtPath<SkillData>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (sd != null) skillDatas.Add(sd);
        }

        SerializedObject so = new SerializedObject(skillPanel);

        SerializedProperty skillCardsProp = so.FindProperty("_skillCards");
        skillCardsProp.arraySize = cardList.Count;
        for (int i = 0; i < cardList.Count; i++)
            skillCardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardList[i];

        SerializedProperty allSkillsProp = so.FindProperty("_allSkillDatas");
        allSkillsProp.arraySize = skillDatas.Count;
        for (int i = 0; i < skillDatas.Count; i++)
            allSkillsProp.GetArrayElementAtIndex(i).objectReferenceValue = skillDatas[i];

        CanvasGroup panelCg = skillPanelObj.GetComponent<CanvasGroup>();
        if (panelCg != null) so.FindProperty("_canvasGroup").objectReferenceValue = panelCg;

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[UISetupEditor] SkillSelectionPanel 구성 완료. " +
                  $"SkillCard: {cardList.Count}개, SkillData: {skillDatas.Count}개.");
    }

    // ──────────────────────────────────────────
    // Step 12. SkillSlot 프리팹 생성
    // ──────────────────────────────────────────

    private static void Step12_CreateSkillSlotPrefab()
    {
        const string path = "Assets/_Project/Prefabs/UI/SkillSlot.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            GameObject go = new GameObject("SkillSlot");
            go.AddComponent<RectTransform>();

            GameObject filledObj = new GameObject("Filled");
            filledObj.transform.SetParent(go.transform);
            filledObj.AddComponent<RectTransform>();

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(filledObj.transform);
            iconObj.AddComponent<RectTransform>();
            iconObj.AddComponent<Image>();

            GameObject levelTextObj = new GameObject("LevelText");
            levelTextObj.transform.SetParent(filledObj.transform);
            levelTextObj.AddComponent<RectTransform>();
            levelTextObj.AddComponent<TextMeshProUGUI>();

            GameObject emptyObj = new GameObject("Empty");
            emptyObj.transform.SetParent(go.transform);
            emptyObj.AddComponent<RectTransform>();
            Image emptyImg = emptyObj.AddComponent<Image>();
            emptyImg.color = Color.black;

            go.AddComponent<SkillSlotIcon>();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[UISetupEditor] SkillSlot.prefab 생성 완료.");
        }

        // Always connect internal refs
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;
            SkillSlotIcon slot = root.GetComponent<SkillSlotIcon>();
            if (slot == null) { Debug.LogWarning("[UISetupEditor] SkillSlotIcon 컴포넌트 없음."); return; }

            SerializedObject so = new SerializedObject(slot);

            GameObject filledRoot = root.transform.Find("Filled")?.gameObject;
            GameObject emptyRoot  = root.transform.Find("Empty")?.gameObject;
            Image iconImg           = root.transform.Find("Filled/Icon")?.GetComponent<Image>();
            TextMeshProUGUI levelTmp = root.transform.Find("Filled/LevelText")?.GetComponent<TextMeshProUGUI>();

            if (iconImg != null)    so.FindProperty("_iconImage").objectReferenceValue  = iconImg;
            if (levelTmp != null)   so.FindProperty("_levelText").objectReferenceValue  = levelTmp;
            if (filledRoot != null) so.FindProperty("_filledRoot").objectReferenceValue = filledRoot;
            if (emptyRoot != null)  so.FindProperty("_emptyRoot").objectReferenceValue  = emptyRoot;

            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Debug.Log("[UISetupEditor] SkillSlot.prefab 내부 참조 연결 완료.");
    }

    // ──────────────────────────────────────────
    // Step 13. SkillSelectionPanel Active/Passive 슬롯 그룹 구성
    // ──────────────────────────────────────────

    private static void Step13_SetupSkillSlotGroups()
    {
        GameObject skillPanelObj = GameObject.Find("SkillSelectionPanel");
        if (skillPanelObj == null) { Debug.LogWarning("[UISetupEditor] SkillSelectionPanel 없음."); return; }

        SkillSelectionPanel skillPanel = skillPanelObj.GetComponent<SkillSelectionPanel>();
        if (skillPanel == null) { Debug.LogWarning("[UISetupEditor] SkillSelectionPanel 컴포넌트 없음."); return; }

        const string skillSlotPath = "Assets/_Project/Prefabs/UI/SkillSlot.prefab";
        GameObject skillSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(skillSlotPath);
        if (skillSlotPrefab == null)
        {
            Debug.LogWarning("[UISetupEditor] SkillSlot.prefab 없음. Step12를 먼저 실행하세요.");
            return;
        }

        SkillSlotGroup activeGroup  = SetupSlotGroup(skillPanelObj.transform, "ActiveSkillGroup",  "Active Skill",  4, skillSlotPrefab);
        SkillSlotGroup passiveGroup = SetupSlotGroup(skillPanelObj.transform, "PassiveSkillGroup", "Passive Skill", 2, skillSlotPrefab);

        SerializedObject so = new SerializedObject(skillPanel);
        if (activeGroup  != null) so.FindProperty("_activeSlotGroup").objectReferenceValue  = activeGroup;
        if (passiveGroup != null) so.FindProperty("_passiveSlotGroup").objectReferenceValue = passiveGroup;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[UISetupEditor] SkillSelectionPanel Active/Passive 슬롯 그룹 구성 완료.");
    }

    private static SkillSlotGroup SetupSlotGroup(
        Transform parent, string groupName, string labelText, int slotCount, GameObject skillSlotPrefab)
    {
        GameObject groupObj = EnsureChildObject(parent, groupName);
        EnsureRectTransform(groupObj);
        SkillSlotGroup group = EnsureComponent<SkillSlotGroup>(groupObj);

        // 라벨 (Active Skill / Passive Skill)
        GameObject labelObj = EnsureChildObject(groupObj.transform, "Label");
        EnsureRectTransform(labelObj);
        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        if (labelTmp == null) labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;

        // 기존 SkillSlotIcon 자식 수집
        System.Collections.Generic.List<SkillSlotIcon> slotList =
            new System.Collections.Generic.List<SkillSlotIcon>();
        foreach (Transform child in groupObj.transform)
        {
            SkillSlotIcon existing = child.GetComponent<SkillSlotIcon>();
            if (existing != null) slotList.Add(existing);
        }

        // 필요한 개수만큼 프리팹 인스턴스 추가
        int needed = slotCount - slotList.Count;
        for (int i = 0; i < needed; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                skillSlotPrefab, groupObj.transform);
            SkillSlotIcon slotIcon = instance.GetComponent<SkillSlotIcon>();
            if (slotIcon != null) slotList.Add(slotIcon);
        }

        SerializedObject groupSo = new SerializedObject(group);
        SerializedProperty slotsProp = groupSo.FindProperty("_slots");
        slotsProp.arraySize = slotList.Count;
        for (int i = 0; i < slotList.Count; i++)
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotList[i];
        groupSo.ApplyModifiedPropertiesWithoutUndo();

        return group;
    }

    // ──────────────────────────────────────────
    // 유틸리티
    // ──────────────────────────────────────────

    private static void Step14_SetupPlayerActiveSkills()
    {
        GameObject managerObj = GameObject.Find("PlayerActiveSkillManager");
        if (managerObj == null)
        {
            managerObj = new GameObject("PlayerActiveSkillManager");
            managerObj.AddComponent<PlayerActiveSkillManager>();
        }

        PlayerActiveSkillData berserk = AssetDatabase.LoadAssetAtPath<PlayerActiveSkillData>(
            "Assets/_Project/Data/PlayerActiveSkillData_Berserk.asset");
        PlayerActiveSkillData clone = AssetDatabase.LoadAssetAtPath<PlayerActiveSkillData>(
            "Assets/_Project/Data/PlayerActiveSkillData_Clone.asset");

        PlayerActiveSkillManager manager = managerObj.GetComponent<PlayerActiveSkillManager>();
        SerializedObject managerSo = new SerializedObject(manager);
        SerializedProperty skills = managerSo.FindProperty("_skills");
        skills.arraySize = 2;
        skills.GetArrayElementAtIndex(0).objectReferenceValue = berserk;
        skills.GetArrayElementAtIndex(1).objectReferenceValue = clone;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        Button berserkButton = FindSceneButton("berserk");
        Button illusionButton = FindSceneButton("illusion");
        if (berserkButton != null && illusionButton != null)
        {
            SetupExistingPlayerActiveSkillButton(berserkButton, 0);
            SetupExistingPlayerActiveSkillButton(illusionButton, 1);
            Debug.Log("[UISetupEditor] Existing berserk/illusion buttons connected.");
            return;
        }

        GameObject hudPanelObj = GameObject.Find("HUDPanel");
        if (hudPanelObj == null)
        {
            Debug.LogWarning("[UISetupEditor] HUDPanel is missing.");
            return;
        }

        GameObject barObj = EnsureChildObject(hudPanelObj.transform, "PlayerActiveSkillBar");
        RectTransform barRect = EnsureRectTransform(barObj);
        barRect.anchorMin = new Vector2(1f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(1f, 0f);
        barRect.anchoredPosition = new Vector2(-36f, 130f);
        barRect.sizeDelta = new Vector2(304f, 142f);

        HorizontalLayoutGroup layout = EnsureComponent<HorizontalLayoutGroup>(barObj);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        SetupPlayerActiveSkillButton(
            barObj.transform, "BerserkButton", 0, new Color(0.78f, 0.18f, 0.14f));
        SetupPlayerActiveSkillButton(
            barObj.transform, "CloneButton", 1, new Color(0.12f, 0.55f, 0.68f));
    }

    private static Button FindSceneButton(string objectName)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (string.Equals(
                    button.gameObject.name,
                    objectName,
                    System.StringComparison.OrdinalIgnoreCase))
                return button;
        }

        return null;
    }

    private static void SetupExistingPlayerActiveSkillButton(Button button, int skillIndex)
    {
        GameObject buttonObj = button.gameObject;
        Image icon = buttonObj.GetComponent<Image>();
        EnsureComponent<UIButton>(buttonObj);
        PlayerActiveSkillButton skillButton = EnsureComponent<PlayerActiveSkillButton>(buttonObj);

        GameObject overlayObj = EnsureChildObject(buttonObj.transform, "CooldownOverlay");
        StretchFill(EnsureRectTransform(overlayObj));
        Image overlay = EnsureComponent<Image>(overlayObj);
        overlay.color = new Color(0f, 0f, 0f, 0.72f);
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillClockwise = true;
        overlay.raycastTarget = false;

        GameObject cooldownTextObj = EnsureChildObject(buttonObj.transform, "CooldownText");
        StretchFill(EnsureRectTransform(cooldownTextObj));
        TextMeshProUGUI cooldownText = EnsureComponent<TextMeshProUGUI>(cooldownTextObj);
        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.fontSize = 36f;
        cooldownText.color = Color.white;
        cooldownText.raycastTarget = false;

        SerializedObject so = new SerializedObject(skillButton);
        so.FindProperty("_skillIndex").intValue = skillIndex;
        so.FindProperty("_button").objectReferenceValue = button;
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_cooldownOverlay").objectReferenceValue = overlay;
        so.FindProperty("_cooldownText").objectReferenceValue = cooldownText;
        so.FindProperty("_nameText").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupPlayerActiveSkillButton(
        Transform parent,
        string objectName,
        int skillIndex,
        Color backgroundColor)
    {
        GameObject buttonObj = EnsureChildObject(parent, objectName);
        EnsureRectTransform(buttonObj).sizeDelta = new Vector2(142f, 142f);

        LayoutElement layoutElement = EnsureComponent<LayoutElement>(buttonObj);
        layoutElement.preferredWidth = 142f;
        layoutElement.preferredHeight = 142f;

        Image background = EnsureComponent<Image>(buttonObj);
        background.color = backgroundColor;
        Button button = EnsureComponent<Button>(buttonObj);
        button.targetGraphic = background;
        EnsureComponent<UIButton>(buttonObj);
        PlayerActiveSkillButton skillButton = EnsureComponent<PlayerActiveSkillButton>(buttonObj);

        GameObject iconObj = EnsureChildObject(buttonObj.transform, "Icon");
        RectTransform iconRect = EnsureRectTransform(iconObj);
        StretchFill(iconRect);
        iconRect.offsetMin = new Vector2(18f, 34f);
        iconRect.offsetMax = new Vector2(-18f, -12f);
        Image icon = EnsureComponent<Image>(iconObj);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject overlayObj = EnsureChildObject(buttonObj.transform, "CooldownOverlay");
        StretchFill(EnsureRectTransform(overlayObj));
        Image overlay = EnsureComponent<Image>(overlayObj);
        overlay.color = new Color(0f, 0f, 0f, 0.72f);
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillClockwise = true;
        overlay.raycastTarget = false;

        GameObject cooldownTextObj = EnsureChildObject(buttonObj.transform, "CooldownText");
        StretchFill(EnsureRectTransform(cooldownTextObj));
        TextMeshProUGUI cooldownText = EnsureComponent<TextMeshProUGUI>(cooldownTextObj);
        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.fontSize = 42f;
        cooldownText.color = Color.white;
        cooldownText.raycastTarget = false;

        GameObject nameTextObj = EnsureChildObject(buttonObj.transform, "NameText");
        RectTransform nameTextRect = EnsureRectTransform(nameTextObj);
        nameTextRect.anchorMin = new Vector2(0f, 0f);
        nameTextRect.anchorMax = new Vector2(1f, 0f);
        nameTextRect.pivot = new Vector2(0.5f, 0f);
        nameTextRect.anchoredPosition = new Vector2(0f, 5f);
        nameTextRect.sizeDelta = new Vector2(-8f, 30f);
        TextMeshProUGUI nameText = EnsureComponent<TextMeshProUGUI>(nameTextObj);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 20f;
        nameText.color = Color.white;
        nameText.raycastTarget = false;

        SerializedObject so = new SerializedObject(skillButton);
        so.FindProperty("_skillIndex").intValue = skillIndex;
        so.FindProperty("_button").objectReferenceValue = button;
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_cooldownOverlay").objectReferenceValue = overlay;
        so.FindProperty("_cooldownText").objectReferenceValue = cooldownText;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject EnsureChildObject(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            if (existing.GetComponent<RectTransform>() != null)
                return existing.gameObject;
            // 이전 실행에서 plain Transform으로 생성된 경우 제거 후 재생성
            Object.DestroyImmediate(existing.gameObject);
        }
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        return rt;
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
        {
            try { comp = go.AddComponent<T>(); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UISetupEditor] {typeof(T).Name} AddComponent 실패: {e.Message}");
            }
        }
        return comp;
    }
}
#endif
