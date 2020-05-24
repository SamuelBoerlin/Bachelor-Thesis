using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SaturationSlider : HSVSliderBase
{
    [SerializeField] private Image image;

    private Texture2D saturationTexture;
    private Sprite saturationSprite;

    protected override void Start()
    {
        base.Start();

        UpdateSaturationTexture();

        image.sprite = saturationSprite;
        image.enabled = true;
    }

    public void OnSetHSVHue(float value)
    {
        UpdateSaturationTexture();
    }

    private void UpdateSaturationTexture()
    {
        if (saturationTexture == null)
        {
            saturationTexture = new Texture2D(4, 1);
            saturationSprite = Sprite.Create(saturationTexture, new Rect(1, 0, 2, 1), Vector2.one * 0.5f);
        }

        Color[] gradient =
        {
            new Color(1, 1, 1),
            new Color(1, 1, 1),
            HSVManager.HueColor,
            HSVManager.HueColor
        };
        saturationTexture.SetPixels(gradient);
        saturationTexture.Apply();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Destroy(saturationSprite);
        Destroy(saturationTexture);
    }

    protected override void OnSliderValueChanged(float value)
    {
        HSVManager.SetSaturation(value);
    }
}
