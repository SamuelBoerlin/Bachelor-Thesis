using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonEventEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image hoverImage;

    [SerializeField] private bool _permanentHover;
    public bool PermanentHover
    {
        get
        {
            return _permanentHover;
        }
        set
        {
            _permanentHover = value;
            if (_permanentHover)
            {
                hoverImage.enabled = true;
            }
            else if (!hovered)
            {
                hoverImage.enabled = false;
            }
        }
    }

    private bool hovered;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (hoverImage)
        {
            hoverImage.enabled = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        if (!_permanentHover && hoverImage)
        {
            hoverImage.enabled = false;
        }
    }
}
