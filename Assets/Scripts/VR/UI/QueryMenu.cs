using UnityEngine;
using System.Collections;

public class QueryMenu : MonoBehaviour
{
    private VRSculpting sculpting;

    public void OnInitializeUI(VRUI.Context ctx)
    {
        sculpting = ctx.controller;
    }
}
