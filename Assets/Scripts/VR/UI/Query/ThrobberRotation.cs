using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ThrobberRotation : MonoBehaviour
{
    [SerializeField] private float speed = -10.0f;

    [SerializeField] private int steps = 12;

    private Image image;

    private void Start()
    {
        image = GetComponent<Image>();
    }

    private void Update()
    {
        image.transform.localRotation = Quaternion.Euler(0, 0, ((int)(Time.time * speed) % steps) * 360.0f / steps);
    }
}
