using UnityEngine;
using System.Collections;
using Valve.VR.InteractionSystem;
using Voxel;
using System.Collections.Generic;
using Valve.VR;

[RequireComponent(typeof(DefaultVoxelChunkContainer))]
[RequireComponent(typeof(Interactable))]
public class ChunkHoverInteractions : MonoBehaviour
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
}
