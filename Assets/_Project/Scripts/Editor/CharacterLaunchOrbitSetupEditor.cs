#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Character를 BallLauncher의 기본 발사 위치(기존 LaunchPoint 기본값과 동일한 로컬 좌표)로 배치하고,
// WallFitter._character 참조를 연결하는 신규 배선 전용 에디터 스크립트.
// SceneSetupEditor.cs는 이미 안정화된 공용 자동화 스크립트이므로 이번 재설계로 새로 필요해진
// 배선만 이 파일에서 별도로 처리한다.
public static class CharacterLaunchOrbitSetupEditor
{
    [MenuItem("PurpleCow/Setup/Character LaunchPoint Orbit Setup")]
    private static void SetupCharacterLaunchOrbit()
    {
        Transform character = FindCharacter();
        if (character != null)
        {
            character.localPosition = new Vector3(0f, -8f, 0f);
            Debug.Log("[CharacterLaunchOrbitSetupEditor] Character 초기 위치 설정 완료.");
        }

        ConnectWallFitterCharacterRef(character);
    }

    private static Transform FindCharacter()
    {
        GameObject launcherObj = GameObject.Find("BallLauncher");
        if (launcherObj == null)
        {
            Debug.LogWarning("[CharacterLaunchOrbitSetupEditor] BallLauncher 오브젝트를 찾을 수 없어 Character 위치 설정을 건너뜁니다.");
            return null;
        }

        Transform character = launcherObj.transform.Find("Character");
        if (character == null)
        {
            Debug.LogWarning("[CharacterLaunchOrbitSetupEditor] Character 오브젝트를 찾을 수 없어 Character 위치 설정을 건너뜁니다.");
            return null;
        }

        return character;
    }

    private static void ConnectWallFitterCharacterRef(Transform character)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[CharacterLaunchOrbitSetupEditor] Main Camera를 찾을 수 없어 WallFitter 연동을 건너뜁니다.");
            return;
        }

        WallFitter fitter = mainCamera.GetComponent<WallFitter>();
        if (fitter == null)
        {
            Debug.LogWarning("[CharacterLaunchOrbitSetupEditor] WallFitter 컴포넌트를 찾을 수 없어 연동을 건너뜁니다.");
            return;
        }

        SerializedObject so = new SerializedObject(fitter);
        so.FindProperty("_character").objectReferenceValue = character;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[CharacterLaunchOrbitSetupEditor] WallFitter._character 연동 완료.");
    }
}
#endif
