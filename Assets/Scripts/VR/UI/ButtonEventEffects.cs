using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonEventEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image hoverImage;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(hoverImage)
        {
            hoverImage.enabled = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverImage)
        {
            hoverImage.enabled = false;
        }
    }
}
