using UnityEngine;
using Voxel;

[RequireComponent(typeof(DefaultCustomBrushSdfRenderer))]
public class DefaultCustomBrushContainer : CustomBrushContainer<LinearIndexer, DefaultCustomBrushType, DefaultCustomBrushSdfEvaluator>
{
    protected override DefaultCustomBrushSdfEvaluator CreateBrushSdfEvaluator()
    {
        return new DefaultCustomBrushSdfEvaluator();
    }

    protected override void Start()
    {
        base.Start();

        Matrix4x4 globalTransform = Matrix4x4.Translate(new Vector3(0.2f, 0.2f, 0.2f));

        Instance.Primitives.Clear();

        float blend = (Mathf.Sin(Time.time) + 1) * 2.5f;

        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Union, blend, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Union, blend, globalTransform * Matrix4x4.Translate(new Vector3(8.5f, 4.5f, 2.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, BrushOperation.Difference, blend, globalTransform * Matrix4x4.TRS(new Vector3(8.5f, 4.5f, -3.5f), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f)));

        /*Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, BrushOperation.Union, 5f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(8.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 8.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 8.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(-7.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, -7.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, -7.5f)));*/

        /*Instance.AddPrimitive(DefaultCustomBrushType.BOX, BrushOperation.Union, 5f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)));
        Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, BrushOperation.Union, (Mathf.Sin(Time.time) + 1) * 6, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 7.5f, 0.5f)));*/

        if (customBrushRenderer != null)
        {
            customBrushRenderer.NeedsRebuild = true;
        }
    }
}
