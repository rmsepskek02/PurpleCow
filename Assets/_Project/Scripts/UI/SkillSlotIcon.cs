using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotIcon : MonoBehaviour
{
    [SerializeField] private Image      _iconImage;
    [SerializeField] private TMP_Text   _levelText;
    [SerializeField] private GameObject _filledRoot;
    [SerializeField] private GameObject _emptyRoot;

    public void SetFilled(Sprite icon, int level)
    {
        _filledRoot.SetActive(true);
        _emptyRoot.SetActive(false);
        _iconImage.sprite = icon;
        _iconImage.preserveAspect = true;
        _levelText.text   = level >= 3 ? "Max" : $"x{level}";
    }

    public void SetEmpty()
    {
        _filledRoot.SetActive(false);
        _emptyRoot.SetActive(true);
    }
}
