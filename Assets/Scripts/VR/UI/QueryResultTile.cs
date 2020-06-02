using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class QueryResultTile : MonoBehaviour
{
    [SerializeField] private Button _button;
    public Button Button
    {
        get
        {
            return _button;
        }
    }

    [SerializeField] private ObjectPreview _objectPreview;
    public ObjectPreview ObjectPreview
    {
        get
        {
            return _objectPreview;
        }
    }
}
