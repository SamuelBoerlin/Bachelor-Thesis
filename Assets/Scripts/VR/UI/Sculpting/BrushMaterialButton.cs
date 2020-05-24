using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BrushMaterialButton : MonoBehaviour
{
    [SerializeField] private RawImage _image;
    public RawImage Image
    {
        get
        {
            return _image;
        }
    }

    [SerializeField] private Button _button;
    public Button Button
    {
        get
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
            return _button;
        }
    }
}
