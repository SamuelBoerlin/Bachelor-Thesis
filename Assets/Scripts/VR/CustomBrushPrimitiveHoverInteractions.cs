using UnityEngine;
using System.Collections;
using Valve.VR.InteractionSystem;
using Voxel;
using System.Collections.Generic;
using Valve.VR;
using UnityEngine.EventSystems;

public class CustomBrushPrimitiveHoverInteractions : VRPointerRaycastHandler
{
    [SerializeField] private MeshRenderer outlineRenderer;

    private bool externalHighlight = false;
    private bool internalHighlight = false;

    public bool HasMoved
    {
        private set;
        get;
    }

    [SerializeField] private float positionUpdateThreshold = 0.001f;
    [SerializeField] private float angleUpdateThreshold = 0.1f;

    private Vector3 lastCheckPosition;
    private Vector3 lastCheckRotation;

    private void Start()
    {
        outlineRenderer.enabled = false;
        lastCheckPosition = transform.position;
        lastCheckRotation = transform.rotation.eulerAngles;
    }

    private void Update()
    {
        HasMoved = false;

        var angleDiff = lastCheckRotation - transform.root.eulerAngles;
        if (Mathf.Abs(angleDiff.x) >= angleUpdateThreshold || Mathf.Abs(angleDiff.y) >= angleUpdateThreshold || Mathf.Abs(angleDiff.z) >= angleUpdateThreshold || (lastCheckPosition - transform.position).magnitude >= positionUpdateThreshold)
        {
            HasMoved = true;
            lastCheckPosition = transform.position;
            lastCheckRotation = transform.rotation.eulerAngles;
        }
    }

    private void OnHandHoverBegin(Hand hand)
    {
        internalHighlight = true;
        outlineRenderer.enabled = internalHighlight || externalHighlight;
    }

    private void OnHandHoverEnd(Hand hand)
    {
        internalHighlight = false;
        outlineRenderer.enabled = internalHighlight || externalHighlight;
    }

    public void SetOutlineEnabled(bool enabled)
    {
        externalHighlight = enabled;
        outlineRenderer.enabled = internalHighlight || externalHighlight;
    }

    public override void HandleRaycast(Vector3 origin, Vector3 start, Vector3 direction, out Vector3 hit, out RaycastMetadata metadata)
    {
        hit = start;
        metadata = null;
    }
}
