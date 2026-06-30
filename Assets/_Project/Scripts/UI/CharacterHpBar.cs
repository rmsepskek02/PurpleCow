using UnityEngine;
using UnityEngine.UI;

public class CharacterHpBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private void OnEnable()  => CharacterManager.OnHpChanged += UpdateHp;
    private void OnDisable() => CharacterManager.OnHpChanged -= UpdateHp;

    private void UpdateHp(int current, int max)
    {
        _slider.value = max > 0 ? (float)current / max : 0f;
    }
}
