using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float _pressedScale  = 0.9f;
    [SerializeField] private float _animDuration  = 0.1f;

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(_pressedScale, _animDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(1f, _animDuration);
    }
}
