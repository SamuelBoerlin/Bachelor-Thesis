using UnityEngine;
using System.Collections;
using Voxel;

public class DefaultCustomBrushSdfRenderer : CustomBrushSdfRenderer<MortonIndexer, DefaultCustomBrushType, DefaultCustomBrushSdfEvaluator>
{
    [SerializeField] private DefaultVoxelWorldContainer parentWorld;

    protected override VoxelWorldContainer<MortonIndexer> GetParentWorld()
    {
        return parentWorld;
    }
}
