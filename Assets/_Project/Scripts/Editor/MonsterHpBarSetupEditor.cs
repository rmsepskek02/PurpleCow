#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 몬스터 HP바 그래픽(Border/Background/Fill Area/Fill) 생성 및 CanvasGroup 세팅 전용 에디터.
// MonsterOverhaulSetupEditor.cs는 이 작업에서 전혀 수정하지 않으며, 대상 프리팹 경로도 이 스크립트 안에 별도로 나열한다.
public static class MonsterHpBarSetupEditor
{
    private static readonly string[] PrefabPaths =
    {
        "Assets/_Project/Prefabs/Monster/Fluffy.prefab",
        "Assets/_Project/Prefabs/Monster/Spider.prefab",
        "Assets/_Project/Prefabs/Monster/StoneBug.prefab",
        "Assets/_Project/Prefabs/Monster/ForestDeer.prefab",
    };

    private static readonly Color BorderColor = new Color(0.353f, 0.063f, 0.059f, 1f); // #5A100F
    private static readonly Color BackgroundColor = new Color(0.173f, 0.173f, 0.173f, 1f); // #2C2C2C

    [MenuItem("PurpleCow/Setup/Monster HP Bar Setup")]
    private static void SetupMonsterHpBars()
    {
        foreach (string prefabPath in PrefabPaths)
        {
            SetupMonsterHpBar(prefabPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MonsterHpBarSetupEditor] Monster HP Bar Setup 완료.");
    }

    private static void SetupMonsterHpBar(string prefabPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            Debug.LogWarning($"[MonsterHpBarSetupEditor] {prefabPath} 없음, 스킵.");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            Slider slider = root.GetComponentInChildren<Slider>(true);
            if (slider == null)
            {
                Debug.LogWarning($"[MonsterHpBarSetupEditor] {prefabPath}에서 Slider(HpSlider)를 찾을 수 없음, 스킵.");
                return;
            }

            Canvas hpBarCanvas = slider.GetComponentInParent<Canvas>();
            if (hpBarCanvas != null)
            {
                CanvasGroup canvasGroup = hpBarCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = hpBarCanvas.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                }
            }
            else
            {
                Debug.LogWarning($"[MonsterHpBarSetupEditor] {prefabPath}에서 HpBarCanvas(Canvas)를 찾을 수 없음.");
            }

            if (slider.fillRect != null)
            {
                Debug.Log($"[MonsterHpBarSetupEditor] {prefabPath}는 이미 Fill 구조가 설정되어 있음, 스킵.");
                return;
            }

            RectTransform sliderRect = slider.transform as RectTransform;

            RectTransform border = CreateFullRect("Border", sliderRect);
            Image borderImage = border.gameObject.AddComponent<Image>();
            borderImage.color = BorderColor;
            borderImage.raycastTarget = false;

            RectTransform background = CreateChildRect("Background", border, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.85f));
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = BackgroundColor;
            backgroundImage.raycastTarget = false;

            RectTransform fillArea = CreateFullRect("Fill Area", background);

            RectTransform fill = CreateFullRect("Fill", fillArea);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = BorderColor;
            fillImage.raycastTarget = false;

            slider.fillRect = fill;
            slider.interactable = false;

            Debug.Log($"[MonsterHpBarSetupEditor] {prefabPath} HP바 그래픽(Border/Background/Fill) 생성 및 fillRect 연결 완료.");
        }
    }

    private static RectTransform CreateFullRect(string name, Transform parent)
    {
        return CreateChildRect(name, parent, Vector2.zero, Vector2.one);
    }

    private static RectTransform CreateChildRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }
}
#endif
