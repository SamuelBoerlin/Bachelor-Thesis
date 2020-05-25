using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Voxel;
using UnityEngine.EventSystems;
using static VRSculpting;
using Valve.VR;

[RequireComponent(typeof(Canvas))]
public class BrushPropertiesMenu : MonoBehaviour
{
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

    [SerializeField] private HSVManager hsvManager;

    [SerializeField] private Button unionButton;
    [SerializeField] private Button differenceButton;
    [SerializeField] private Button replaceButton;

    [SerializeField] private Button materialButton;
    [SerializeField] private RawImage materialImage;
    [SerializeField] private GameObject materialTilePrefab;
    [SerializeField] private int initialMaterialTileXSpacing = 300;
    [SerializeField] private int initialMaterialTileYSpacing = 0;
    [SerializeField] private int materialTilesPerRow = 3;
    [SerializeField] private int materialTileXSpacing = 210;
    [SerializeField] private int materialTileYSpacing = -210;

    [SerializeField] private Button materialPickButton;
    [SerializeField] private SteamVR_Action_Boolean materialPickAction;

    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;

    private bool initialized = false;

    private List<(GameObject, BrushMaterialButton, ButtonEventEffects, BrushMaterialType)> materialButtons = new List<(GameObject, BrushMaterialButton, ButtonEventEffects, BrushMaterialType)>();

    private bool isMaterialPicking = false;

    public void OnInitializeUI(VRUI.Context ctx)
    {
        VRSculpting = ctx.controller;

        Color.RGBToHSV(VRSculpting.BrushColor, out float h, out float s, out float v);
        hsvManager.SetHue(h);
        hsvManager.SetSaturation(s);
        hsvManager.SetValue(v);

        SetActiveOperation(VRSculpting.BrushOperation);

        initialized = true;
    }

    private void Start()
    {
        hsvManager.onColorChanged.AddListener(OnColorChanged);

        unionButton.onClick.AddListener(OnUnionButtonClick);
        differenceButton.onClick.AddListener(OnDifferenceButtonClick);
        replaceButton.onClick.AddListener(OnReplaceButtonClick);
        materialButton.onClick.AddListener(OnMaterialButtonClick);
        materialPickButton.onClick.AddListener(OnMaterialPickButtonClick);
        undoButton.onClick.AddListener(OnUndoButtonClick);
        redoButton.onClick.AddListener(OnRedoButtonClick);

        UpdateSelectedMaterial();
    }

    private void OnDestroy()
    {
        hsvManager.onColorChanged.RemoveListener(OnColorChanged);

        unionButton.onClick.RemoveListener(OnUnionButtonClick);
        differenceButton.onClick.RemoveListener(OnDifferenceButtonClick);
        replaceButton.onClick.RemoveListener(OnReplaceButtonClick);
        materialButton.onClick.RemoveListener(OnMaterialButtonClick);
        materialPickButton.onClick.RemoveListener(OnMaterialPickButtonClick);
        undoButton.onClick.RemoveListener(OnUndoButtonClick);
        redoButton.onClick.RemoveListener(OnRedoButtonClick);
    }

    private void Update()
    {
        if (isMaterialPicking && materialPickAction != null && materialPickAction.stateDown && VRSculpting.InputModule.RaycastMetadata != null && VRSculpting.InputModule.RaycastMetadata is ChunkHoverInteractions.ChunkRaycastMetadata)
        {
            var voxelColor = MaterialColors.FromInteger(((ChunkHoverInteractions.ChunkRaycastMetadata)VRSculpting.InputModule.RaycastMetadata).material);

            Color.RGBToHSV(voxelColor, out float h, out float s, out float v);
            hsvManager.SetHSV(h, s, v);

            var materialType = VRSculpting.FindBrushMaterialTypeForId(voxelColor.a);
            if (materialType.HasValue)
            {
                VRSculpting.BrushMaterial = materialType.Value;
                UpdateSelectedMaterial();
            }
        }
    }

    private void OnColorChanged()
    {
        //Skip the default values
        if (initialized)
        {
            VRSculpting.BrushColor = hsvManager.Color;
        }
    }

    private void SetActiveOperation(BrushOperation operation)
    {
        VRSculpting.BrushOperation = operation;

        Button button;
        switch (operation)
        {
            default:
            case BrushOperation.Union:
                button = unionButton;
                break;
            case BrushOperation.Difference:
                button = differenceButton;
                break;
            case BrushOperation.Replace:
                button = replaceButton;
                break;
        }

        unionButton.GetComponent<ButtonEventEffects>().PermanentHover = false;
        differenceButton.GetComponent<ButtonEventEffects>().PermanentHover = false;
        replaceButton.GetComponent<ButtonEventEffects>().PermanentHover = false;

        button.GetComponent<ButtonEventEffects>().PermanentHover = true;
    }

    private void OnUnionButtonClick()
    {
        SetActiveOperation(BrushOperation.Union);
    }

    private void OnDifferenceButtonClick()
    {
        SetActiveOperation(BrushOperation.Difference);
    }

    private void OnReplaceButtonClick()
    {
        SetActiveOperation(BrushOperation.Replace);
    }

    private void OnMaterialButtonClick()
    {
        if (materialButtons.Count == 0)
        {
            int i = 0;
            int xOffset = 0;
            int yOffset = 0;

            foreach (var type in VRSculpting.BrushMaterials)
            {
                var tile = Instantiate(materialTilePrefab, materialButton.transform.parent);

                tile.transform.localPosition = materialButton.transform.localPosition + new Vector3(initialMaterialTileXSpacing + xOffset, initialMaterialTileYSpacing + yOffset, 0);
                tile.transform.localScale = materialTilePrefab.transform.localScale;

                var script = tile.GetComponent<BrushMaterialButton>();
                script.Image.texture = type.Texture;

                var hoverEffects = script.Button.GetComponent<ButtonEventEffects>();

                script.Button.onClick.AddListener(() =>
                {
                    VRSculpting.BrushMaterial = type;
                    UpdateSelectedMaterial();
                });

                materialButtons.Add((tile, script, hoverEffects, type));

                xOffset += materialTileXSpacing;

                if (i > 0 && (i + 1) % materialTilesPerRow == 0)
                {
                    xOffset = 0;
                    yOffset += materialTileYSpacing;
                }

                i++;
            }

            UpdateSelectedMaterial();
        }
        else
        {
            foreach (var tile in materialButtons)
            {
                Destroy(tile.Item1);
            }
            materialButtons.Clear();
        }
    }

    private void UpdateSelectedMaterial()
    {
        foreach (var tile in materialButtons)
        {
            tile.Item3.PermanentHover = tile.Item4.Equals(VRSculpting.BrushMaterial);
        }

        materialImage.texture = VRSculpting.BrushMaterial.Texture;
    }

    private void OnMaterialPickButtonClick()
    {
        if (isMaterialPicking)
        {
            isMaterialPicking = false;
            materialPickButton.GetComponent<ButtonEventEffects>().PermanentHover = false;
        }
        else
        {
            isMaterialPicking = true;
            materialPickButton.GetComponent<ButtonEventEffects>().PermanentHover = true;
        }
    }

    private void OnUndoButtonClick()
    {
        VRSculpting.VoxelEditsManager.Instance.Undo();
    }

    private void OnRedoButtonClick()
    {
        VRSculpting.VoxelEditsManager.Instance.Redo();
    }
}
