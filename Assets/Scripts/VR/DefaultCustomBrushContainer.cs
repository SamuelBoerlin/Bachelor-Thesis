using UnityEngine;
using Voxel;

[RequireComponent(typeof(DefaultCustomBrushSdfRenderer))]
public class DefaultCustomBrushContainer : CustomBrushContainer<LinearIndexer, DefaultCustomBrushType, DefaultCustomBrushSdfEvaluator>
{
    protected override DefaultCustomBrushSdfEvaluator CreateBrushSdfEvaluator()
    {
        return new DefaultCustomBrushSdfEvaluator(BrushProperties.DEFAULT);
    }

    private void Start()
    {
        Matrix4x4 globalTransform = Matrix4x4.Translate(new Vector3(0.2f, 0.2f, 0.2f));

        Instance.Primitives.Clear();

        float blend = 5.5f;

        Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, BrushOperation.Union, blend, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 5.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Union, blend, globalTransform * Matrix4x4.TRS(new Vector3(0.5f, -2.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 1.5f, 0.5f)));

        /*Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, BrushOperation.Union, 5f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(8.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 8.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 8.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(-7.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, -7.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, -7.5f)));*/

        /*Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Union, 5f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, BrushOperation.Union, (Mathf.Sin(Time.time) + 1) * 6, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 7.5f, 0.5f)));*/

        if (_customBrushRenderer != null)
        {
            _customBrushRenderer.NeedsRebuild = true;
        }
    }
}
