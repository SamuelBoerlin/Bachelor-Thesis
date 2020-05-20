using UnityEngine;
using System.Collections;
using Voxel;

public class DefaultCustomBrushSdfRenderer : CustomBrushSdfRenderer<LinearIndexer, DefaultCustomBrushType, DefaultCustomBrushSdfEvaluator>
{
    [SerializeField] private DefaultVoxelWorldContainer parentWorld;

    protected override VoxelWorldContainer<LinearIndexer> GetParentWorld()
    {
        return parentWorld;
    }
}
