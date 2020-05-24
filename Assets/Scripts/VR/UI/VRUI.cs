using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VRUI : MonoBehaviour
{
    public readonly struct Context
    {
        public readonly VRSculpting controller;
        public readonly VRPointerInputModule inputModule;
        public readonly Camera eventCamera;

        public Context(VRSculpting controller, VRPointerInputModule inputModule, Camera eventCamera)
        {
            this.controller = controller;
            this.inputModule = inputModule;
            this.eventCamera = eventCamera;
        }
    }

    public UnityEvent<Context> onInitialize;

    [SerializeField] private bool setCanvasEventCameras = true;

    public void InitializeUI(VRSculpting controller, VRPointerInputModule inputModule, Camera eventCamera)
    {
        var ctx = new Context(controller, inputModule, eventCamera);

        BroadcastMessage("OnInitializeUI", ctx, SendMessageOptions.DontRequireReceiver);

        onInitialize?.Invoke(ctx);

        if (setCanvasEventCameras)
        {
            foreach (var canvas in GetComponents<Canvas>())
            {
                canvas.worldCamera = eventCamera;
            }
            foreach (var canvas in GetComponentsInChildren<Canvas>())
            {
                canvas.worldCamera = eventCamera;
            }
        }
    }
}
