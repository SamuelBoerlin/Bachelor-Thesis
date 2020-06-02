using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectPreviewToTextureRendererEditor
{
    [MenuItem("GameObject/UI/ObjectToTextureRenderManager", false, 0)]
    public static void CreateObjectRenderer()
    {
        var obj = new GameObject("ObjectToTextureRenderManager");

        var camera = obj.AddComponent<Camera>();
        camera.orthographic = false;
        camera.clearFlags = CameraClearFlags.Color;
        camera.backgroundColor = Color.clear;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1000.0f;
        camera.enabled = false;
        camera.cullingMask = 0;

        var manager = obj.AddComponent<ObjectToTextureRenderManager>();
        manager.Camera = camera;
    }
}
