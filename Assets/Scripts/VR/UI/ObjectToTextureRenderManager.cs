using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class ObjectToTextureRenderManager : MonoBehaviour
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

    public Camera Camera
    {
        get;
        private set;
    }

    [Tooltip("Camera render layer. This should be set to a layer that does not contain any other objects.")]
    [SerializeField, Layer] private int cameraLayer;

    [Tooltip("Instantiated preview object layer. This should be set to a different layer than the camera layer and should also not interfere with other objects.")]
    [SerializeField, Layer] private int objectLayer;

    [SerializeField] private RenderPassEvent _passEvent = RenderPassEvent.AfterRendering;
    public RenderPassEvent PassEvent
    {
        get
        {
            return _passEvent;
        }
        set
        {
            _passEvent = value;
        }
    }

    public Dictionary<GameObject, RenderTexture> RenderTargets
    {
        get;
        private set;
    } = new Dictionary<GameObject, RenderTexture>();

    private void Awake()
    {
        if (Camera == null)
        {
            Camera = GetComponent<Camera>();
        }
        Camera.cullingMask = 1 << cameraLayer;
    }

    private void LateUpdate()
    {
        if (RenderTargets.Count > 0)
        {
            Camera.Render();

            foreach (var entry in RenderTargets)
            {
                Destroy(entry.Key);
            }
            RenderTargets.Clear();
        }
    }

    public void QueueRenderer(GameObject obj, Vector3 position, Quaternion rotation, Vector3 scale, int width, int height, ref RenderTexture renderTexture)
    {
        if (RenderTargets.Count > 32)
        {
            Debug.LogWarning("Too many render targets");
            return;
        }

        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(width, height, 16);
        }

        var instance = Instantiate(obj, Camera.transform);

        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = scale;
        instance.layer = objectLayer;

        RenderTargets.Add(instance, renderTexture);
    }
}
