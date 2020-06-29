using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Voxel;
using UnityEngine.EventSystems;
using Unity.Collections;
using Valve.VR;

[RequireComponent(typeof(Canvas))]
public class CustomBrushEditingMenu : MonoBehaviour
{
    [SerializeField] private SteamVR_Action_Boolean selectAction;

    [SerializeField] private GameObject customBrushPrimitivePrefab;

    [SerializeField] private Transform customBrushCenter;
    [SerializeField] private Transform primitiveSpawner;

    [SerializeField] private float maxPrimitiveDistance = 100.0f;

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

    [SerializeField] private bool _renderSurface = true;
    public bool RenderSurface
    {
        get
        {
            return _renderSurface;
        }
        set
        {
            _renderSurface = value;
            showSurfaceButton.GetComponent<ButtonEventEffects>().PermanentHover = value;
        }
    }

    [SerializeField] private bool _renderPrimitives = true;
    public bool RenderPrimitives
    {
        get
        {
            return _renderPrimitives;
        }
        set
        {
            _renderPrimitives = value;
            showPrimitivesButton.GetComponent<ButtonEventEffects>().PermanentHover = value;
        }
    }

    [SerializeField] private Slider blendSlider;

    [SerializeField] private Slider scaleXSlider;
    [SerializeField] private Slider scaleYSlider;
    [SerializeField] private Slider scaleZSlider;

    [SerializeField] private Button addButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button unionButton;
    [SerializeField] private Button differenceButton;
    [SerializeField] private Button showSurfaceButton;
    [SerializeField] private Button showPrimitivesButton;

    [SerializeField] private GameObject primitiveButtonPrefab;

    [SerializeField] private Vector3 primitivePreviewOffset = new Vector3(0, 0, 0);
    [SerializeField] private float primitivePreviewScale = 0.01f;
    [SerializeField] private int initialPrimitivePreviewTileXSpacing = 300;
    [SerializeField] private int initialPrimitivePreviewTileYSpacing = 0;
    [SerializeField] private int primitiveTilesPerRow = 3;
    [SerializeField] private int primitiveTileXSpacing = 210;
    [SerializeField] private int primitiveTileYSpacing = -210;
    [SerializeField] private Material primitivePreviewHightlightMaterial;

    public class CustomBrushPrimitiveEntry
    {
        public DefaultCustomBrushType type;
        public BrushOperation operation;
        public float blend;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public CustomBrushPrimitiveEntry(DefaultCustomBrushType type, BrushOperation operation, float blend, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.type = type;
            this.operation = operation;
            this.blend = blend;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }

    private Dictionary<GameObject, (CustomBrushPrimitiveHoverInteractions, CustomBrushPrimitiveEntry)> primitives = new Dictionary<GameObject, (CustomBrushPrimitiveHoverInteractions, CustomBrushPrimitiveEntry)>();

    private GameObject _selectedPrimitive;
    public GameObject SelectedPrimitive
    {
        private set
        {
            if (_selectedPrimitive != value)
            {
                _selectedPrimitive = value;
                OnChangeSelectedPrimitive();
            }
            else
            {
                _selectedPrimitive = value;
            }
        }
        get
        {
            return _selectedPrimitive;
        }
    }


    private Dictionary<BrushSelectionButton, DefaultCustomBrushType> primitiveButtons = new Dictionary<BrushSelectionButton, DefaultCustomBrushType>();

    private bool rebuildSurface = true;

    public void OnInitializeUI(VRUI.Context ctx)
    {
        VRSculpting = ctx.controller;

        foreach(var primitive in VRSculpting.CustomBrush.Instance.Primitives)
        {
            var primitiveTransform = (Matrix4x4)primitive.transform;
            InstantiatePrimitive(false, primitive.type, primitive.operation, primitive.blend, primitiveTransform.MultiplyPoint(Vector3.zero), primitiveTransform.rotation, primitiveTransform.lossyScale);
        }
    }

    private void Start()
    {
        blendSlider.onValueChanged.AddListener(OnBlendSliderValueChange);
        scaleXSlider.onValueChanged.AddListener(OnScaleXSliderValueChange);
        scaleYSlider.onValueChanged.AddListener(OnScaleYSliderValueChange);
        scaleZSlider.onValueChanged.AddListener(OnScaleZSliderValueChange);

        addButton.onClick.AddListener(OnAddButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        unionButton.onClick.AddListener(OnUnionButtonClicked);
        differenceButton.onClick.AddListener(OnDifferenceButtonClicked);

        showSurfaceButton.onClick.AddListener(OnShowSurfaceButtonClicked);
        showPrimitivesButton.onClick.AddListener(OnShowPrimitivesButtonClicked);

        //Update the button states
        RenderSurface = _renderSurface;
        RenderPrimitives = _renderPrimitives;

        OnChangeSelectedPrimitive();
    }

    private void ShowPrimitivesButtons()
    {
        RemovePrimitivesButtons();

        float xOffset = 10.0f;
        float yOffset = 0.0f;

        int i = 0;
        foreach (var type in DefaultCustomBrushType.ALL)
        {
            var buttonObject = Instantiate(primitiveButtonPrefab, addButton.transform);

            buttonObject.transform.localPosition = new Vector3(initialPrimitivePreviewTileXSpacing + xOffset, initialPrimitivePreviewTileYSpacing + yOffset, 0.0f);

            var buttonScript = buttonObject.GetComponent<BrushSelectionButton>();
            buttonScript.Button.onClick.AddListener(() =>
            {
                SelectedPrimitive = InstantiatePrimitive(true, type, BrushOperation.Union, 0.000001f, Vector3.zero, Quaternion.identity, Vector3.one);
                RemovePrimitivesButtons();
            });

            primitiveButtons.Add(buttonScript, type);

            xOffset += primitiveTileXSpacing;

            if (i > 0 && i % (primitiveTilesPerRow - 1) == 0)
            {
                xOffset = 0;
                yOffset += primitiveTileYSpacing;
            }

            i++;
        }

        addButton.GetComponent<ButtonEventEffects>().PermanentHover = true;
    }

    private void RemovePrimitivesButtons()
    {
        foreach (var button in primitiveButtons.Keys)
        {
            Destroy(button);
        }
        primitiveButtons.Clear();

        addButton.GetComponent<ButtonEventEffects>().PermanentHover = false;
    }

    private GameObject InstantiatePrimitive(bool setToSpawner, DefaultCustomBrushType type, BrushOperation operation, float blend, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var instance = Instantiate(customBrushPrimitivePrefab, customBrushCenter.transform);
        if(setToSpawner)
        {
            instance.transform.position = primitiveSpawner.transform.position;
        }
        else
        {
            var brushBaseTransform = Matrix4x4.TRS(Vector3.zero, customBrushCenter.rotation, VRSculpting.VoxelWorld.transform.localScale);
            instance.transform.position = customBrushCenter.position + brushBaseTransform.MultiplyPoint(position);
            instance.transform.rotation = brushBaseTransform.rotation * rotation;
        }
        primitives.Add(instance, (instance.GetComponent<CustomBrushPrimitiveHoverInteractions>(), new CustomBrushPrimitiveEntry(type, operation, blend, position, rotation, scale)));
        rebuildSurface = true;
        return instance;
    }

    private void Update()
    {
        if (VRSculpting != null)
        {
            var hovered = VRSculpting.InputModule.EventData.pointerCurrentRaycast.gameObject;

            if (selectAction.stateDown)
            {
                if (hovered == null)
                {
                    SelectedPrimitive = null;
                }
                else if (primitives.ContainsKey(hovered))
                {
                    SelectedPrimitive = hovered;
                }
            }

            var brushBaseTransform = Matrix4x4.TRS(Vector3.zero, customBrushCenter.rotation, VRSculpting.VoxelWorld.transform.localScale);
            var brushBaseTransformInverse = brushBaseTransform.inverse;

            var brush = VRSculpting.CustomBrush.Instance;
            brush.Primitives.Clear();

            bool rebuildPreview = false;

            foreach (var entry in primitives)
            {
                var obj = entry.Key;
                var interactions = entry.Value.Item1;
                var primitive = entry.Value.Item2;

                rebuildPreview |= interactions.HasMoved;

                var isSelected = hovered == obj || SelectedPrimitive == obj;
                interactions.SetOutlineEnabled(isSelected);

                var offset = brushBaseTransformInverse.MultiplyPoint(obj.transform.position - customBrushCenter.position);
                if (offset.magnitude > maxPrimitiveDistance)
                {
                    offset = offset.normalized * maxPrimitiveDistance;
                }
                primitive.position = offset;
                primitive.rotation = brushBaseTransformInverse.rotation * obj.transform.rotation;
                brush.AddPrimitive(primitive.type, primitive.operation, primitive.blend, Matrix4x4.TRS(primitive.position, primitive.rotation, primitive.scale));
            }

            //TODO Rebuilding seems to cause a memory leak?
            if (rebuildPreview || rebuildSurface)
            {
                rebuildSurface = false;
                VRSculpting.CustomBrush.CustomBrushRenderer.NeedsRebuild = true;
            }

            if (RenderSurface)
            {
                using (var sdf = brush.CreateSdf(Allocator.TempJob))
                {
                    VRSculpting.BrushRenderer.Render(Matrix4x4.TRS(customBrushCenter.position, Quaternion.identity, Vector3.one) * brushBaseTransform, sdf);
                }
            }

            if (RenderPrimitives)
            {
                foreach (var primitive in brush.Primitives)
                {
                    using (var renderSdf = brush.Evaluator.GetRenderSdf(primitive))
                    {
                        VRSculpting.BrushRenderer.Render(Matrix4x4.TRS(customBrushCenter.position, Quaternion.identity, Vector3.one) * brushBaseTransform * (Matrix4x4)primitive.transform, renderSdf);
                    }
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (VRSculpting != null)
        {
            foreach (var entry in primitiveButtons)
            {
                using (var renderSdf = VRSculpting.CreateSdf(entry.Value.type))
                {
                    if (renderSdf != null)
                    {
                        Matrix4x4 renderTransform = Matrix4x4.TRS(entry.Key.transform.position + entry.Key.transform.rotation * primitivePreviewOffset, entry.Key.transform.rotation, primitivePreviewScale * entry.Key.transform.localScale);

                        if (primitivePreviewHightlightMaterial != null && entry.Key.Hovered)
                        {
                            VRSculpting.BrushRenderer.Render(renderTransform, renderSdf, BrushOperation.Union, primitivePreviewHightlightMaterial);
                        }
                        else
                        {
                            VRSculpting.BrushRenderer.Render(renderTransform, renderSdf);
                        }
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var obj in primitives.Keys)
        {
            Destroy(obj);
        }
        primitives.Clear();
    }

    private void OnChangeSelectedPrimitive()
    {
        if (SelectedPrimitive != null)
        {
            var primitiveEntry = primitives[SelectedPrimitive].Item2;

            blendSlider.interactable = true;
            blendSlider.value = primitiveEntry.blend;

            scaleXSlider.interactable = true;
            scaleXSlider.value = primitiveEntry.scale.x;

            scaleYSlider.interactable = true;
            scaleYSlider.value = primitiveEntry.scale.y;

            scaleZSlider.interactable = true;
            scaleZSlider.value = primitiveEntry.scale.z;

            unionButton.interactable = true;
            unionButton.GetComponent<ButtonEventEffects>().PermanentHover = primitiveEntry.operation == BrushOperation.Union;

            differenceButton.interactable = true;
            differenceButton.GetComponent<ButtonEventEffects>().PermanentHover = primitiveEntry.operation == BrushOperation.Difference;

            deleteButton.interactable = true;
        }
        else
        {
            blendSlider.interactable = false;
            scaleXSlider.interactable = false;
            scaleYSlider.interactable = false;
            scaleZSlider.interactable = false;
            unionButton.interactable = false;
            differenceButton.interactable = false;
            deleteButton.interactable = false;
        }
    }

    private void OnBlendSliderValueChange(float value)
    {
        if (SelectedPrimitive != null)
        {
            primitives[SelectedPrimitive].Item2.blend = value;
            rebuildSurface = true;
        }
    }

    private void OnScaleXSliderValueChange(float value)
    {
        if (SelectedPrimitive != null)
        {
            primitives[SelectedPrimitive].Item2.scale.x = value;
            rebuildSurface = true;
        }
    }

    private void OnScaleYSliderValueChange(float value)
    {
        if (SelectedPrimitive != null)
        {
            primitives[SelectedPrimitive].Item2.scale.y = value;
            rebuildSurface = true;
        }
    }

    private void OnScaleZSliderValueChange(float value)
    {
        if (SelectedPrimitive != null)
        {
            primitives[SelectedPrimitive].Item2.scale.z = value;
            rebuildSurface = true;
        }
    }

    private void OnAddButtonClicked()
    {
        if (primitiveButtons.Count > 0)
        {
            RemovePrimitivesButtons();
        }
        else
        {
            ShowPrimitivesButtons();
        }
    }

    private void OnDeleteButtonClicked()
    {
        if (SelectedPrimitive != null)
        {
            primitives.Remove(SelectedPrimitive);
            Destroy(SelectedPrimitive);
            SelectedPrimitive = null;
            rebuildSurface = true;
        }
    }

    private void OnUnionButtonClicked()
    {
        if (SelectedPrimitive != null)
        {
            primitives[SelectedPrimitive].Item2.operation = BrushOperation.Union;
            unionButton.GetComponent<ButtonEventEffects>().PermanentHover = true;
            differenceButton.GetComponent<ButtonEventEffects>().PermanentHover = false;
            rebuildSurface = true;
        }
    }

    private void OnDifferenceButtonClicked()
    {
        if (SelectedPrimitive != null)
        {
            primitives[SelectedPrimitive].Item2.operation = BrushOperation.Difference;
            unionButton.GetComponent<ButtonEventEffects>().PermanentHover = false;
            differenceButton.GetComponent<ButtonEventEffects>().PermanentHover = true;
            rebuildSurface = true;
        }
    }

    private void OnShowSurfaceButtonClicked()
    {
        RenderSurface = !RenderSurface;
    }

    private void OnShowPrimitivesButtonClicked()
    {
        RenderPrimitives = !RenderPrimitives;
    }
}
