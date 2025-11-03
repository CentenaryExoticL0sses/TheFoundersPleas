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

        root.Q<RadioButtonGroup>("Terrain").RegisterValueChangedCallback(
            change => _hexMapEditor.SetActiveTerrainTypeIndex(change.newValue - 1));

        root.Q<Toggle>("ApplyElevation").RegisterValueChangedCallback(
            change => _hexMapEditor.SetApplyElevation(change.newValue));
        root.Q<SliderInt>("Elevation").RegisterValueChangedCallback(
            change => _hexMapEditor.SetActiveElevation(change.newValue));

        root.Q<Toggle>("ApplyWaterLevel").RegisterValueChangedCallback(
            change => _hexMapEditor.SetApplyWaterLevel(change.newValue));
        root.Q<SliderInt>("WaterLevel").RegisterValueChangedCallback(
            change => _hexMapEditor.SetActiveWaterLevel(change.newValue));

        root.Q<RadioButtonGroup>("River").RegisterValueChangedCallback(
            change => _hexMapEditor.SetRiverMode((HexMapEditor.OptionalToggle)change.newValue));

        root.Q<RadioButtonGroup>("Roads").RegisterValueChangedCallback(
            change => _hexMapEditor.SetRoadMode((HexMapEditor.OptionalToggle)change.newValue));

        root.Q<RadioButtonGroup>("Units").RegisterValueChangedCallback(
            change => _hexMapEditor.SetUnitsMode((HexMapEditor.OptionalToggle)change.newValue));

        root.Q<SliderInt>("BrushSize").RegisterValueChangedCallback(
            change => _hexMapEditor.SetBrushSize(change.newValue));

        root.Q<Toggle>("ApplyUrbanLevel").RegisterValueChangedCallback(
            change => _hexMapEditor.SetApplyUrbanLevel(change.newValue));
        root.Q<SliderInt>("UrbanLevel").RegisterValueChangedCallback(
            change => _hexMapEditor.SetActiveUrbanLevel(change.newValue));

        root.Q<Toggle>("ApplyFarmLevel").RegisterValueChangedCallback(
            change => _hexMapEditor.SetApplyFarmLevel(change.newValue));
        root.Q<SliderInt>("FarmLevel").RegisterValueChangedCallback(
            change => _hexMapEditor.SetActiveFarmLevel(change.newValue));

        root.Q<Toggle>("ApplyPlantLevel").RegisterValueChangedCallback(
            change => _hexMapEditor.SetApplyPlantLevel(change.newValue));
        root.Q<SliderInt>("PlantLevel").RegisterValueChangedCallback(
            change => _hexMapEditor.SetActivePlantLevel(change.newValue));

        root.Q<Toggle>("ApplySpecialIndex").RegisterValueChangedCallback(
            change => _hexMapEditor.SetApplySpecialIndex(change.newValue));
        root.Q<SliderInt>("SpecialIndex").RegisterValueChangedCallback(
            change => _hexMapEditor.SetActiveSpecialIndex(change.newValue));

        root.Q<RadioButtonGroup>("Walled").RegisterValueChangedCallback(
            change => _hexMapEditor.SetWalledMode((HexMapEditor.OptionalToggle)change.newValue));

        root.Q<Button>("SaveButton").clicked += () => _saveLoadMenu.Open(true);
        root.Q<Button>("LoadButton").clicked += () => _saveLoadMenu.Open(false);

        root.Q<Button>("NewMapButton").clicked += _newMapMenu.Open;

        root.Q<Toggle>("Grid").RegisterValueChangedCallback(change => {
            if (change.newValue)
            {
                _hexMapEditor.ShowGrid();
            }
            else
            {
                _hexMapEditor.HideGrid();
            }
        });

        root.Q<Toggle>("EditMode").RegisterValueChangedCallback(change => {
            _hexMapEditor.enabled = change.newValue;
            _hexGameUI.SetEditMode(change.newValue);
        });
    }
}