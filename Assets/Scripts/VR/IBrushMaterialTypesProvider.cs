using UnityEngine;
using System.Collections;
using static VRSculpting;

public interface IBrushMaterialsProvider
{
    BrushMaterialType[] BrushMaterials
    {
        get;
    }

    BrushMaterialType BrushMaterial
    {
        get;
        set;
    }
}
