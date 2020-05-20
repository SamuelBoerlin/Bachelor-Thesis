using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BrushSelectionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool Hovered
    {
        private set;
        get;
    }

    public Button Button
    {
        private set;
        get;
    }

    private void Awake()
    {
        Button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hovered = false;
    }
}
