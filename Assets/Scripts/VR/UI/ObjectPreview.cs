using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ObjectPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private ObjectToTextureRenderManager _objectRenderer;
    public ObjectToTextureRenderManager ObjectRenderer
    {
        get
        {
            return _objectRenderer;
        }
        set
        {
            _objectRenderer = value;
        }
    }

    [SerializeField] private GameObject _renderObject;
    public GameObject RenderObject
    {
        get
        {
            return _renderObject;
        }
        set
        {
            if (_renderObject != value)
            {
                UpdateTexture = true;
            }
            _renderObject = value;
        }
    }

    [SerializeField] private RawImage image;

    [Serializable]
    private struct PreviewTransform
    {
        [SerializeField] public Vector3 position;
        [SerializeField] public Vector3 rotation1;
        [SerializeField] public Vector3 rotation2;
        [SerializeField] public Vector3 scale;
    }

    [SerializeField]
    private PreviewTransform bakedTransform = new PreviewTransform()
    {
        position = new Vector3(0, 0, 2),
        rotation1 = new Vector3(-22.5f, 0, 0),
        rotation2 = new Vector3(0, 45, 0),
        scale = Vector3.one
    };

    [SerializeField]
    private PreviewTransform virtualTransform = new PreviewTransform()
    {
        position = new Vector3(0, 0, -0.3f),
        rotation1 = new Vector3(-22.5f, 0, 0),
        rotation2 = new Vector3(0, 45, 0),
        scale = Vector3.one * 0.25f
    };

    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;

    [SerializeField] private bool setKinematicToFalseOnRelease = true;
    [SerializeField] private bool setScaleToOriginalOnRelease = false;

    [SerializeField] private Animator virtualPreviewAnimator;

    [SerializeField] private bool updateTextureEveryFrame = false;

    [Serializable]
    public class ReleaseEvent : UnityEvent<GameObject> { }
    public ReleaseEvent onReleased;

    public bool UpdateTexture
    {
        get;
        set;
    } = true;

    private Canvas canvas;

    private RenderTexture texture;

    private GameObject virtualPreviewContainer;
    private GameObject virtualPreviewObject;

    private bool isPointingAt = false;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (_renderObject != null && (UpdateTexture || updateTextureEveryFrame))
        {
            UpdateTexture = false;
            _objectRenderer.QueueRenderer(_renderObject, bakedTransform.position, Quaternion.Euler(bakedTransform.rotation1) * Quaternion.Euler(bakedTransform.rotation2), bakedTransform.scale, width, height, ref texture);
            image.texture = texture;
            image.enabled = true;
        }

        if (virtualPreviewContainer != null)
        {
            UpdateContainerTransform(virtualPreviewContainer);
        }

        virtualPreviewAnimator.SetBool("IsPreviewing", virtualPreviewContainer != null && isPointingAt);
        virtualPreviewAnimator.Update(0); //Makes sure that the animator's state is up to date

        if (virtualPreviewContainer != null)
        {
            //Check if preview is no longer selected and animator has finished its animation
            if (!isPointingAt && virtualPreviewAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !virtualPreviewAnimator.IsInTransition(0))
            {
                Destroy(virtualPreviewObject);
                virtualPreviewObject = null;

                Destroy(virtualPreviewContainer);
                virtualPreviewContainer = null;
            }

            //Check if preview object has been released
            if (virtualPreviewObject != null && virtualPreviewObject.transform.parent != virtualPreviewContainer.transform)
            {
                Destroy(virtualPreviewContainer);
                virtualPreviewContainer = null;

                if (setKinematicToFalseOnRelease)
                {
                    var rb = virtualPreviewObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                    }
                }

                if (setScaleToOriginalOnRelease && _renderObject != null)
                {
                    virtualPreviewObject.transform.localScale = _renderObject.transform.localScale;
                }

                if (onReleased != null)
                {
                    onReleased.Invoke(virtualPreviewObject);
                }

                virtualPreviewObject = null;
            }
        }
    }

    private void OnDestroy()
    {
        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }

        if (virtualPreviewObject != null)
        {
            Destroy(virtualPreviewObject);
            virtualPreviewObject = null;
        }

        if (virtualPreviewContainer != null)
        {
            Destroy(virtualPreviewContainer);
            virtualPreviewContainer = null;
        }
    }

    private void UpdateContainerTransform(GameObject container)
    {
        var scale = new Vector3(1.0f / canvas.transform.localScale.x * virtualTransform.scale.x, 1.0f / canvas.transform.localScale.y * virtualTransform.scale.y, 1.0f / canvas.transform.localScale.z * virtualTransform.scale.z);
        container.transform.localScale = scale;
        container.transform.localPosition = new Vector3(virtualTransform.position.x * scale.x, virtualTransform.position.y * scale.y, virtualTransform.position.z * scale.z);
        container.transform.localRotation = Quaternion.Euler(virtualTransform.rotation1) * Quaternion.Euler(virtualTransform.rotation2);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (virtualPreviewContainer == null && _renderObject != null)
        {
            virtualPreviewContainer = new GameObject("Virtual Preview Container");
            virtualPreviewContainer.transform.parent = transform;
            UpdateContainerTransform(virtualPreviewContainer);

            virtualPreviewObject = Instantiate(_renderObject, virtualPreviewContainer.transform);
            virtualPreviewObject.transform.localScale = Vector3.one;
            virtualPreviewObject.transform.localPosition = Vector3.zero;
            virtualPreviewObject.transform.localRotation = Quaternion.identity;

            var rb = virtualPreviewObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            virtualPreviewObject.SetActive(true);
        }

        virtualPreviewAnimator.SetTrigger("PreviewRendererEnter");

        isPointingAt = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        virtualPreviewAnimator.SetTrigger("PreviewRendererExit");

        isPointingAt = false;
    }
}
