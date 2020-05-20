using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;

public class VRPointerInputModule : BaseInputModule
{
    public class Args : EventArgs
    {
        public VRPointerInputModule Module
        {
            get;
            internal set;
        }
    }

    public static event EventHandler<Args> OnVRPointerInputModuleInitialized;

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
        EventSystem.current = eventSystem;
    }

    protected override void Start()
    {
        base.Start();

        OnVRPointerInputModuleInitialized?.Invoke(this, new Args()
        {
            Module = this
        });
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnVRPointerInputModuleInitialized = null;
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

        eventSystem.SetSelectedGameObject(hoveredGameObject);
    }

    private void HandlePointerRelease()
    {
        ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerUpHandler);

        if (EventData.rawPointerPress == hoveredGameObject)
        {
            ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerClickHandler);
        }

        eventSystem.SetSelectedGameObject(null);

        EventData.pressPosition = Vector2.zero;
        EventData.pointerPress = null;
        EventData.rawPointerPress = null;
    }
}
