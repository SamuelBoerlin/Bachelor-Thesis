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

    [SerializeField] private float dragThreshold = 0.005f;

    public GameObject HoveredGameObject
    {
        private set;
        get;
    }

    private GameObject pressedGameObject;
    private Vector3 pressedPosition;

    public PointerEventData EventData
    {
        private set;
        get;
    }

    public VRPointerRaycastHandler.RaycastMetadata RaycastMetadata
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

        HandlePointerExitAndEnter(EventData, HoveredGameObject);

        if (clickAction.GetStateDown(inputSource))
        {
            HandlePointerPress();
        }

        if (clickAction.GetState(inputSource))
        {
            HandlePointerHeld();
        }

        if (clickAction.GetStateUp(inputSource))
        {
            HandlePointerRelease();
        }
    }

    private RaycastResult FindCachedRaycastAndSetMeta()
    {
        var result = FindFirstRaycast(m_RaycastResultCache);

        VRPointerRaycastHandler.RaycastMetadata meta = null;
        if (result.gameObject == null && Physics.Raycast(eventCamera.transform.position, eventCamera.transform.forward, out RaycastHit physicsRaycast, 100.0f))
        {
            var handler = physicsRaycast.collider.gameObject.GetComponent<VRPointerRaycastHandler>();
            if (handler != null)
            {
                handler.HandleRaycast(eventCamera.transform.position, physicsRaycast.point, eventCamera.transform.forward, out Vector3 hit, out meta);
                result = new RaycastResult
                {
                    distance = (hit - eventCamera.transform.position).magnitude,
                    gameObject = physicsRaycast.collider.gameObject,
                    screenPosition = EventData.position,
                    worldPosition = hit
                };
            }
        }

        RaycastMetadata = meta;

        return result;
    }

    private void UpdateEventData()
    {
        EventData.Reset();
        EventData.position = new Vector2(eventCamera.pixelWidth / 2, eventCamera.pixelHeight / 2);

        eventSystem.RaycastAll(EventData, m_RaycastResultCache);
        EventData.pointerCurrentRaycast = FindCachedRaycastAndSetMeta();
        HoveredGameObject = EventData.pointerCurrentRaycast.gameObject;

        m_RaycastResultCache.Clear();
    }

    private Vector3 GetLocalPosition(GameObject reference, Vector3 worldPos)
    {
        return reference.transform.worldToLocalMatrix.MultiplyPoint(worldPos);
    }

    private void HandlePointerPress()
    {
        EventData.pointerPressRaycast = EventData.pointerCurrentRaycast;

        GameObject pointerPress = ExecuteEvents.ExecuteHierarchy(HoveredGameObject, EventData, ExecuteEvents.pointerDownHandler);

        if (pointerPress == null)
        {
            pointerPress = ExecuteEvents.ExecuteHierarchy(HoveredGameObject, EventData, ExecuteEvents.pointerClickHandler);
        }

        EventData.pressPosition = EventData.position;
        EventData.pointerPress = pointerPress;
        EventData.rawPointerPress = HoveredGameObject;

        eventSystem.SetSelectedGameObject(HoveredGameObject);

        if (HoveredGameObject != null)
        {
            pressedGameObject = HoveredGameObject;
            pressedPosition = EventData.pointerCurrentRaycast.worldPosition;
        }
        else
        {
            pressedGameObject = null;
            pressedPosition = Vector3.zero;
        }
    }

    private void HandlePointerHeld()
    {
        if (pressedGameObject != null)
        {
            if (!EventData.dragging)
            {
                if ((EventData.pointerCurrentRaycast.worldPosition - pressedPosition).magnitude > dragThreshold)
                {
                    GameObject pointerDrag = ExecuteEvents.ExecuteHierarchy(HoveredGameObject, EventData, ExecuteEvents.beginDragHandler);

                    if (pointerDrag == null)
                    {
                        pointerDrag = ExecuteEvents.ExecuteHierarchy(HoveredGameObject, EventData, ExecuteEvents.dragHandler);
                    }

                    EventData.dragging = true;
                    EventData.pointerDrag = pointerDrag;
                }
            }
            else if (EventData.pointerDrag != null)
            {
                ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.dragHandler);
            }
        }
    }

    private void HandlePointerRelease()
    {
        if (EventData.dragging)
        {
            EventData.dragging = false;
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.endDragHandler);
            EventData.pointerDrag = null;
        }

        ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerUpHandler);

        if (EventData.rawPointerPress == HoveredGameObject)
        {
            ExecuteEvents.Execute(EventData.pointerPress, EventData, ExecuteEvents.pointerClickHandler);
        }

        eventSystem.SetSelectedGameObject(null);

        EventData.pressPosition = Vector2.zero;
        EventData.pointerPress = null;
        EventData.rawPointerPress = null;
    }
}
