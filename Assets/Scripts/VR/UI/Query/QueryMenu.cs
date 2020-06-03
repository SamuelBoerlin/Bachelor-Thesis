using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class QueryMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;

    [SerializeField] private Button clearButton;

    [SerializeField] private VRSculpting _sculpting;
    public VRSculpting VRSculpting
    {
        get
        {
            return _sculpting;
        }
        set
        {
            _sculpting = value;
        }
    }

    public void OnInitializeUI(VRUI.Context ctx)
    {
        VRSculpting = ctx.controller;
    }

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        clearButton.onClick.AddListener(OnClearButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        VRSculpting.QueryResultDisplay.PrepareNewQuery(VRSculpting.CineastApi.StartVoxelQuery(VRSculpting.VoxelWorld));
        VRSculpting.CloseMenu(gameObject);
    }

    private void OnClearButtonClicked()
    {
        VRSculpting.QueryResultDisplay.ResetDisplay();
        VRSculpting.CloseMenu(gameObject);
    }
}
