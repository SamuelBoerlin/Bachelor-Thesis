using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class HSVSliderBase : MonoBehaviour
{
    public virtual HSVManager HSVManager
    {
        get;
        set;
    }

    private Slider _slider;
    public Slider Slider
    {
        get
        {
            if (_slider == null)
            {
                _slider = GetComponent<Slider>();
            }
            return _slider;
        }
    }

    protected virtual void Start()
    {
        Slider.onValueChanged.AddListener(OnSliderValueChangedInternal);
    }

    protected virtual void OnDestroy()
    {
        Slider.onValueChanged.RemoveListener(OnSliderValueChangedInternal);
    }

    private void OnSliderValueChangedInternal(float value)
    {
        OnSliderValueChanged(value);
    }

    protected virtual void OnSliderValueChanged(float value)
    {
    }
}
