#if UNITY_EDITOR
using UnityEditor;
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

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
        StretchFill(safeAreaPanel.GetComponent<RectTransform>() ?? safeAreaPanel.AddComponent<RectTransform>());
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
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("[UISetupEditor] SkillCard.prefab 이미 존재, 스킵.");
            return;
        }

        GameObject go = new GameObject("SkillCard");
        go.AddComponent<RectTransform>();
        EnsureComponent<CanvasGroup>(go);

        // 아이콘 이미지
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(go.transform);
        iconObj.AddComponent<RectTransform>();
        iconObj.AddComponent<Image>();

        // 이름 텍스트
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(go.transform);
        nameObj.AddComponent<RectTransform>();
        nameObj.AddComponent<TextMeshProUGUI>();

        // 설명 텍스트
        GameObject descObj = new GameObject("DescText");
        descObj.transform.SetParent(go.transform);
        descObj.AddComponent<RectTransform>();
        descObj.AddComponent<TextMeshProUGUI>();

        // 타입 텍스트
        GameObject typeObj = new GameObject("TypeText");
        typeObj.transform.SetParent(go.transform);
        typeObj.AddComponent<RectTransform>();
        typeObj.AddComponent<TextMeshProUGUI>();

        // 선택 버튼
        GameObject btnObj = new GameObject("SelectButton");
        btnObj.transform.SetParent(go.transform);
        btnObj.AddComponent<RectTransform>();
        Button btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btnObj.AddComponent<UIButton>();

        go.AddComponent<SkillCardUI>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[UISetupEditor] SkillCard.prefab 생성 완료. Inspector에서 SerializedField 참조 연결 필요.");
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

        SerializedObject so = new SerializedObject(dtm);
        if (fxPrefab != null)
            so.FindProperty("_prefab").objectReferenceValue = fxPrefab;
        else
            Debug.LogWarning("[UISetupEditor] DamageTextFx.prefab 없음.");

        if (poolParent != null)
            so.FindProperty("_poolParent").objectReferenceValue = poolParent;
        else
            Debug.LogWarning("[UISetupEditor] DamageTextPool 없음. DamageTextManager 하위에 DamageTextPool 오브젝트가 필요합니다.");

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UISetupEditor] DamageTextManager 참조 연결 완료.");
    }

    // ──────────────────────────────────────────
    // 유틸리티
    // ──────────────────────────────────────────

    private static GameObject EnsureChildObject(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
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
