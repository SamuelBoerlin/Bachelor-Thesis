using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Voxel;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Canvas))]
public class BrushSelectionMenu : MonoBehaviour
{
    [SerializeField] private GameObject brushButtonPrefab;

    [SerializeField] private Vector2 wheelCenter = Vector2.zero;
    [SerializeField] private float wheelRadius = 300.0f;

    [SerializeField] private BrushType[] selectableBrushTypes;

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

    [SerializeField] private Vector3 brushPreviewOffset = new Vector3(0, 0, -0.25f);
    [SerializeField] private float brushPreviewScale = 0.01f;
    [SerializeField] private Material brushPreviewHightlightMaterial;

    private Dictionary<BrushSelectionButton, BrushType> buttons = new Dictionary<BrushSelectionButton, BrushType>();

    private void Start()
    {
        var canvas = GetComponent<Canvas>();

        float step = Mathf.PI * 2 / selectableBrushTypes.Length;
        float angle = Mathf.PI / 2;

        foreach (var type in selectableBrushTypes)
        {
            var buttonObject = Instantiate(brushButtonPrefab, transform);

            buttonObject.transform.localPosition = wheelCenter + new Vector2(Mathf.Cos(angle) * wheelRadius, Mathf.Sin(angle) * wheelRadius);

            var buttonScript = buttonObject.GetComponent<BrushSelectionButton>();
            buttonScript.Button.onClick.AddListener(() =>
            {
                _sculpting.BrushType = type;
            });

            buttons.Add(buttonScript, type);

            angle -= step;
        }
    }

    private void LateUpdate()
    {
        ISdf renderSdf = VRSculpting.CreateSdf(VRSculpting.BrushType);
        if (renderSdf != null)
        {
            Matrix4x4 renderTransform = Matrix4x4.TRS(transform.localToWorldMatrix.MultiplyPoint(new Vector3(wheelCenter.x, wheelCenter.y, 0)), transform.rotation, Vector3.one * brushPreviewScale);
            VRSculpting.BrushRenderer.Render(renderTransform, renderSdf);
        }

        foreach (var entry in buttons)
        {
            renderSdf = VRSculpting.CreateSdf(entry.Value);
            if (renderSdf != null)
            {
                Matrix4x4 renderTransform = Matrix4x4.TRS(entry.Key.transform.position + entry.Key.transform.rotation * brushPreviewOffset, entry.Key.transform.rotation, brushPreviewScale * entry.Key.transform.localScale);

                if (brushPreviewHightlightMaterial != null && entry.Key.Hovered)
                {
                    VRSculpting.BrushRenderer.Render(renderTransform, renderSdf, brushPreviewHightlightMaterial);
                }
                else
                {
                    VRSculpting.BrushRenderer.Render(renderTransform, renderSdf);
                }
            }
        }
    }
}
