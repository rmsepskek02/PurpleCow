using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IPointerExitHandler, ICancelHandler
{
    [SerializeField] private float _pressedScale  = 0.9f;
    [SerializeField] private float _animDuration  = 0.1f;

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateScale(_pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateScale(1f);
    }

    public void OnPointerExit(PointerEventData eventData) => AnimateScale(1f);
    public void OnCancel(BaseEventData eventData) => AnimateScale(1f);
    private void OnDisable() { transform.DOKill(); transform.localScale = Vector3.one; }

    private void AnimateScale(float scale)
    {
        transform.DOKill();
        transform.DOScale(scale, _animDuration).SetUpdate(true);
    }
}
