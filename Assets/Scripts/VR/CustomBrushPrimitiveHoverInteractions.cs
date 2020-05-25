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

    public void Start()
    {
        outlineRenderer.enabled = false;
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
