using UnityEngine;
using System.Collections;
using Voxel;

public class DefaultVoxelChunkContainer : VoxelChunkContainer<LinearIndexer>
{
    [SerializeField] private MeshFilter outlineMesh;
    [SerializeField] private MeshRenderer outlineRenderer;

    public void Start()
    {
        outlineRenderer.enabled = false;
    }

    protected override void OnSetChunk()
    {
        if(Chunk.World.VoxelWorldObject.GetComponent<DefaultVoxelWorldContainer>().HoveringHands.Count > 0)
        {
            SetOutlineEnabled(true);
        }
    }

    public override void OnChunkRebuilt()
    {
        if(outlineRenderer.enabled)
        {
            outlineMesh.mesh = Chunk.mesh;
        }
    }

    public void SetOutlineEnabled(bool enabled)
    {
        outlineRenderer.enabled = enabled;

        if (enabled)
        {
            outlineMesh.sharedMesh = Chunk.mesh;
        }
        else
        {
            outlineMesh.sharedMesh = null;
        }
    }
}