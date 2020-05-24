using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public abstract class VRPointerRaycastHandler : MonoBehaviour
{
    public class RaycastMetadata
    {

    }

    public abstract void HandleRaycast(Vector3 origin, Vector3 start, Vector3 direction, out Vector3 hit, out RaycastMetadata metadata);
}