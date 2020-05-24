using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ValueSlider : HSVSliderBase
{
    [SerializeField] private Image image;

    private Texture2D valueTexture;
    private Sprite valueSprite;

    protected override void Start()
    {
        base.Start();

        UpdateValueTexture();

        image.sprite = valueSprite;
        image.enabled = true;
    }

    public void OnSetHSVHue(float value)
    {
        UpdateValueTexture();
    }

    public void OnSetHSVSaturation(float value)
    {
        UpdateValueTexture();
    }

    private void UpdateValueTexture()
    {
        if (valueTexture == null)
        {
            valueTexture = new Texture2D(4, 1);
            valueSprite = Sprite.Create(valueTexture, new Rect(1, 0, 2, 1), Vector2.one * 0.5f);
        }

        Color[] gradient =
        {
            new Color(0, 0, 0),
            new Color(0, 0, 0),
            HSVManager.HueSaturationColor,
            HSVManager.HueSaturationColor
        };
        valueTexture.SetPixels(gradient);
        valueTexture.Apply();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Destroy(valueSprite);
        Destroy(valueTexture);
    }

    protected override void OnSliderValueChanged(float value)
    {
        HSVManager.SetValue(value);
    }
}
