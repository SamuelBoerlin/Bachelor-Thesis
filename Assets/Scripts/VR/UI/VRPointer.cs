using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VRPointer : MonoBehaviour
{
    [SerializeField] private float maxLength = 5.0f;

    [SerializeField] private VRPointerInputModule inputModule;

    [SerializeField] private LineRenderer lineRenderer;

    private void Update()
    {
        if (lineRenderer != null)
        {
            float pointerLength = inputModule != null && inputModule.EventData != null && inputModule.EventData.pointerCurrentRaycast.distance > 0 ? inputModule.EventData.pointerCurrentRaycast.distance : maxLength;

            var ray = new Ray(transform.position, transform.forward);
            Physics.Raycast(ray, out RaycastHit hit, pointerLength);

            var lineEnd = transform.position + (transform.forward * pointerLength);
            if (hit.collider != null)
            {
                lineEnd = hit.point;
            }

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, lineEnd);
        }
    }
}
