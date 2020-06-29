using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class HueSlider : HSVSliderBase
{
    [SerializeField] private Image image;

    private Texture2D hueTexture;
    private Sprite hueSprite;

    protected override void Start()
    {
        base.Start();

        Color[] gradient = {
            new Color(1, 0, 0),
            new Color(1, 1, 0),
            new Color(0, 1, 0),
            new Color(0, 1, 1),
            new Color(0, 0, 1),
            new Color(1, 0, 1),
            new Color(1, 0, 0)
        };
        hueTexture = new Texture2D(gradient.Length, 1);
        hueTexture.SetPixels(gradient);
        hueTexture.Apply();

        hueSprite = Sprite.Create(hueTexture, new Rect(0, 0, gradient.Length, 1), Vector2.one * 0.5f);

        image.sprite = hueSprite;
        image.enabled = true;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Destroy(hueSprite);
        Destroy(hueTexture);
    }

    protected override void OnSliderValueChanged(float value)
    {
        HSVManager.SetHue(value);

        if(HSVManager.Saturation < 0.025f)
        {
            HSVManager.SetSaturation(1.0f);
        }
    }
}
