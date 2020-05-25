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
        }
    }

    [SerializeField] private bool _renderPrimitives = false;
    public bool RenderPrimitives
    {
        get
        {
            return _renderPrimitives;
        }
        set
        {
            _renderPrimitives = value;
        }
    }

    public class CustomBrushPrimitiveEntry
    {
        public DefaultCustomBrushType type;
        public BrushOperation operation;
        public float blend;
        public Matrix4x4 transform;

        public CustomBrushPrimitiveEntry(DefaultCustomBrushType type, BrushOperation operation, float blend, Matrix4x4 transform)
        {
            this.type = type;
            this.operation = operation;
            this.blend = blend;
            this.transform = transform;
        }
    }

    private Dictionary<GameObject, CustomBrushPrimitiveEntry> primitives = new Dictionary<GameObject, CustomBrushPrimitiveEntry>();
    private GameObject selectedPrimitive = null;

    public void OnInitializeUI(VRUI.Context ctx)
    {
        VRSculpting = ctx.controller;



    }

    private void Start()
    {
        //TODO Add primitives in OnInitializeUI

        var instance = Instantiate(customBrushPrimitivePrefab);
        instance.transform.position = customBrushCenter.position;
        primitives.Add(instance, new CustomBrushPrimitiveEntry(DefaultCustomBrushType.SPHERE, BrushOperation.Union, 2.0f, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one)));

        instance = Instantiate(customBrushPrimitivePrefab);
        instance.transform.position = customBrushCenter.position;
        primitives.Add(instance, new CustomBrushPrimitiveEntry(DefaultCustomBrushType.BOX, BrushOperation.Union, 2.0f, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one)));
    }

    private void Update()
    {
        if (VRSculpting != null)
        {
            var hovered = VRSculpting.InputModule.EventData.pointerCurrentRaycast.gameObject;

            if (selectAction.stateDown)
            {
                if(hovered == null)
                {
                    selectedPrimitive = null;
                }
                else if(primitives.ContainsKey(hovered))
                {
                    selectedPrimitive = hovered;
                }
            }

            var brushBaseTransform = Matrix4x4.TRS(Vector3.zero, customBrushCenter.rotation, VRSculpting.VoxelWorld.transform.localScale);
            var brushBaseTransformInverse = brushBaseTransform.inverse;

            var brush = VRSculpting.CustomBrush.Instance;
            brush.Primitives.Clear();

            foreach (var entry in primitives)
            {
                var obj = entry.Key;
                var primitive = entry.Value;

                //TODO Cache component
                var isSelected = hovered == obj || selectedPrimitive == obj;
                obj.GetComponent<CustomBrushPrimitiveHoverInteractions>().SetOutlineEnabled(isSelected);

                var offset = brushBaseTransformInverse.MultiplyPoint(obj.transform.position - customBrushCenter.position);
                if(offset.magnitude > maxPrimitiveDistance)
                {
                    offset = offset.normalized * maxPrimitiveDistance;
                }
                primitive.transform = Matrix4x4.TRS(offset, brushBaseTransformInverse.rotation * obj.transform.rotation, Vector3.one);
                brush.AddPrimitive(primitive.type, primitive.operation, primitive.blend, primitive.transform);
            }

            //TODO Trigger on change or button
            //TODO Rebuilding also seems to cause a memory leak
            VRSculpting.CustomBrush.CustomBrushRenderer.NeedsRebuild = true;

            if(RenderSurface)
            {
                using (var sdf = brush.CreateSdf(Allocator.Persistent))
                {
                    VRSculpting.BrushRenderer.Render(Matrix4x4.TRS(customBrushCenter.position, Quaternion.identity, Vector3.one) * brushBaseTransform, sdf);
                }
            }

            if (RenderPrimitives)
            {
                foreach (var primitive in brush.Primitives)
                {
                    var renderSdf = brush.Evaluator.GetRenderSdf(primitive);
                    VRSculpting.BrushRenderer.Render(Matrix4x4.TRS(customBrushCenter.position, Quaternion.identity, Vector3.one)* brushBaseTransform * (Matrix4x4)primitive.transform, renderSdf);
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach(var obj in primitives.Keys)
        {
            Destroy(obj);
        }
        primitives.Clear();
    }
}
