using TheFoundersPleas.InputSystem;
using TheFoundersPleas.World;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HexGrid _hexGrid;
    [SerializeField] private HexMapGenerator _mapGenerator;
    [SerializeField] private HexMapCreator _mapCreator;
    [SerializeField] private InputProvider _inputProvider;
    [SerializeField] private HexMapEditor _mapEditor;
    [SerializeField] private HexGamePlayer _gamePlayer;
    [SerializeField] private HexMapCamera _camera;
    [SerializeField] private NewMapMenu _newMapMenu;
    [SerializeField] private SaveLoadMenu _saveLoadMenu;
    [SerializeField] private HexMapEditorUI _mapEditorUI;

    private void Start()
    {
        _hexGrid.Initialize();
        _inputProvider.Initialize();
        _camera.Initialize(_hexGrid, _inputProvider);
        _mapGenerator.Initialize(_hexGrid);
        _mapCreator.Initialize(_hexGrid, _mapGenerator, _camera);
        _mapEditor.Initialize(_hexGrid, _inputProvider);
        _gamePlayer.Initialize(_hexGrid, _inputProvider);
        _newMapMenu.Initialize(_mapCreator, _camera);
        _saveLoadMenu.Initialize(_hexGrid, _camera);
        _mapEditorUI.Initialize(_mapEditor, _gamePlayer);
        _mapCreator.CreateMap();
    }
}
