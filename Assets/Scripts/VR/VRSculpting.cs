using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Valve.VR;
using Voxel;
using System;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;
using Unity.Collections;
using Unity.Mathematics;
using Voxel.Voxelizer;

[RequireComponent(typeof(SdfShapeRenderHandler))]
public class VRSculpting : MonoBehaviour, IBrushMaterialsProvider
{
    public class SdfConsumer
    {
        public static readonly SdfConsumer PASSTHROUGH = new SdfConsumer();

        public virtual ISdf Consume<TSdf>(TSdf sdf) where TSdf : struct, ISdf
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
        private float scale;

        public PlacementSdfConsumer(DefaultVoxelWorldContainer voxelWorld, DefaultVoxelEditManagerContainer voxelEditsManager, Vector3 position, Quaternion rotation, float scale, int material, bool replace)
        {
            this.voxelWorld = voxelWorld;
            this.voxelEditsManager = voxelEditsManager;
            this.position = position;
            this.rotation = rotation;
            this.material = material;
            this.replace = replace;
            this.scale = scale;
        }

        public override ISdf Consume<TSdf>(TSdf sdf)
        {
            voxelWorld.Instance.ApplySdf(position, rotation, new ScaleSDF<TSdf>(scale, sdf), material, replace, voxelEditsManager.Instance.Consumer());
            return sdf;
        }
    }

    [SerializeField] private SteamVR_Action_Boolean placeAction;

    [SerializeField] private SteamVR_Action_Boolean lineGuideAction;
    [SerializeField] private GameObject lineGuidePrefab;

    [SerializeField] private Camera eventCamera;
    public Camera EventCamera
    {
        get
        {
            return eventCamera;
        }
    }

    [SerializeField] private bool _fixateBrushRotation;
    public bool FixateBrushRotation
    {
        get
        {
            return _fixateBrushRotation;
        }
        set
        {
            _fixateBrushRotation = value;
        }
    }

    [SerializeField] private SteamVR_Action_Vector2 brushRotateAction;
    [SerializeField] private float brushRotateSpeed = 45.0f;
    [SerializeField] private float brushRotateDeadzone = 0.1f;
    [SerializeField] private SteamVR_Action_Boolean brushResetAction;

    [SerializeField] private SteamVR_Action_Vector2 brushScaleAction;
    [SerializeField] private float brushScaleSpeed = 0.25f;
    [SerializeField] private float brushScaleDeadzone = 0.1f;
    [SerializeField] private float brushMaxScale = 1.0f;
    [SerializeField] private float brushMinScale = 0.1f;

    [SerializeField] private GameObject controllerBrush;

    [SerializeField] private SteamVR_Action_Boolean voxelizeAction;
    [SerializeField] private Hand voxelizeHand;
    [SerializeField] private int voxelizeResolution = 128;
    [SerializeField] private bool voxelizeSmoothNormals = false;

    [SerializeField] private Camera _userCamera;
    public Camera UserCamera
    {
        get
        {
            return _userCamera;
        }
        set
        {
            _userCamera = value;
        }
    }

    [SerializeField] private QueryResultDisplay _queryResultDisplay;
    public QueryResultDisplay QueryResultDisplay
    {
        get
        {
            return _queryResultDisplay;
        }
        set
        {
            _queryResultDisplay = value;
        }
    }

    [SerializeField] private UnityCineastApi _cineastApi;
    public UnityCineastApi CineastApi
    {
        get
        {
            return _cineastApi;
        }
        set
        {
            _cineastApi = value;
        }
    }

    [SerializeField] private DefaultCustomBrushContainer _customBrush;
    public DefaultCustomBrushContainer CustomBrush
    {
        get
        {
            return _customBrush;
        }
        set
        {
            _customBrush = value;
        }
    }

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
                OnBrushShapeChange();
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
            var newColor = new Color32(
                (byte)Mathf.Max(1, value.r * 255),
                (byte)Mathf.Max(1, value.g * 255),
                (byte)Mathf.Max(1, value.b * 255),
                (byte)(value.a * 255)
                );
            _brushColor = newColor;
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
        [SerializeField] public string _name;
        public string Name
        {
            get
            {
                return _name;
            }
        }

        [SerializeField] public int _id;
        public int ID
        {
            get
            {
                return _id;
            }
        }

        [SerializeField] public Texture2D _texture;
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

    private Vector3 lineGuideStart = Vector3.zero;
    private Vector3 lineGuideDirection = Vector3.zero;
    private GameObject lineGuideObject = null;

    private Vector2 brushRotateStart = Vector2.zero;
    public Quaternion BrushRotation {
        get; set;
    } = Quaternion.identity;
    private Quaternion brushControllerRotation = Quaternion.identity;

    private Vector2 brushScaleStart = Vector2.zero;
    public float BrushScale
    {
        get; set;
    } = 1.0f;

    private ISdf previewSdf;

    private SdfShapeRenderHandler _brushRenderer;
    public SdfShapeRenderHandler BrushRenderer
    {
        get
        {
            return _brushRenderer;
        }
    }

    [SerializeField] private MenuEntry[] _menuEntries;

    private List<Menu> openMenus = new List<Menu>();

    public bool IsPointerActive
    {
        get
        {
            return openMenus.Count > 0 || (InputModule != null && InputModule.EventData != null && InputModule.EventData.pointerCurrentRaycast.gameObject != null && InputModule.EventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Canvas>() != null);
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

        if (previewSdf != null)
        {
            previewSdf.Dispose();
            previewSdf = null;
        }
    }

    private void OnVRPointerInputModuleInitialized(object sender, VRPointerInputModule.Args args)
    {
        InputModule = args.Module;
    }

    private void Start()
    {
        _brushRenderer = GetComponent<SdfShapeRenderHandler>();
    }

    public Menu InstantiateUI(MenuEntry entry)
    {
        var instance = Instantiate(entry.Prefab);

        instance.transform.position = entry.Spawner.transform.position;
        instance.transform.rotation = entry.Spawner.transform.rotation;
        instance.transform.rotation = Quaternion.LookRotation(new Vector3(instance.transform.forward.x, 0, instance.transform.forward.z), new Vector3(0, 1, 0));

        var script = instance.GetComponent<VRUI>();
        if (script != null)
        {
            script.InitializeUI(this, InputModule, eventCamera);
        }
        else
        {
            throw new InvalidOperationException("VR UI prefab instance does not have VRUI MonoBehaviour");
        }

        return new Menu(entry, instance);
    }

    public TComponent GetUIWithComponent<TComponent>()
    {
        foreach (var menu in openMenus)
        {
            if (menu.instance.TryGetComponent<TComponent>(out var component))
            {
                return component;
            }
        }
        return default;
    }

    public void CloseMenu(GameObject go)
    {
        foreach (var menu in openMenus)
        {
            if (menu.instance == go)
            {
                menu.shouldClose = true;
            }
        }
    }

    public void Update()
    {
        List<Menu> closedMenus = null;
        foreach (var menu in openMenus)
        {
            if (menu.shouldClose || (menu.entry.Toggle && menu.entry.Action.GetStateDown(menu.entry.InputSource)) || (!menu.entry.Toggle && !menu.entry.Action.GetState(menu.entry.InputSource)))
            {
                if (closedMenus == null)
                {
                    closedMenus = new List<Menu>();
                }
                closedMenus.Add(menu);
                Destroy(menu.instance);
            }
        }
        if (closedMenus != null)
        {
            foreach (var closedMenu in closedMenus)
            {
                openMenus.Remove(closedMenu);
            }
        }

        foreach (var entry in _menuEntries)
        {
            if (entry.Action.GetStateDown(entry.InputSource))
            {
                bool canOpen = true;

                foreach (var menu in openMenus)
                {
                    if (menu.entry.InputSource == entry.InputSource || menu.entry.Equals(entry))
                    {
                        canOpen = false;
                        break;
                    }
                }

                if (closedMenus != null)
                {
                    foreach (var menu in closedMenus)
                    {
                        if (menu.entry.Equals(entry))
                        {
                            canOpen = false;
                            break;
                        }
                    }
                }

                if (canOpen)
                {
                    openMenus.Add(InstantiateUI(entry));
                }
            }
        }

        if (brushResetAction != null && brushResetAction.active && brushResetAction.stateDown)
        {
            BrushRotation = Quaternion.identity;
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
                BrushRotation = (Quaternion.Euler(rotationVector.y, -rotationVector.x, 0) * BrushRotation).normalized;
            }
        }
        else
        {
            brushRotateStart = Vector2.zero;
        }

        if (brushScaleAction != null && brushScaleAction.active && (brushScaleAction.axis - brushScaleAction.delta).magnitude > 0.01f && brushScaleAction.axis.magnitude > 0.01f)
        {
            if (brushScaleStart == Vector2.zero)
            {
                brushScaleStart = brushScaleAction.axis;
            }

            if (Mathf.Abs((brushScaleStart - brushScaleAction.axis).y) >= brushScaleDeadzone)
            {
                var scaleChange = brushScaleAction.delta * brushScaleSpeed;
                BrushScale = Mathf.Clamp(BrushScale + scaleChange.y, brushMinScale, brushMaxScale);
            }
        }
        else
        {
            brushScaleStart = Vector2.zero;
        }

        bool isPlacing = placeAction != null && placeAction.active && placeAction.state && !IsPointerActive;
        bool isLineGuiding = lineGuideAction != null && lineGuideAction.active && lineGuideAction.state && !IsPointerActive;

        if (isLineGuiding)
        {
            if (!isPlacing)
            {
                if (lineGuideStart == Vector3.zero)
                {
                    lineGuideStart = controllerBrush.transform.position;
                }
                else
                {
                    lineGuideDirection = (controllerBrush.transform.position - lineGuideStart).normalized;
                }
            }

            if (lineGuideObject == null)
            {
                lineGuideObject = Instantiate(lineGuidePrefab);
            }
        }
        else
        {
            lineGuideStart = Vector3.zero;
            lineGuideDirection = Vector3.zero;

            if (lineGuideObject != null)
            {
                Destroy(lineGuideObject);
                lineGuideObject = null;
            }
        }


        Vector3 brushPosition;
        if (isLineGuiding)
        {
            var diff = controllerBrush.transform.position - lineGuideStart;
            var projection = Vector3.Dot(diff, lineGuideDirection);
            brushPosition = lineGuideStart + lineGuideDirection * projection;
        }
        else
        {
            brushPosition = controllerBrush.transform.position;
        }

        if (lineGuideObject != null)
        {
            var lineRenderer = lineGuideObject.GetComponent<LineRenderer>();
            lineRenderer.SetPosition(0, lineGuideStart);
            lineRenderer.SetPosition(1, brushPosition);
        }

        if (!FixateBrushRotation && !IsPointerActive)
        {
            brushControllerRotation = controllerBrush.transform.rotation;
        }

        if (isPlacing)
        {
            //Merge edits while holding down
            VoxelEditsManager.Instance.Merge = true;

            //Apply SDF
            var sdf = CreateSdf(BrushType, new PlacementSdfConsumer(VoxelWorld, VoxelEditsManager, brushPosition, brushControllerRotation * BrushRotation, BrushScale,
                BrushOperation == BrushOperation.Difference ? 0 : MaterialColors.ToInteger((int)Mathf.Round(BrushColor.r * 255), (int)Mathf.Round(BrushColor.g * 255), (int)Mathf.Round(BrushColor.b * 255), BrushMaterial.ID),
                BrushOperation == BrushOperation.Replace));
            sdf?.Dispose();
        }
        else
        {
            VoxelEditsManager.Instance.Merge = false;
        }

        if (previewSdf != null && !IsPointerActive)
        {
            _brushRenderer.Render(Matrix4x4.TRS(brushPosition, brushControllerRotation * BrushRotation, VoxelWorld.transform.localScale * BrushScale), previewSdf, BrushOperation);
        }

        if (voxelizeAction != null && voxelizeAction.active && voxelizeAction.stateDown)
        {
            MeshFilter meshFilter = null;
            QueryResultObject resultObject = null;

            foreach (var attached in voxelizeHand.AttachedObjects)
            {
                resultObject = null;
                meshFilter = attached.attachedObject.GetComponent<MeshFilter>();

                if (meshFilter != null && meshFilter.sharedMesh != null & meshFilter.sharedMesh.isReadable)
                {
                    resultObject = attached.attachedObject.GetComponent<QueryResultObject>();

                    if (resultObject != null)
                    {
                        break;
                    }
                }
            }

            if (meshFilter != null && resultObject != null)
            {
                VoxelizeMesh(meshFilter.sharedMesh);
            }
        }
    }

    private void VoxelizeMesh(Mesh voxelizeMesh)
    {
        VoxelWorld.Instance.Clear();

        //Move voxel world to brush
        VoxelWorld.transform.position = controllerBrush.transform.position;
        VoxelWorld.transform.rotation = controllerBrush.transform.rotation;

        var triangles = voxelizeMesh.triangles;
        var vertices = voxelizeMesh.vertices;
        var normals = voxelizeMesh.normals;

        var inVertices = new NativeArray<float3>(triangles.Length, Allocator.TempJob);
        var inNormals = new NativeArray<float3>(triangles.Length, Allocator.TempJob);

        for (int l = triangles.Length, i = 0; i < l; i += 3)
        {
            inVertices[i] = vertices[triangles[i]];
            inVertices[i + 1] = vertices[triangles[i + 1]];
            inVertices[i + 2] = vertices[triangles[i + 2]];

            inNormals[i] = normals[triangles[i]];
            inNormals[i + 1] = normals[triangles[i + 1]];
            inNormals[i + 2] = normals[triangles[i + 2]];
        }

        var outVoxels = new NativeArray3D<Voxel.Voxel, LinearIndexer>(new LinearIndexer(voxelizeResolution, voxelizeResolution, voxelizeResolution), voxelizeResolution, voxelizeResolution, voxelizeResolution, Allocator.TempJob);

        var voxelizationProperties = voxelizeSmoothNormals ? Voxelizer.VoxelizationProperties.SMOOTH : Voxelizer.VoxelizationProperties.FLAT;

        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        using (var job = Voxelizer.Voxelize(inVertices, inNormals, outVoxels, MaterialColors.ToInteger((int)Mathf.Round(BrushColor.r * 255), (int)Mathf.Round(BrushColor.g * 255), (int)Mathf.Round(BrushColor.b * 255), BrushMaterial.ID), voxelizationProperties))
        {
            job.Handle.Complete();
        }

        watch.Stop();
        Debug.Log("Voxelized mesh: " + watch.ElapsedMilliseconds + "ms");
        watch.Reset();
        watch.Start();

        //TODO Make voxelizer also undoable?
        VoxelWorld.Instance.ApplyGrid(0, 0, 0, outVoxels, true, false, null);

        watch.Stop();
        Debug.Log("Applied to grid: " + watch.ElapsedMilliseconds + "ms");

        inVertices.Dispose();
        inNormals.Dispose();
        outVoxels.Dispose();
    }

    private void OnBrushShapeChange()
    {
        if (previewSdf != null)
        {
            //Dispose the previous SDF
            previewSdf.Dispose();
        }
        previewSdf = CreateSdf(BrushType);
    }

    /// <summary>
    /// Creates an SDF for the given brush type and runs it through the specified consumer, if not null.
    /// The SDF is a disposable object and must be disposed of by the caller!
    /// </summary>
    /// <param name="type"></param>
    /// <param name="consumer"></param>
    /// <returns></returns>
    public ISdf CreateSdf(BrushType type, SdfConsumer consumer = null)
    {
        if (consumer == null)
        {
            consumer = SdfConsumer.PASSTHROUGH;
        }

        switch (type)
        {
            default:
                return null;
            case BrushType.Box:
                return consumer.Consume(new BoxSDF(BrushProperties.DEFAULT.boxSize));
            case BrushType.Sphere:
                return consumer.Consume(new SphereSDF(BrushProperties.DEFAULT.sphereRadius));
            case BrushType.Cylinder:
                return consumer.Consume(new CylinderSDF(BrushProperties.DEFAULT.cylinderHeight, BrushProperties.DEFAULT.cylinderRadius));
            case BrushType.Pyramid:
                return consumer.Consume(new PyramidSDF(BrushProperties.DEFAULT.pyramidHeight, BrushProperties.DEFAULT.pyramidBase));
            case BrushType.Custom:
                return consumer.Consume(CustomBrush.Instance.CreateSdf(Unity.Collections.Allocator.Persistent));
        }
    }

    public BrushMaterialType? FindBrushMaterialTypeForId(int id)
    {
        foreach (var type in BrushMaterials)
        {
            if (type.ID == id)
            {
                return type;
            }
        }
        return null;
    }
}
