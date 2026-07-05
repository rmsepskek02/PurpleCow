using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterHpBar : MonoBehaviour
{
    [SerializeField] private Slider  _slider;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private Transform _orientationRoot;

    private void OnEnable()  => CharacterManager.OnHpChanged += UpdateHp;
    private void OnDisable() => CharacterManager.OnHpChanged -= UpdateHp;

    private void Start()
    {
        if (CharacterManager.Instance != null)
            UpdateHp(CharacterManager.Instance.CurrentHp, CharacterManager.Instance.MaxHp);
    }

    private void LateUpdate()
    {
        if (_orientationRoot == null || _orientationRoot.parent == null)
            return;

        Vector3 scale = _orientationRoot.localScale;
        float magnitude = Mathf.Abs(scale.x);
        scale.x = _orientationRoot.parent.lossyScale.x < 0f ? -magnitude : magnitude;
        _orientationRoot.localScale = scale;
    }

    private void UpdateHp(int current, int max)
    {
        _slider.value = max > 0 ? (float)current / max : 0f;
        _hpText.text  = $"{current}/{max}";
    }
}
