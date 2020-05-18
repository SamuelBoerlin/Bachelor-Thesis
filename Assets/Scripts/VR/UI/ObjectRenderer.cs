using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ObjectRenderer : MonoBehaviour
{
    [MenuItem("GameObject/UI/Object Renderer", false, 0)]
    public static void CreateObjectRenderer()
    {
        GameObject obj = new GameObject("Object Renderer");

        Camera camera = obj.AddComponent<Camera>();

        camera.orthographic = false;
        camera.clearFlags = CameraClearFlags.Color;
        camera.backgroundColor = Color.clear;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1000.0f;
        camera.enabled = false;
        camera.cullingMask = 0;

        var renderScript = obj.AddComponent<ObjectRenderer>();
        renderScript.camera = camera;
    }

    private Camera camera;

    [SerializeField, Layer] private int renderLayer;

    private void Awake()
    {
        if (camera == null)
        {
            camera = GetComponent<Camera>();
        }
        camera.cullingMask = 1 << renderLayer;
    }

    public void Render(GameObject obj, Vector3 position, Quaternion rotation, Vector3 scale, int width, int height, ref RenderTexture renderTexture)
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(width, height, 16);
        }
        var prevRenderTexture = RenderTexture.active;

        var instance = Instantiate(obj, camera.transform);

        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = scale;
        instance.layer = renderLayer;

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;

            camera.Render();
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = prevRenderTexture;

            DestroyImmediate(instance);
        }
    }
}
