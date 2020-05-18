using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;

public class VRPointerInputModule : BaseInputModule
{
    [SerializeField] private Camera eventCamera;
    [SerializeField] private SteamVR_Input_Sources inputSource;
    [SerializeField] private SteamVR_Action_Boolean clickAction;

    private GameObject hoveredGameObject;

    public PointerEventData EventData
    {
        private set;
        get;
    }

    protected override void Awake()
    {
        base.Awake();
        EventData = new PointerEventData(eventSystem);
    }

    public override void Process()
    {
        UpdateEventData();

        HandlePointerExitAndEnter(EventData, hoveredGameObject);

        if (clickAction.GetStateDown(inputSource))
        {
            HandlePointerPress();
        }

        if (clickAction.GetStateUp(inputSource))
        {
            HandlePointerRelease();
        }
    }

    private void UpdateEventData()
    {
        EventData.Reset();
        EventData.position = new Vector2(eventCamera.pixelWidth / 2, eventCamera.pixelHeight / 2);

        eventSystem.RaycastAll(EventData, m_RaycastResultCache);
        EventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        hoveredGameObject = EventData.pointerCurrentRaycast.gameObject;

        m_RaycastResultCache.Clear();
    }

    private void HandlePointerPress()
    {
        EventData.pointerPressRaycast = EventData.pointerCurrentRaycast;

        GameObject pointerPress = ExecuteEvents.ExecuteHierarchy(hoveredGameObject, EventData, ExecuteEvents.pointerDownHandler);

        if (pointerPress == null)
        {
            pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hoveredGameObject);
        }

        EventData.pressPosition = EventData.position;
        EventData.pointerPress = pointerPress;
        EventData.rawPointerPress = hoveredGameObject;
    }

    private void HandlePointerRelease()
    {
        ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerUpHandler);

        GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hoveredGameObject);

        if (EventData.pointerPress == pointerUpHandler)
        {
            ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerUpHandler);
        }

        eventSystem.SetSelectedGameObject(null);

        EventData.pressPosition = Vector2.zero;
        EventData.pointerPress = null;
        EventData.rawPointerPress = null;
    }
}
