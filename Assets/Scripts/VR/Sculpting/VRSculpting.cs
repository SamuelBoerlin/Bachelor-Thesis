using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Valve.VR;
using Voxel;
using System;

[RequireComponent(typeof(SdfShapeRenderHandler))]
public class VRSculpting : MonoBehaviour
{
    public class SdfConsumer
    {
        public static readonly SdfConsumer NONE = new SdfConsumer();

        public virtual TSdf Consume<TSdf>(TSdf sdf) where TSdf : struct, ISdf
        {
            return sdf;
        }
    }

    public class PlacementSdfConsumer : SdfConsumer
    {
        private DefaultVoxelWorldContainer voxelWorld;
        private Vector3 position;
        private Quaternion rotation;
        private int material;
        private bool replace;

        public PlacementSdfConsumer(DefaultVoxelWorldContainer voxelWorld, Vector3 position, Quaternion rotation, int material, bool replace)
        {
            this.voxelWorld = voxelWorld;
            this.position = position;
            this.rotation = rotation;
            this.material = material;
            this.replace = replace;
        }

        public override TSdf Consume<TSdf>(TSdf sdf)
        {
            voxelWorld.Instance.ApplySdf(position, rotation, sdf, material, replace, null); //TODO Edit manager
            return sdf;
        }
    }

    [SerializeField] private SteamVR_Action_Boolean placeAction;

    [SerializeField] private Camera eventCamera;

    [SerializeField] private SteamVR_Action_Boolean brushSelectionAction;
    [SerializeField] private GameObject controllerBrushSelectionMenu;
    [SerializeField] private GameObject brushSelectionMenuPrefab;

    [SerializeField] private SteamVR_Action_Vector2 brushRotateAction;
    [SerializeField] private float brushRotateSpeed = 45.0f;
    [SerializeField] private float brushRotateDeadzone = 0.1f;
    [SerializeField] private SteamVR_Action_Boolean brushResetAction;

    [SerializeField] private GameObject controller;
    [SerializeField] private GameObject controllerBrush;

    [SerializeField] private DefaultVoxelWorldContainer voxelWorld;

    private BrushType _brushType = BrushType.None;
    public BrushType BrushType
    {
        get
        {
            return _brushType;
        }
        set
        {
            bool change = _brushType != value;
            _brushType = value;
            if (change)
            {
                OnBrushChange();
            }
        }
    }

    private Vector2 brushRotateStart = Vector2.zero;
    private Quaternion brushRotation = Quaternion.identity;

    private ISdf previewSdf;

    private SdfShapeRenderHandler _brushRenderer;
    public SdfShapeRenderHandler BrushRenderer
    {
        get
        {
            return _brushRenderer;
        }
    }

    private GameObject brushSelectionMenuObject;

    public bool IsPointerActive
    {
        get
        {
            return brushSelectionMenuObject != null;
        }
    }

    private void Awake()
    {
        BrushType = BrushType.Box;
    }

    private void Start()
    {
        _brushRenderer = GetComponent<SdfShapeRenderHandler>();
    }

    public void Update()
    {
        if (brushSelectionAction != null && brushSelectionAction.state)
        {
            if (brushSelectionMenuObject == null)
            {
                brushSelectionMenuObject = Instantiate(brushSelectionMenuPrefab);
                brushSelectionMenuObject.transform.position = controllerBrushSelectionMenu.transform.position;
                brushSelectionMenuObject.transform.rotation = controllerBrushSelectionMenu.transform.rotation;
                brushSelectionMenuObject.transform.rotation = Quaternion.LookRotation(new Vector3(brushSelectionMenuObject.transform.forward.x, 0, brushSelectionMenuObject.transform.forward.z), new Vector3(0, 1, 0));
                brushSelectionMenuObject.GetComponentInChildren<Canvas>().worldCamera = eventCamera;
                brushSelectionMenuObject.GetComponentInChildren<BrushSelectionMenu>().VRSculpting = this;
            }
        }
        else if (brushSelectionMenuObject != null)
        {
            Destroy(brushSelectionMenuObject);
            brushSelectionMenuObject = null;
        }

        if (brushResetAction != null && brushResetAction.active && brushResetAction.stateDown)
        {
            brushRotation = Quaternion.identity;
        }
        else if (brushRotateAction != null && brushRotateAction.active && (brushRotateAction.axis - brushRotateAction.delta).magnitude > 0.01f && brushRotateAction.axis.magnitude > 0.01f)
        {
            if (brushRotateStart == Vector2.zero)
            {
                brushRotateStart = brushRotateAction.axis;
            }

            if ((brushRotateStart - brushRotateAction.axis).magnitude >= brushRotateDeadzone)
            {
                var rotationVector = brushRotateAction.delta * brushRotateSpeed;
                brushRotation = (Quaternion.Euler(rotationVector.y, -rotationVector.x, 0) * brushRotation).normalized;
            }
        }
        else
        {
            brushRotateStart = Vector2.zero;
        }

        if (placeAction != null && placeAction.active && placeAction.state && !IsPointerActive)
        {
            CreateSdf(BrushType, new PlacementSdfConsumer(voxelWorld, controllerBrush.transform.position, controllerBrush.transform.rotation * brushRotation, 1, false));
        }

        if (previewSdf != null && !IsPointerActive)
        {
            _brushRenderer.Render(Matrix4x4.TRS(controllerBrush.transform.position, controllerBrush.transform.rotation * brushRotation, voxelWorld.transform.localScale), previewSdf);
        }
    }

    private void OnBrushChange()
    {
        previewSdf = CreateSdf(BrushType);
    }

    //TODO Disposing
    public ISdf CreateSdf(BrushType type, SdfConsumer consumer = null)
    {
        if (consumer == null)
        {
            consumer = SdfConsumer.NONE;
        }

        float baseSize = 10.0f;

        switch (type)
        {
            default:
                return null;
            case BrushType.Box:
                return consumer.Consume(new BoxSDF(baseSize));
            case BrushType.Sphere:
                return consumer.Consume(new SphereSDF(baseSize));
            case BrushType.Cylinder:
                return consumer.Consume(new CylinderSDF(baseSize, baseSize));
            case BrushType.Pyramid:
                return consumer.Consume(new PyramidSDF(baseSize * 2, baseSize * 2));
        }
    }
}
