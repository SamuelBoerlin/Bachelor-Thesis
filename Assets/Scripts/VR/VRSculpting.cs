using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Valve.VR;
using Voxel;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(SdfShapeRenderHandler))]
public class VRSculpting : MonoBehaviour, IBrushMaterialsProvider
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
        private DefaultVoxelEditManagerContainer voxelEditsManager;
        private Vector3 position;
        private Quaternion rotation;
        private int material;
        private bool replace;

        public PlacementSdfConsumer(DefaultVoxelWorldContainer voxelWorld, DefaultVoxelEditManagerContainer voxelEditsManager, Vector3 position, Quaternion rotation, int material, bool replace)
        {
            this.voxelWorld = voxelWorld;
            this.voxelEditsManager = voxelEditsManager;
            this.position = position;
            this.rotation = rotation;
            this.material = material;
            this.replace = replace;
        }

        public override TSdf Consume<TSdf>(TSdf sdf)
        {
            voxelWorld.Instance.ApplySdf(position, rotation, sdf, material, replace, voxelEditsManager.Instance.Consumer());
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

    [SerializeField] private SteamVR_Action_Boolean queryMenuAction;
    [SerializeField] private GameObject controllerQueryMenu;
    [SerializeField] private GameObject queryMenuPrefab;

    [SerializeField] private GameObject controller;
    [SerializeField] private GameObject controllerBrush;

    [SerializeField] private DefaultVoxelWorldContainer _voxelWorld;
    public DefaultVoxelWorldContainer VoxelWorld
    {
        get
        {
            return _voxelWorld;
        }
        set
        {
            _voxelWorld = value;
        }
    }

    [SerializeField] private DefaultVoxelEditManagerContainer _voxelEditsManager;
    public DefaultVoxelEditManagerContainer VoxelEditsManager
    {
        get
        {
            return _voxelEditsManager;
        }
        set
        {
            _voxelEditsManager = value;
        }
    }

    [SerializeField] private BrushType _brushType = BrushType.None;
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
                OnBrushTypeChange();
            }
        }
    }

    [SerializeField] private Color _brushColor = Color.white;
    public Color BrushColor
    {
        get
        {
            return _brushColor;
        }
        set
        {
            _brushColor = value;
        }
    }

    [SerializeField] private BrushOperation _brushOperation = BrushOperation.Union;
    public BrushOperation BrushOperation
    {
        get
        {
            return _brushOperation;
        }
        set
        {
            _brushOperation = value;
        }
    }

    [Serializable]
    public struct BrushMaterialType
    {
        [SerializeField] internal string _name;
        public string Name
        {
            get
            {
                return _name;
            }
        }

        [SerializeField] internal int _id;
        public int ID
        {
            get
            {
                return _id;
            }
        }

        [SerializeField] internal Texture2D _texture;
        public Texture2D Texture
        {
            get
            {
                return _texture;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BrushMaterialType))
            {
                return false;
            }

            var type = (BrushMaterialType)obj;
            return _name == type._name &&
                   _id == type._id &&
                   EqualityComparer<Texture2D>.Default.Equals(_texture, type._texture);
        }

        public override int GetHashCode()
        {
            var hashCode = 554432026;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_name);
            hashCode = hashCode * -1521134295 + _id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Texture2D>.Default.GetHashCode(_texture);
            return hashCode;
        }
    }

    [SerializeField] private BrushMaterialType[] _brushMaterials;
    public BrushMaterialType[] BrushMaterials
    {
        get
        {
            return _brushMaterials;
        }
    }

    [SerializeField, BrushMaterial] private BrushMaterialType _brushMaterial;
    public BrushMaterialType BrushMaterial
    {
        get
        {
            return _brushMaterial;
        }
        set
        {
            _brushMaterial = value;
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
    private GameObject queryMenuObject;

    public bool IsPointerActive
    {
        get
        {
            return brushSelectionMenuObject != null ||
                (InputModule != null && InputModule.EventData != null && InputModule.EventData.pointerCurrentRaycast.gameObject != null && InputModule.EventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Canvas>() != null);
        }
    }

    public VRPointerInputModule InputModule
    {
        private set;
        get;
    }

    private void Awake()
    {
        BrushType = BrushType.Box;

        VRPointerInputModule.OnVRPointerInputModuleInitialized += OnVRPointerInputModuleInitialized;
    }

    private void OnDestroy()
    {
        VRPointerInputModule.OnVRPointerInputModuleInitialized -= OnVRPointerInputModuleInitialized;
    }

    private void OnVRPointerInputModuleInitialized(object sender, VRPointerInputModule.Args args)
    {
        InputModule = args.Module;
    }

    private void Start()
    {
        _brushRenderer = GetComponent<SdfShapeRenderHandler>();
    }

    public GameObject InstantiateUI(GameObject prefab)
    {
        var ui = Instantiate(prefab);
        var script = ui.GetComponent<VRUI>();
        if (script != null)
        {
            script.InitializeUI(this, InputModule, eventCamera);
        }
        else
        {
            throw new InvalidOperationException("VR UI prefab instance does not have VRUI MonoBehaviour");
        }
        return ui;
    }

    public void Update()
    {
        if (queryMenuAction != null && queryMenuAction.stateDown)
        {
            if (queryMenuObject == null)
            {
                queryMenuObject = InstantiateUI(queryMenuPrefab);
                queryMenuObject.transform.position = controllerQueryMenu.transform.position;
                queryMenuObject.transform.rotation = controllerQueryMenu.transform.rotation;
                queryMenuObject.transform.rotation = Quaternion.LookRotation(new Vector3(queryMenuObject.transform.forward.x, 0, queryMenuObject.transform.forward.z), new Vector3(0, 1, 0));
            }
            else
            {
                Destroy(queryMenuObject);
                queryMenuObject = null;
            }
        }

        if (brushSelectionAction != null && brushSelectionAction.state)
        {
            if (brushSelectionMenuObject == null)
            {
                brushSelectionMenuObject = InstantiateUI(brushSelectionMenuPrefab);
                brushSelectionMenuObject.transform.position = controllerBrushSelectionMenu.transform.position;
                brushSelectionMenuObject.transform.rotation = controllerBrushSelectionMenu.transform.rotation;
                brushSelectionMenuObject.transform.rotation = Quaternion.LookRotation(new Vector3(brushSelectionMenuObject.transform.forward.x, 0, brushSelectionMenuObject.transform.forward.z), new Vector3(0, 1, 0));
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
            //Merge edits while holding down
            VoxelEditsManager.Instance.Merge = true;

            //Apply SDF
            CreateSdf(BrushType, new PlacementSdfConsumer(VoxelWorld, VoxelEditsManager, controllerBrush.transform.position, controllerBrush.transform.rotation * brushRotation,
                BrushOperation == BrushOperation.Difference ? 0 : MaterialColors.ToInteger((int)Mathf.Round(BrushColor.r * 255), (int)Mathf.Round(BrushColor.g * 255), (int)Mathf.Round(BrushColor.b * 255), BrushMaterial.ID),
                BrushOperation == BrushOperation.Replace));
        }
        else
        {
            VoxelEditsManager.Instance.Merge = false;
        }

        if (previewSdf != null && !IsPointerActive)
        {
            _brushRenderer.Render(Matrix4x4.TRS(controllerBrush.transform.position, controllerBrush.transform.rotation * brushRotation, VoxelWorld.transform.localScale), previewSdf);
        }
    }

    private void OnBrushTypeChange()
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

    public BrushMaterialType? FindBrushMaterialTypeForId(int id)
    {
        foreach(var type in BrushMaterials)
        {
            if(type.ID == id)
            {
                return type;
            }
        }
        return null;
    }
}
