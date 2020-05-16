using UnityEngine;
using System.Collections;

namespace Voxel
{
    public class DefaultCustomBrushSdfRenderer : CustomBrushSdfRenderer<MortonIndexer, DefaultCustomBrushType, DefaultCustomBrushSdfEvaluator>
    {
        [SerializeField] private DefaultVoxelWorldContainer parentWorld;

        protected override VoxelWorldContainer<MortonIndexer> GetParentWorld()
        {
            return parentWorld;
        }
    }
}