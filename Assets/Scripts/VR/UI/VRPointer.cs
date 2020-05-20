using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VRPointer : MonoBehaviour
{
    [SerializeField] private float maxLength = 5.0f;

    [SerializeField] private VRPointerInputModule inputModule;

    [SerializeField] private VRSculpting sculpting;

    [SerializeField] private LineRenderer lineRenderer;

    [SerializeField] private GameObject canvasCursorPrefab;

    private GameObject canvasCursor;

    private void LateUpdate()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = sculpting.IsPointerActive;

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

                    Debug.Log("Update cursor");

                    canvasCursor.transform.position = inputModule.EventData.pointerCurrentRaycast.worldPosition;
                    canvasCursor.transform.rotation = canvas.transform.rotation;

                    lineEnd = canvasCursor.transform.position;

                    hasCursor = true;
                }
            }

            if (!hasCursor)
            {
                if(canvasCursor != null)
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

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, lineEnd);
        }
    }
}
