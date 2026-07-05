#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BackgroundGridFitSetupEditor
{
    private const float CellAspectCorrection = 1.647f;
    private const float GridAreaWidth = 14.53f;
    private const float GridAreaHeight = 10.16f;

    [MenuItem("PurpleCow/Setup/Background Grid Fit Setup")]
    private static void SetupBackgroundGridFit()
    {
        ConnectBackgroundFitter();
        ConnectWallFitter();

        Debug.Log("[BackgroundGridFitSetupEditor] Background Grid Fit Setup 완료.");
    }

    private static void ConnectBackgroundFitter()
    {
        BackgroundFitter fitter = Object.FindFirstObjectByType<BackgroundFitter>();
        if (fitter == null)
        {
            Debug.LogWarning("[BackgroundGridFitSetupEditor] BackgroundFitter 컴포넌트를 찾을 수 없어 건너뜁니다.");
            return;
        }

        SerializedObject so = new SerializedObject(fitter);
        so.FindProperty("_cellAspectCorrection").floatValue = CellAspectCorrection;
        so.FindProperty("_gridAreaWidth").floatValue = GridAreaWidth;
        so.FindProperty("_gridAreaHeight").floatValue = GridAreaHeight;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[BackgroundGridFitSetupEditor] BackgroundFitter 필드 주입 완료.");
    }

    private static void ConnectWallFitter()
    {
        WallFitter fitter = Object.FindFirstObjectByType<WallFitter>();
        if (fitter == null)
        {
            Debug.LogWarning("[BackgroundGridFitSetupEditor] WallFitter 컴포넌트를 찾을 수 없어 건너뜁니다.");
            return;
        }

        SerializedObject so = new SerializedObject(fitter);
        so.FindProperty("_cellAspectCorrection").floatValue = CellAspectCorrection;
        so.FindProperty("_gridAreaWidth").floatValue = GridAreaWidth;
        so.FindProperty("_gridAreaHeight").floatValue = GridAreaHeight;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[BackgroundGridFitSetupEditor] WallFitter 필드 주입 완료.");
    }
}
#endif
