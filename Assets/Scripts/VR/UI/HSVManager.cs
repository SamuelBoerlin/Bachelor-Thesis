using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class HSVManager : MonoBehaviour
{
    [SerializeField] private HSVSliderBase hueSlider;
    [SerializeField] private HSVSliderBase saturationSlider;
    [SerializeField] private HSVSliderBase valueSlider;

    [SerializeField] private bool usePreviewTexture = false;

    [SerializeField] private Image image;
    [SerializeField] private RawImage rawImage;

    private float hue, saturation, value;

    /// <summary>
    /// Current color
    /// </summary>
    public Color Color
    {
        get;
        private set;
    } = Color.white;

    /// <summary>
    /// Current color with S=1, V=1
    /// </summary>
    public Color HueColor
    {
        get;
        private set;
    } = Color.white;

    /// <summary>
    /// Current color with V=1
    /// </summary>
    public Color HueSaturationColor
    {
        get;
        private set;
    } = Color.white;

    private Texture2D previewTexture;
    private Sprite previewSprite;

    public UnityEvent onColorChanged;

    private void Awake()
    {
        hueSlider.HSVManager = this;
        saturationSlider.HSVManager = this;
        valueSlider.HSVManager = this;
    }

    private void Start()
    {
        SetHue(hueSlider.Slider.value);
        SetSaturation(saturationSlider.Slider.value);
        SetValue(valueSlider.Slider.value);

        if (image != null && previewSprite != null)
        {
            image.sprite = previewSprite;
        }

        if (rawImage != null && previewTexture != null)
        {
            rawImage.texture = previewTexture;
        }
    }

    private void OnDestroy()
    {
        if (previewTexture != null)
        {
            Destroy(previewTexture);
            Destroy(previewSprite);
        }
    }

    public void SetHSV(float h, float s, float v)
    {
        hueSlider.Slider.value = h;
        saturationSlider.Slider.value = s;
        valueSlider.Slider.value = v;
        hue = h;
        saturation = s;
        value = v;
        UpdateColor();
        BroadcastMessage("OnSetHSVHue", value, SendMessageOptions.DontRequireReceiver);
        BroadcastMessage("OnSetHSVSaturation", value, SendMessageOptions.DontRequireReceiver);
        BroadcastMessage("OnSetHSVValue", value, SendMessageOptions.DontRequireReceiver);
        onColorChanged?.Invoke();
    }

    public void SetHue(float value)
    {
        hueSlider.Slider.value = value;
        hue = value;
        UpdateColor();
        BroadcastMessage("OnSetHSVHue", value, SendMessageOptions.DontRequireReceiver);
        onColorChanged?.Invoke();
    }

    public void SetSaturation(float value)
    {
        saturationSlider.Slider.value = value;
        saturation = value;
        UpdateColor();
        BroadcastMessage("OnSetHSVSaturation", value, SendMessageOptions.DontRequireReceiver);
        onColorChanged?.Invoke();
    }

    public void SetValue(float value)
    {
        valueSlider.Slider.value = value;
        this.value = value;
        UpdateColor();
        BroadcastMessage("OnSetHSVValue", value, SendMessageOptions.DontRequireReceiver);
        onColorChanged?.Invoke();
    }

    private void UpdateColor()
    {
        HueColor = Color.HSVToRGB(hue, 1, 1);
        HueSaturationColor = Color.HSVToRGB(hue, saturation, 1);
        Color = Color.HSVToRGB(hue, saturation, value);

        if (usePreviewTexture)
        {
            if (previewTexture == null)
            {
                previewTexture = new Texture2D(1, 1);
                previewSprite = Sprite.Create(previewTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            }

            previewTexture.SetPixels(new Color[] { Color });
            previewTexture.Apply();
        }
        else
        {
            if (image != null)
            {
                image.color = Color;
            }

            if (rawImage != null)
            {
                rawImage.color = Color;
            }
        }
    }
}
