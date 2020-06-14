using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class QueryMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;

    [SerializeField] private Button clearButton;

    [SerializeField] private Button resetButton;

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
        resetButton.onClick.AddListener(OnResetButtonClicked);
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

    private void OnResetButtonClicked()
    {
        VRSculpting.VoxelWorld.Instance.Clear();
        VRSculpting.BrushScale = 1.0f;
        VRSculpting.BrushRotation = Quaternion.identity;
        VRSculpting.BrushType = BrushType.Box;
        VRSculpting.BrushOperation = Voxel.BrushOperation.Union;
        VRSculpting.BrushMaterial = VRSculpting.BrushMaterials[0];
        VRSculpting.BrushColor = Color.white;
        VRSculpting.QueryResultDisplay.ResetDisplay();
        VRSculpting.CloseMenu(gameObject);
    }
}
