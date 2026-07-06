using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageTextFx : MonoBehaviour, IPoolable
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _floatDist     = 1f;
    [SerializeField] private float _duration      = 0.8f;
    [SerializeField] private float _criticalScale = 1.5f;
    [SerializeField] private Color _normalColor   = Color.white;
    [SerializeField] private Color _criticalColor = new Color32(255, 75, 62, 255);

    public void OnSpawn()
    {
        DOTween.Kill(transform);
        if (_text != null)
        {
            var c = _text.color;
            c.a = 1f;
            _text.color = c;
        }
        transform.localScale = Vector3.one;
    }

    public void OnDespawn()
    {
        DOTween.Kill(transform);
        DOTween.Kill(_text);
    }

    public void Play(Vector3 worldPos, float damage, bool isCritical)
    {
        transform.position = worldPos;
        _text.text         = isCritical ? $"<b>{Mathf.RoundToInt(damage)}</b>" : Mathf.RoundToInt(damage).ToString();
        _text.color        = isCritical ? _criticalColor : _normalColor;
        transform.localScale = isCritical ? Vector3.one * _criticalScale : Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMoveY(worldPos.y + _floatDist, _duration));
        seq.Join(_text.DOFade(0f, _duration));
        seq.OnComplete(() => DamageTextManager.Instance.Return(this));
    }
}
