using TheFoundersPleas.Core.Enums;
using TheFoundersPleas.InputSystem;
using TheFoundersPleas.World;
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

    public int ActiveElevation { get; set; }
    public int ActiveWaterLevel { get; set; }
    public AnimalType ActiveAnimalType { get; set; }
    public PlantType ActivePlantType { get; set; }
    public MineralType ActiveMineralType { get; set; }
    public StructureType ActiveStructureType { get; set; }
    public TerrainType ActiveTerrainType { get; set; }
    public int BrushSize { get; set; }

    public bool ApplyElevation { get; set; }
    public bool ApplyWaterLevel { get; set; }
    public bool ApplyUrbanLevel { get; set; }
    public bool ApplyFarmLevel { get; set; }
    public bool ApplyPlantLevel { get; set; }
    public bool ApplySpecialIndex { get; set; }

    public OptionalToggle RiverMode { get; set; }
    public OptionalToggle RoadMode { get; set; }
    public OptionalToggle UnitsMode { get; set; }
    public OptionalToggle WalledMode { get; set; }

    private Vector2 _mousePosition;
    private bool _isInteraction;

    private bool _isDrag;
    private HexDirection _dragDirection;
    private HexCell _previousCell;

    private static readonly string EditKeyword = "_HEX_MAP_EDIT_MODE";
    private static readonly string GridKeyword = "_SHOW_GRID";
    private static readonly int CellHighlighId = Shader.PropertyToID("_CellHighlighting");

    public void ShowGrid() => _terrainMaterial.EnableKeyword(GridKeyword);
    public void HideGrid() => _terrainMaterial.DisableKeyword(GridKeyword);

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
                BrushSize * BrushSize + 0.5f,
                HexMetrics.WrapSize
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

        for (int r = 0, z = centerZ - BrushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + BrushSize; x++)
            {
                EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + BrushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - BrushSize; x <= centerX + r; x++)
            {
                EditCell(_hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell cell)
    {
        if (cell)
        {
            if (ApplyElevation)
            {
                cell.SetElevation(ActiveElevation);
            }
            if (ApplyWaterLevel)
            {
                cell.SetWaterLevel(ActiveWaterLevel);
            }
            if (ActiveTerrainType >= 0)
            {
                cell.SetTerrainType(ActiveTerrainType);
            }
            if (ApplySpecialIndex)
            {
                cell.SetStructureType(ActiveStructureType);
            }
            if (ApplyUrbanLevel)
            {
                cell.SetAnimalType(ActiveAnimalType);
            }
            if (ApplyFarmLevel)
            {
                cell.SetPlantType(ActivePlantType);
            }
            if (ApplyPlantLevel)
            {
                cell.SetMineralType(ActiveMineralType);
            }
            if (RiverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            if (RoadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }
            if (UnitsMode == OptionalToggle.No)
            {
                DestroyUnit(cell);
            }
            if (UnitsMode == OptionalToggle.Yes)
            {
                CreateUnit(cell);
            }
            if (WalledMode != OptionalToggle.Ignore)
            {
                cell.SetWalled(WalledMode == OptionalToggle.Yes);
            }

            if (_isDrag && cell.TryGetNeighbor(_dragDirection.Opposite(), out HexCell otherCell))
            {
                if (RiverMode == OptionalToggle.Yes)
                {
                    otherCell.SetOutgoingRiver(_dragDirection);
                }
                if (RoadMode == OptionalToggle.Yes)
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