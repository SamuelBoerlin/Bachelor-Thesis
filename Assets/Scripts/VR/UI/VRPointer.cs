using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class VRPointer : MonoBehaviour
{
    [SerializeField] private float maxLength = 5.0f;

    [SerializeField] private VRSculpting _sculpting;
    public VRSculpting VRSculpting
    {
        get
        {
            return _sculpting;
        }
        set
        {
            _sculpting = value;
        }
    }

    [SerializeField] private LineRenderer lineRenderer;

    [SerializeField] private GameObject canvasCursorPrefab;

    private GameObject canvasCursor;

    private SteamVR_Events.Action renderModelLoadedAction;

    private VRPointerInputModule inputModule;

    private void Awake()
    {
        renderModelLoadedAction = SteamVR_Events.RenderModelLoadedAction(OnRenderModelLoaded);

        VRPointerInputModule.OnVRPointerInputModuleInitialized += OnVRPointerInputModuleInitialized;
    }

    private void OnDestroy()
    {
        VRPointerInputModule.OnVRPointerInputModuleInitialized -= OnVRPointerInputModuleInitialized;
    }

    private void OnVRPointerInputModuleInitialized(object sender, VRPointerInputModule.Args args)
    {
        inputModule = args.Module;
    }

    private void OnEnable()
    {
        renderModelLoadedAction.enabled = true;
    }

    private void OnDisable()
    {
        renderModelLoadedAction.enabled = false;
    }

    private void OnRenderModelLoaded(SteamVR_RenderModel loadedRenderModel, bool success)
    {
        //Check if render model is the main model of the hand and if so move them pointer there
        if (transform.parent != null)
        {
            var hand = transform.parent.GetComponent<Hand>();
            if (hand != null && hand.mainRenderModel != null && hand.mainRenderModel.GetComponentInChildren<SteamVR_RenderModel>() == loadedRenderModel)
            {
                transform.parent = loadedRenderModel.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = _sculpting.IsPointerActive;

            bool hasCursor = false;

            Vector3 lineEnd = Vector3.zero;

            if (canvasCursorPrefab != null && inputModule.EventData != null && inputModule.EventData.pointerCurrentRaycast.gameObject != null)
            {
                var canvas = inputModule.EventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    if (canvasCursor == null)
                    {
                        canvasCursor = Instantiate(canvasCursorPrefab);
                    }

                    canvasCursor.transform.position = inputModule.EventData.pointerCurrentRaycast.worldPosition;
                    canvasCursor.transform.rotation = canvas.transform.rotation;

                    lineEnd = canvasCursor.transform.position;

                    hasCursor = true;
                }
            }

            if (!hasCursor)
            {
                if (canvasCursor != null)
                {
                    Destroy(canvasCursor);
                    canvasCursor = null;
                }

                float pointerLength = inputModule != null && inputModule.EventData != null && inputModule.EventData.pointerCurrentRaycast.distance > 0 ? inputModule.EventData.pointerCurrentRaycast.distance : maxLength;

                var ray = new Ray(transform.position, transform.forward);
                Physics.Raycast(ray, out RaycastHit hit, pointerLength);

                lineEnd = transform.position + (transform.forward * pointerLength);
                if (hit.collider != null)
                {
                    lineEnd = hit.point;
                }
            }

            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, transform.worldToLocalMatrix.MultiplyPoint(lineEnd));
        }
    }
}
