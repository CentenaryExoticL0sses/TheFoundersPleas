using TheFoundersPleas.Core.Enums;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controls the Hex Map Editor UI, querying UI elements and
/// sending commands to the HexMapEditor component.
/// </summary>
public class HexMapEditorUI : MonoBehaviour
{
    [SerializeField] private HexMapEditor _hexMapEditor;
    [SerializeField] private HexGamePlayer _hexGameUI;

    [SerializeField] private UIDocument _sidePanels;
    [SerializeField] private NewMapMenu _newMapMenu;
    [SerializeField] private SaveLoadMenu _saveLoadMenu;

    private void OnEnable()
    {
        VisualElement root = _sidePanels.rootVisualElement;

        root.Q<RadioButtonGroup>("Terrain").RegisterValueChangedCallback(change => _hexMapEditor.ActiveTerrainType = (TerrainType)(change.newValue - 1));

        root.Q<Toggle>("ApplyElevation").RegisterValueChangedCallback(change => _hexMapEditor.ApplyElevation = change.newValue);
        root.Q<SliderInt>("Elevation").RegisterValueChangedCallback(change => _hexMapEditor.ActiveElevation = change.newValue);

        root.Q<Toggle>("ApplyWaterLevel").RegisterValueChangedCallback(change => _hexMapEditor.ApplyWaterLevel = change.newValue);
        root.Q<SliderInt>("WaterLevel").RegisterValueChangedCallback(change => _hexMapEditor.ActiveWaterLevel = change.newValue);

        root.Q<RadioButtonGroup>("River").RegisterValueChangedCallback(change => _hexMapEditor.RiverMode = (HexMapEditor.OptionalToggle)change.newValue);
        root.Q<RadioButtonGroup>("Roads").RegisterValueChangedCallback(change => _hexMapEditor.RoadMode = (HexMapEditor.OptionalToggle)change.newValue);
        root.Q<RadioButtonGroup>("Units").RegisterValueChangedCallback(change => _hexMapEditor.UnitsMode = (HexMapEditor.OptionalToggle)change.newValue);

        root.Q<SliderInt>("BrushSize").RegisterValueChangedCallback(change => _hexMapEditor.BrushSize = change.newValue);

        root.Q<Toggle>("ApplyAnimalType").RegisterValueChangedCallback(change => _hexMapEditor.ApplyUrbanLevel = change.newValue);
        root.Q<EnumField>("AnimalType").RegisterValueChangedCallback(change => _hexMapEditor.ActiveAnimalType = (AnimalType)change.newValue);

        root.Q<Toggle>("ApplyPlantType").RegisterValueChangedCallback(change => _hexMapEditor.ApplyFarmLevel = change.newValue);
        root.Q<EnumField>("PlantType").RegisterValueChangedCallback(change => _hexMapEditor.ActivePlantType = (PlantType)change.newValue);

        root.Q<Toggle>("ApplyMineralType").RegisterValueChangedCallback(change => _hexMapEditor.ApplyPlantLevel = change.newValue);
        root.Q<EnumField>("MineralType").RegisterValueChangedCallback(change => _hexMapEditor.ActiveMineralType = (MineralType)change.newValue);

        root.Q<Toggle>("ApplyStructureType").RegisterValueChangedCallback(change => _hexMapEditor.ApplySpecialIndex = change.newValue);
        root.Q<EnumField>("StructureType").RegisterValueChangedCallback(change => _hexMapEditor.ActiveStructureType = (StructureType)change.newValue);

        root.Q<RadioButtonGroup>("Walled").RegisterValueChangedCallback(change => _hexMapEditor.WalledMode = (HexMapEditor.OptionalToggle)change.newValue);

        root.Q<Button>("SaveButton").clicked += () => _saveLoadMenu.Open(true);
        root.Q<Button>("LoadButton").clicked += () => _saveLoadMenu.Open(false);
        root.Q<Button>("NewMapButton").clicked += _newMapMenu.Open;

        root.Q<Toggle>("Grid").RegisterValueChangedCallback(change => {
            if (change.newValue) _hexMapEditor.ShowGrid();
            else _hexMapEditor.HideGrid();
        });

        root.Q<Toggle>("EditMode").RegisterValueChangedCallback(change => {
            _hexMapEditor.enabled = change.newValue;
            _hexGameUI.SetEditMode(change.newValue);
        });
    }
}