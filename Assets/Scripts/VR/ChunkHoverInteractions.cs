using UnityEngine;
using System.Collections;
using Valve.VR.InteractionSystem;
using Voxel;
using System.Collections.Generic;
using Valve.VR;
using UnityEngine.EventSystems;

[RequireComponent(typeof(DefaultVoxelChunkContainer))]
[RequireComponent(typeof(Interactable))]
public class ChunkHoverInteractions : VRPointerRaycastHandler
{
    [SerializeField] private SteamVR_Input_Sources[] interactableSources;

    private DefaultVoxelChunkContainer chunkContainer;

    private Interactable interactable;

    private void Start()
    {
        chunkContainer = GetComponent<DefaultVoxelChunkContainer>();
        interactable = GetComponent<Interactable>();
    }

    private bool IsInteractable(SteamVR_Input_Sources source)
    {
        foreach (var sourceType in interactableSources)
        {
            if (source == sourceType)
            {
                return true;
            }
        }
        return false;
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (IsInteractable(hand.handType))
        {
            var world = chunkContainer.Chunk.World;
            var worldObject = world.VoxelWorldObject;
            var worldContainer = worldObject.GetComponent<DefaultVoxelWorldContainer>();

            worldContainer.HoveringHands.Add(hand.handType);

            if (worldContainer.HoveringHands.Count == 1)
            {
                foreach (var pos in world.Chunks)
                {
                    var chunk = world.GetChunk(pos);
                    if (chunk.ChunkObject != null)
                    {
                        chunk.ChunkObject.GetComponent<DefaultVoxelChunkContainer>()?.SetOutlineEnabled(true);
                    }
                }
            }
        }
    }

    private void OnHandHoverEnd(Hand hand)
    {
        var world = chunkContainer.Chunk.World;
        var worldObject = world.VoxelWorldObject;
        var worldContainer = worldObject.GetComponent<DefaultVoxelWorldContainer>();

        worldContainer.HoveringHands.Remove(hand.handType);

        if (worldContainer.HoveringHands.Count == 0)
        {
            foreach (var pos in world.Chunks)
            {
                var chunk = world.GetChunk(pos);
                if (chunk.ChunkObject != null)
                {
                    chunk.ChunkObject.GetComponent<DefaultVoxelChunkContainer>()?.SetOutlineEnabled(false);
                }
            }
        }
    }

    private void HandHoverUpdate(Hand hand)
    {
        var worldObject = chunkContainer.Chunk.World.VoxelWorldObject;
        var worldContainer = worldObject.GetComponent<DefaultVoxelWorldContainer>();

        GrabTypes grabType = hand.GetGrabStarting();
        bool isGrabEnding = hand.IsGrabEnding(worldObject);

        if (worldContainer.InteractableComponent.attachedToHand == null && grabType != GrabTypes.None)
        {
            hand.AttachObject(worldObject, grabType, Hand.AttachmentFlags.ParentToHand |
                                                              Hand.AttachmentFlags.DetachOthers |
                                                              Hand.AttachmentFlags.DetachFromOtherHand |
                                                              Hand.AttachmentFlags.TurnOnKinematic);
            hand.HoverLock(interactable);
        }
        else if (isGrabEnding)
        {
            hand.DetachObject(worldObject);
            hand.HoverUnlock(interactable);
        }
    }

    public class ChunkRaycastMetadata : RaycastMetadata
    {
        public readonly Vector3Int voxel;
        public readonly int material;

        internal ChunkRaycastMetadata(Vector3Int voxel, int material)
        {
            this.voxel = voxel;
            this.material = material;
        }
    }

    public override void HandleRaycast(Vector3 origin, Vector3 start, Vector3 direction, out Vector3 hit, out RaycastMetadata metadata)
    {
        var chunk = chunkContainer.Chunk;
        var world = chunk.World;

        hit = new Vector3(0, 0, 0);
        metadata = null;

        if (world.RayCast(start, direction, chunk.ChunkSize * 1.735f * 2.0f, out var result))
        {
            hit = world.Transform.localToWorldMatrix.MultiplyPoint(result.pos + new Vector3(0.5f, 0.5f, 0.5f));

            var voxelPos = result.isPosEmpty ? new Vector3Int((int)result.nonEmptyPos.x, (int)result.nonEmptyPos.y, (int)result.nonEmptyPos.z) : new Vector3Int((int)result.pos.x, (int)result.pos.y, (int)result.pos.z);
            metadata = new ChunkRaycastMetadata(voxelPos,
                world.GetChunk(ChunkPos.FromVoxel(voxelPos, chunk.ChunkSize)).GetMaterial(
                    ((voxelPos.x % chunk.ChunkSize) + chunk.ChunkSize) % chunk.ChunkSize,
                    ((voxelPos.y % chunk.ChunkSize) + chunk.ChunkSize) % chunk.ChunkSize,
                    ((voxelPos.z % chunk.ChunkSize) + chunk.ChunkSize) % chunk.ChunkSize)
                    );
        }
    }
}
