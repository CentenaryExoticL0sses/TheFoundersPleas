using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles direct interaction with the hex map grid, applying edits.
/// Receives commands from an external UI controller.
/// </summary>
public class HexMapEditor : MonoBehaviour
{
    public enum OptionalToggle { Ignore, Yes, No }

    [SerializeField] private InputProvider _inputProvider;
    [SerializeField] private HexGrid _hexGrid;
    [SerializeField] private Material _terrainMaterial;

    private int _activeElevation;
    private int _activeWaterLevel;
    private int _activeUrbanLevel;
    private int _activeFarmLevel;
    private int _activePlantLevel;
    private int _activeSpecialIndex;
    private int _activeTerrainTypeIndex = -1;
    private int _brushSize;

    private bool _applyElevation = true;
    private bool _applyWaterLevel = true;
    private bool _applyUrbanLevel;
    private bool _applyFarmLevel;
    private bool _applyPlantLevel;
    private bool _applySpecialIndex;

    private OptionalToggle _riverMode;
    private OptionalToggle _roadMode;
    private OptionalToggle _unitsMode;
    private OptionalToggle _walledMode;

    private Vector2 _mousePosition;
    private bool _isInteraction;

    private bool _isDrag;
    private HexDirection _dragDirection;
    private HexCell _previousCell;

    private static readonly string EditKeyword = "_HEX_MAP_EDIT_MODE";
    private static readonly string GridKeyword = "_SHOW_GRID";
    private static readonly int CellHighlighId = Shader.PropertyToID("_CellHighlighting");

    #region Public Setters for UI Interaction
    public void SetActiveTerrainTypeIndex(int index) => _activeTerrainTypeIndex = index;
    public void SetApplyElevation(bool apply) => _applyElevation = apply;
    public void SetActiveElevation(int elevation) => _activeElevation = elevation;
    public void SetApplyWaterLevel(bool apply) => _applyWaterLevel = apply;
    public void SetActiveWaterLevel(int level) => _activeWaterLevel = level;
    public void SetRiverMode(OptionalToggle mode) => _riverMode = mode;
    public void SetRoadMode(OptionalToggle mode) => _roadMode = mode;
    public void SetUnitsMode(OptionalToggle mode) => _unitsMode = mode;
    public void SetBrushSize(int size) => _brushSize = size;
    public void SetApplyUrbanLevel(bool apply) => _applyUrbanLevel = apply;
    public void SetActiveUrbanLevel(int level) => _activeUrbanLevel = level;
    public void SetApplyFarmLevel(bool apply) => _applyFarmLevel = apply;
    public void SetActiveFarmLevel(int level) => _activeFarmLevel = level;
    public void SetApplyPlantLevel(bool apply) => _applyPlantLevel = apply;
    public void SetActivePlantLevel(int level) => _activePlantLevel = level;
    public void SetApplySpecialIndex(bool apply) => _applySpecialIndex = apply;
    public void SetActiveSpecialIndex(int index) => _activeSpecialIndex = index;
    public void SetWalledMode(OptionalToggle mode) => _walledMode = mode;
    public void ShowGrid() => _terrainMaterial.EnableKeyword(GridKeyword);
    public void HideGrid() => _terrainMaterial.DisableKeyword(GridKeyword);
    #endregion

    private void Awake()
    {
        Shader.EnableKeyword(EditKeyword);
        _inputProvider.PointerMoved += OnPointerMove;
        _inputProvider.InteractPerformed += OnInteractPerformed;
        _inputProvider.InteractCancelled += OnInteractCanceled;
    }

    private void OnDestroy()
    {
        _inputProvider.PointerMoved -= OnPointerMove;
        _inputProvider.InteractPerformed -= OnInteractPerformed;
        _inputProvider.InteractCancelled -= OnInteractCanceled;
    }

    private void OnEnable() => ClearCellHighlightData();
    private void OnDisable() => ClearCellHighlightData();

    private void OnPointerMove(Vector2 mousePosition) => _mousePosition = mousePosition;
    private void OnInteractPerformed() => _isInteraction = true;
    private void OnInteractCanceled() => _isInteraction = false;

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (_isInteraction)
            {
                HandleInput();
                return;
            }
            UpdateCellHighlightData(GetCellUnderCursor());
        }
        else
        {
            ClearCellHighlightData();
        }
        _previousCell = default;
    }

    private HexCell GetCellUnderCursor()
    {
        Ray ray = Camera.main.ScreenPointToRay(_mousePosition);
        return _hexGrid.GetCell(ray, _previousCell);
    }

    private void HandleInput()
    {
        HexCell currentCell = GetCellUnderCursor();
        if (currentCell)
        {
            if (_previousCell && _previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                _isDrag = false;
            }
            EditCells(currentCell);
            _previousCell = currentCell;
        }
        else
        {
            _previousCell = default;
        }
        UpdateCellHighlightData(currentCell);
    }

    private void UpdateCellHighlightData(HexCell cell)
    {
        if (!cell)
        {
            ClearCellHighlightData();
            return;
        }

        Shader.SetGlobalVector(
            CellHighlighId,
            new Vector4(
                cell.Coordinates.HexX,
                cell.Coordinates.HexZ,
                _brushSize * _brushSize + 0.5f,
                HexMetrics.wrapSize
            )
        );
    }

    private void ClearCellHighlightData() => Shader.SetGlobalVector(
        CellHighlighId, new Vector4(0f, 0f, -1f, 0f));

    private void ValidateDrag(HexCell currentCell)
    {
        for (_dragDirection = HexDirection.NE;
            _dragDirection <= HexDirection.NW;
            _dragDirection++)
        {
            if (_previousCell.GetNeighbor(_dragDirection) == currentCell)
            {
                _isDrag = true;
                return;
            }
        }
        _isDrag = false;
    }

    private void EditCells(HexCell center)
    {
        int centerX = center.Coordinates.X;
        int centerZ = center.Coordinates.Z;

        for (int r = 0, z = centerZ - _brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + _brushSize; x++)
            {
                EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - _brushSize; x <= centerX + r; x++)
            {
                EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell cell)
    {
        if (cell)
        {
            if (_activeTerrainTypeIndex >= 0)
            {
                cell.SetTerrainTypeIndex(_activeTerrainTypeIndex);
            }
            if (_applyElevation)
            {
                cell.SetElevation(_activeElevation);
            }
            if (_applyWaterLevel)
            {
                cell.SetWaterLevel(_activeWaterLevel);
            }
            if (_applySpecialIndex)
            {
                cell.SetSpecialIndex(_activeSpecialIndex);
            }
            if (_applyUrbanLevel)
            {
                cell.SetUrbanLevel(_activeUrbanLevel);
            }
            if (_applyFarmLevel)
            {
                cell.SetFarmLevel(_activeFarmLevel);
            }
            if (_applyPlantLevel)
            {
                cell.SetPlantLevel(_activePlantLevel);
            }
            if (_riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            if (_roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }
            if (_unitsMode == OptionalToggle.No)
            {
                DestroyUnit(cell);
            }
            if (_unitsMode == OptionalToggle.Yes)
            {
                CreateUnit(cell);
            }
            if (_walledMode != OptionalToggle.Ignore)
            {
                cell.SetWalled(_walledMode == OptionalToggle.Yes);
            }

            if (_isDrag && cell.TryGetNeighbor(_dragDirection.Opposite(), out HexCell otherCell))
            {
                if (_riverMode == OptionalToggle.Yes)
                {
                    otherCell.SetOutgoingRiver(_dragDirection);
                }
                if (_roadMode == OptionalToggle.Yes)
                {
                    otherCell.AddRoad(_dragDirection);
                }
            }
        }
    }

    private void CreateUnit(HexCell cell)
    {
        if (cell && !cell.Unit)
        {
            _hexGrid.AddUnit(
                Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f)
            );
        }
    }

    private void DestroyUnit(HexCell cell)
    {
        if (cell && cell.Unit)
        {
            _hexGrid.RemoveUnit(cell.Unit);
        }
    }
}