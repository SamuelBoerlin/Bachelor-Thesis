using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Voxel
{
    public class ChunkJobUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CompareMaterialsAndAdjustCounter(NativeArray<int> voxelCount, int currentMaterial, int newMaterial)
        {
            if (currentMaterial == 0 && newMaterial != 0)
            {
                voxelCount[0]++;
            }
            else if (currentMaterial != 0 && newMaterial == 0)
            {
                voxelCount[0]--;
            }
        }
    }
}