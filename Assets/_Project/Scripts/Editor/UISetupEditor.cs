#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UISetupEditor
{
    [MenuItem("PurpleCow/Setup/UI Setup")]
    private static void SetupUI()
    {
        EnsurePrefabFolders();

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

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[UISetupEditor] UI Setup 완료.");
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

        // ── CharacterXP 바 (CharacterHP 위)
        GameObject charXpObj = EnsureChildObject(hudRoot, "CharacterXP");
        RectTransform charXpRect = EnsureRectTransform(charXpObj);
        charXpRect.anchorMin = new Vector2(0f, 0f);
        charXpRect.anchorMax = new Vector2(1f, 0f);
        charXpRect.pivot     = new Vector2(0.5f, 0f);
        charXpRect.anchoredPosition = new Vector2(0f, 80f);
        charXpRect.sizeDelta        = new Vector2(0f, 30f);
        if (charXpObj.GetComponent<Slider>() == null) charXpObj.AddComponent<Slider>();
        // TMP_Text (레벨 표시)
        GameObject levelTextObj = EnsureChildObject(charXpObj.transform, "LevelText");
        EnsureRectTransform(levelTextObj);
        if (levelTextObj.GetComponent<TextMeshProUGUI>() == null)
            levelTextObj.AddComponent<TextMeshProUGUI>();
        EnsureComponent<CharacterXpBar>(charXpObj);

        Debug.Log("[UISetupEditor] HUD Canvas 구성 완료.");
    }

    // ──────────────────────────────────────────
    // Step 3. Canvas_Panel 내부 구성 (빈 패널 오브젝트)
    // ──────────────────────────────────────────

    private static void Step3_SetupPanelCanvas(Canvas panelCanvas)
    {
        if (panelCanvas == null) return;
        Transform panelRoot = panelCanvas.transform;

        string[] panelNames = { "LevelUpPanel", "PausePanel", "BallLevelUpPanel", "PrismPanel" };
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

            SerializedObject so = new SerializedObject(card);

            Image iconImg         = root.transform.Find("Icon")?.GetComponent<Image>();
            TextMeshProUGUI nameTmp = root.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descTmp = root.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI typeTmp = root.transform.Find("TypeText")?.GetComponent<TextMeshProUGUI>();
            Button selectBtn      = root.transform.Find("SelectButton")?.GetComponent<Button>();
            CanvasGroup cg        = root.GetComponent<CanvasGroup>();

            if (iconImg != null)   so.FindProperty("_iconImage").objectReferenceValue       = iconImg;
            if (nameTmp != null)   so.FindProperty("_nameText").objectReferenceValue         = nameTmp;
            if (descTmp != null)   so.FindProperty("_descriptionText").objectReferenceValue  = descTmp;
            if (typeTmp != null)   so.FindProperty("_typeText").objectReferenceValue         = typeTmp;
            if (selectBtn != null) so.FindProperty("_selectButton").objectReferenceValue     = selectBtn;
            if (cg != null)        so.FindProperty("_canvasGroup").objectReferenceValue      = cg;

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

        GameObject waveTextObj = EnsureChildObject(hudPanelObj.transform, "WaveText");
        if (waveTextObj.GetComponent<TextMeshProUGUI>() == null) waveTextObj.AddComponent<TextMeshProUGUI>();

        GameObject scoreTextObj = EnsureChildObject(hudPanelObj.transform, "ScoreText");
        if (scoreTextObj.GetComponent<TextMeshProUGUI>() == null) scoreTextObj.AddComponent<TextMeshProUGUI>();

        GameObject lriObj = EnsureChildObject(hudPanelObj.transform, "LaunchReadyIndicator");
        CanvasGroup lriCg = lriObj.GetComponent<CanvasGroup>();
        if (lriCg == null) lriCg = lriObj.AddComponent<CanvasGroup>();

        SerializedObject so = new SerializedObject(hudPanel);
        so.FindProperty("_waveText").objectReferenceValue              = waveTextObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_scoreText").objectReferenceValue             = scoreTextObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_launchReadyIndicator").objectReferenceValue  = lriObj;
        so.FindProperty("_launchReadyCanvasGroup").objectReferenceValue = lriCg;

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

        GameObject scoreTextObj = EnsureChildObject(resultPanelObj.transform, "ScoreText");
        if (scoreTextObj.GetComponent<TextMeshProUGUI>() == null) scoreTextObj.AddComponent<TextMeshProUGUI>();

        GameObject restartBtnObj = EnsureChildObject(resultPanelObj.transform, "RestartButton");
        if (restartBtnObj.GetComponent<Button>() == null) restartBtnObj.AddComponent<Button>();

        SerializedObject so = new SerializedObject(resultPanel);
        so.FindProperty("_resultTitleText").objectReferenceValue = titleTextObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_finalScoreText").objectReferenceValue  = scoreTextObj.GetComponent<TextMeshProUGUI>();
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
    // 유틸리티
    // ──────────────────────────────────────────

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
