using UnityEngine;
using UnityEngine.UI;

public class MonsterHpBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    private MonsterBase _monster;

    private void Start()
    {
        _monster = GetComponentInParent<MonsterBase>();
        if (_monster != null)
            _monster.OnHpChanged += UpdateHp;
    }

    private void OnDisable()
    {
        if (_monster != null)
            _monster.OnHpChanged -= UpdateHp;
    }

    private void UpdateHp(float current, float max)
    {
        _slider.value = max > 0f ? current / max : 0f;
    }
}
