using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;
using Valve.VR;
using Voxel;

[RequireComponent(typeof(Interactable))]
public class DefaultVoxelWorldContainer : VoxelWorldContainer<LinearIndexer>
{
    public HashSet<SteamVR_Input_Sources> HoveringHands
    {
        get;
        set;
    } = new HashSet<SteamVR_Input_Sources>();

    public Interactable InteractableComponent
    {
        private set;
        get;
    }

    protected override void Start()
    {
        base.Start();
        InteractableComponent = GetComponent<Interactable>();
    }

    protected override IndexerFactory<LinearIndexer> CreateIndexerFactory()
    {
        return (xSize, ySize, zSize) => new LinearIndexer(xSize, ySize, zSize);
    }
}