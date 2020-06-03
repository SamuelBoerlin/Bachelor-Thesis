using UnityEngine;
using System.Collections;

public class ObjectLabel : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    public Camera Camera
    {
        get
        {
            return _camera;
        }
        set
        {
            _camera = value;
        }
    }

    [SerializeField] private Transform _anchor;

    [SerializeField] private float _height = 0.6f;

    private void LateUpdate()
    {
        Vector3 localUp = _anchor.transform.worldToLocalMatrix.MultiplyVector(Vector3.up).normalized;
        transform.localPosition = localUp * _height;
        transform.rotation = Quaternion.LookRotation(Camera.transform.forward, Camera.transform.up);
    }
}
