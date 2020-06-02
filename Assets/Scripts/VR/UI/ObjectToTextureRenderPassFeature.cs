using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ObjectToTextureRenderPassFeature : ScriptableRendererFeature
{
    private class Pass : ScriptableRenderPass
    {
        private readonly ObjectToTextureRenderManager manager;

        internal Pass(ObjectToTextureRenderManager manager)
        {
            this.manager = manager;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var buffer = CommandBufferPool.Get("Object to texture renderer buffer");
            try
            {
                foreach (var entry in manager.RenderTargets)
                {
                    buffer.SetRenderTarget(entry.Value);
                    buffer.ClearRenderTarget(true, true, Color.clear);

                    var objectRenderer = entry.Key.GetComponent<Renderer>();
                    if (objectRenderer)
                    {
                        buffer.DrawRenderer(objectRenderer, objectRenderer.sharedMaterial, 0, 0);
                    }

                    foreach (var transform in entry.Key.GetComponentsInChildren<Transform>(false))
                    {
                        objectRenderer = transform.gameObject.GetComponent<Renderer>();
                        if (objectRenderer)
                        {
                            buffer.DrawRenderer(objectRenderer, objectRenderer.sharedMaterial, 0, 0);
                        }
                    }

                    manager.RenderedTargets.Add(entry.Key);

                    //TODO Set limit somewhere
                    break;
                }

                context.ExecuteCommandBuffer(buffer);
            }
            finally
            {
                CommandBufferPool.Release(buffer);
            }
        }
    }

    private Pass pass;
    private ObjectToTextureRenderManager manager;

    public override void Create()
    {
        try
        {
            manager = GameObject.FindObjectOfType<ObjectToTextureRenderManager>();
            if (manager)
            {
                pass = new Pass(manager)
                {
                    renderPassEvent = manager.PassEvent
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (manager != null && pass != null && renderingData.cameraData.camera == manager.Camera)
        {
            renderer.EnqueuePass(pass);
        }
    }
}
