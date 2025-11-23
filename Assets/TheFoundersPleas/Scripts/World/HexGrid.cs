using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TheFoundersPleas.Common.Pooling;

namespace TheFoundersPleas.World
{
    /// <summary>
    /// Component that represents an entire hexagon map.
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        [SerializeField] 
        private Text _cellLabelPrefab;

        [SerializeField]
        private HexGridChunk _chunkPrefab;

        [SerializeField]
        private HexUnit _unitPrefab;

        [SerializeField]
        private Texture2D _noiseSource;

        [SerializeField]
        private int _seed;

        /// <summary>
        /// Amount of cells in the X dimension.
        /// </summary>
        public int CellCountX { get; private set; }

        /// <summary>
        /// Amount of cells in the Z dimension.
        /// </summary>
        public int CellCountZ { get; private set; }

        /// <summary>
        /// Whether there currently exists a path that should be displayed.
        /// </summary>
        public bool HasPath => _currentPathExists;

        /// <summary>
        /// Whether east-west wrapping is enabled.
        /// </summary>
        public bool Wrapping { get; private set; }

        /// <summary>
        /// Bundled cell data.
        /// </summary>
        public HexCellData[] CellData { get; private set; }

        /// <summary>
        /// Separate cell positions.
        /// </summary>
        public Vector3[] CellPositions { get; private set; }

        public HexUnit[] CellUnits { get; private set; }

        /// <summary>
        /// Search data array usable for current map.
        /// </summary>
        public HexCellSearchData[] SearchData => _searchData;

        /// <summary>
        /// The <see cref="HexCellShaderData"/> container
        /// for cell visualization data.
        /// </summary>
        public HexCellShaderData ShaderData => _cellShaderData;

        private Transform[] _columns;
        private HexGridChunk[] _chunks;
        private HexCellSearchData[] _searchData;

        private int[] _cellVisibility;
        private HexGridChunk[] _cellGridChunks;
        private RectTransform[] _cellUIRects;

        private int _chunkCountX; 
        private int _chunkCountZ;
        private HexCellPriorityQueue _searchFrontier;
        private int _searchFrontierPhase;
        private int _currentPathFromIndex = -1;
        private int _currentPathToIndex = -1;
        private bool _currentPathExists;
        private int _currentCenterColumnIndex = -1;

        private HexCellShaderData _cellShaderData;

#pragma warning disable IDE0044 // Add readonly modifier
        private List<HexUnit> _units = new();
#pragma warning restore IDE0044 // Add readonly modifier

        public void Initialize()
        {
            HexMetrics.NoiseSource = _noiseSource;
            HexMetrics.InitializeHashGrid(_seed);
            HexUnit.unitPrefab = _unitPrefab;
            _cellShaderData = gameObject.AddComponent<HexCellShaderData>();
            _cellShaderData.Grid = this;
        }

        /// <summary>
        /// Create a new map.
        /// </summary>
        /// <param name="width">X size of the map.</param>
        /// <param name="height">Z size of the map.</param>
        /// <param name="wrapping">Whether the map wraps east-west.</param>
        /// <returns>Whether the map was successfully created. It fails when the X
        /// or Z size is not a multiple of the respective chunk size.</returns>
        public bool CreateMap(int width, int height, bool wrapping)
        {
            if (width <= 0 || width % HexMetrics.ChunkSizeX != 0 ||
                height <= 0 || height % HexMetrics.ChunkSizeZ != 0)
            {
                Debug.LogError("Unsupported map size.");
                return false;
            }

            ClearPath();
            ClearUnits();
            if (_columns != null)
            {
                for (int i = 0; i < _columns.Length; i++)
                {
                    Destroy(_columns[i].gameObject);
                }
            }

            CellCountX = width;
            CellCountZ = height;
            Wrapping = wrapping;
            _currentCenterColumnIndex = -1;
            HexMetrics.WrapSize = wrapping ? CellCountX : 0;
            _chunkCountX = CellCountX / HexMetrics.ChunkSizeX;
            _chunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;
            _cellShaderData.Initialize(CellCountX, CellCountZ);
            CreateChunks();
            CreateCells();
            return true;
        }

        /// <summary>
        /// Add a unit to the map.
        /// </summary>
        /// <param name="unit">Unit to add.</param>
        /// <param name="location">Cell in which to place the unit.</param>
        /// <param name="orientation">Orientation of the unit.</param>
        public void AddUnit(HexUnit unit, HexCell location, float orientation)
        {
            _units.Add(unit);
            unit.Grid = this;
            unit.Location = location;
            unit.Orientation = orientation;
        }

        /// <summary>
        /// Remove a unit from the map.
        /// </summary>
        /// <param name="unit">The unit to remove.</param>
        public void RemoveUnit(HexUnit unit)
        {
            _units.Remove(unit);
            unit.Die();
        }

        /// <summary>
        /// Make a game object a child of a map column.
        /// </summary>
        /// <param name="child"><see cref="Transform"/>
        /// of the child game object.</param>
        /// <param name="columnIndex">Index of the parent column.</param>
        public void MakeChildOfColumn(Transform child, int columnIndex) =>
            child.SetParent(_columns[columnIndex], false);

        private void CreateChunks()
        {
            _columns = new Transform[_chunkCountX];
            for (int x = 0; x < _chunkCountX; x++)
            {
                _columns[x] = new GameObject("Column").transform;
                _columns[x].SetParent(transform, false);
            }

            _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];
            for (int z = 0, i = 0; z < _chunkCountZ; z++)
            {
                for (int x = 0; x < _chunkCountX; x++)
                {
                    HexGridChunk chunk = _chunks[i++] = Instantiate(_chunkPrefab);
                    chunk.transform.SetParent(_columns[x], false);
                    chunk.Grid = this;
                }
            }
        }

        private void CreateCells()
        {
            CellData = new HexCellData[CellCountZ * CellCountX];
            CellPositions = new Vector3[CellData.Length];
            _cellUIRects = new RectTransform[CellData.Length];
            _cellGridChunks = new HexGridChunk[CellData.Length];
            CellUnits = new HexUnit[CellData.Length];
            _searchData = new HexCellSearchData[CellData.Length];
            _cellVisibility = new int[CellData.Length];

            for (int z = 0, i = 0; z < CellCountZ; z++)
            {
                for (int x = 0; x < CellCountX; x++)
                {
                    CreateCell(x, z, i++);
                }
            }
        }

        private void ClearUnits()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                _units[i].Die();
            }
            _units.Clear();
        }

        private void OnEnable()
        {
            if (!HexMetrics.NoiseSource)
            {
                HexMetrics.NoiseSource = _noiseSource;
                HexMetrics.InitializeHashGrid(_seed);
                HexUnit.unitPrefab = _unitPrefab;
                HexMetrics.WrapSize = Wrapping ? CellCountX : 0;
                ResetVisibility();
            }
        }

        /// <summary>
        /// Get a cell given a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray"><see cref="Ray"/> used to perform a raycast.</param>
        /// <param name="stickyCell">Cell to stick to if close enough.</param>
        /// <returns>The hit cell, if any.</returns>
        public HexCell GetCell(Ray ray, HexCell stickyCell = default)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return GetCell(hit.point, stickyCell);
            }
            return default;
        }

        /// <summary>
        /// Get the cell that contains a position.
        /// </summary>
        /// <param name="position">Position to check.</param>
        /// <param name="stickyCell">Cell to stick to if close enough.</param>
        /// <returns>The cell containing the position, if it exists.</returns>
        public HexCell GetCell(Vector3 position, HexCell stickyCell = default)
        {
            position = transform.InverseTransformPoint(position);
            if (stickyCell)
            {
                Vector3 v = position - stickyCell.Position;
                if (
                    v.x * v.x + v.z * v.z <
                    HexMetrics.StickyRadius * HexMetrics.StickyRadius)
                {
                    return stickyCell;
                }
            }
            HexCoordinates coordinates = HexCoordinates.FromPosition(position);
            return GetCell(coordinates);
        }

        /// <summary>
        /// Get the cell with specific <see cref="HexCoordinates"/>.
        /// </summary>
        /// <param name="coordinates"><see cref="HexCoordinates"/>
        /// of the cell.</param>
        /// <returns>The cell with the given coordinates, if it exists.</returns>
        public HexCell GetCell(HexCoordinates coordinates)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= CellCountZ || x < 0 || x >= CellCountX)
            {
                return default;
            }
            return new HexCell(x + z * CellCountX, this);
        }

        /// <summary>
        /// Try to get the cell with specific <see cref="HexCoordinates"/>.
        /// </summary>
        /// <param name="coordinates"><see cref="HexCoordinates"/>
        /// of the cell.</param>
        /// <param name="cell">The cell, if it exists.</param>
        /// <returns>Whether the cell exists.</returns>
        public bool TryGetCell(HexCoordinates coordinates, out HexCell cell)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= CellCountZ || x < 0 || x >= CellCountX)
            {
                cell = default;
                return false;
            }
            cell = new HexCell(x + z * CellCountX, this);
            return true;
        }

        /// <summary>
        /// Try to get the cell index for specific <see cref="HexCoordinates"/>.
        /// </summary>
        /// <param name="coordinates"><see cref="HexCoordinates"/>
        /// of the cell.</param>
        /// <param name="cell">The cell index, if it exists, otherwise -1.</param>
        /// <returns>Whether the cell index exists.</returns>
        public bool TryGetCellIndex(HexCoordinates coordinates, out int cellIndex)
        {
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;
            if (z < 0 || z >= CellCountZ || x < 0 || x >= CellCountX)
            {
                cellIndex = -1;
                return false;
            }
            cellIndex = x + z * CellCountX;
            return true;
        }

        /// <summary>
        /// Get the cell index with specific offset coordinates.
        /// </summary>
        /// <param name="xOffset">X array offset coordinate.</param>
        /// <param name="zOffset">Z array offset coordinate.</param>
        /// <returns>Cell index.</returns>
        public int GetCellIndex(int xOffset, int zOffset) =>
            xOffset + zOffset * CellCountX;

        /// <summary>
        /// Get the cell with a specific index.
        /// </summary>
        /// <param name="cellIndex">Cell index, which should be valid.</param>
        /// <returns>The indicated cell.</returns>
        public HexCell GetCell(int cellIndex) => new(cellIndex, this);

        /// <summary>
        /// Check whether a cell is visibile.
        /// </summary>
        /// <param name="cellIndex">Index of the cell to check.</param>
        /// <returns>Whether the cell is visible.</returns>
        public bool IsCellVisible(int cellIndex) => _cellVisibility[cellIndex] > 0;

        /// <summary>
        /// Control whether the map UI should be visible or hidden.
        /// </summary>
        /// <param name="visible">Whether the UI should be visibile.</param>
        public void ShowUI(bool visible)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i].ShowUI(visible);
            }
        }

        private void CreateCell(int x, int z, int i)
        {
            Vector3 position;
            position.x = (x + z * 0.5f - z / 2) * HexMetrics.InnerDiameter;
            position.y = 0f;
            position.z = z * (HexMetrics.OuterRadius * 1.5f);

            var cell = new HexCell(i, this);
            CellPositions[i] = position;
            CellData[i].Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

            bool explorable = Wrapping ?
                z > 0 && z < CellCountZ - 1 :
                x > 0 && z > 0 && x < CellCountX - 1 && z < CellCountZ - 1;
            cell.Flags = explorable ?
                cell.Flags.With(HexFlags.Explorable) :
                cell.Flags.Without(HexFlags.Explorable);

            Text label = Instantiate(_cellLabelPrefab);
            label.rectTransform.anchoredPosition =
                new Vector2(position.x, position.z);
            RectTransform rect = _cellUIRects[i] = label.rectTransform;

            cell.Values = cell.Values.WithElevation(0);
            RefreshCellPosition(i);

            int chunkX = x / HexMetrics.ChunkSizeX;
            int chunkZ = z / HexMetrics.ChunkSizeZ;
            HexGridChunk chunk = _chunks[chunkX + chunkZ * _chunkCountX];

            int localX = x - chunkX * HexMetrics.ChunkSizeX;
            int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
            _cellGridChunks[i] = chunk;
            chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, i, rect);
        }

        /// <summary>
        /// Refresh the chunk the cell is part of.
        /// </summary>
        /// <param name="cellIndex">Cell index.</param>
        public void RefreshCell(int cellIndex) =>
            _cellGridChunks[cellIndex].Refresh();

        /// <summary>
        /// Refresh the cell, all its neighbors, and its unit.
        /// </summary>
        /// <param name="cellIndex">Cell index.</param>
        public void RefreshCellWithDependents(int cellIndex)
        {
            HexGridChunk chunk = _cellGridChunks[cellIndex];
            chunk.Refresh();
            HexCoordinates coordinates = CellData[cellIndex].Coordinates;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                if (TryGetCellIndex(coordinates.Step(d), out int neighborIndex))
                {
                    HexGridChunk neighborChunk = _cellGridChunks[neighborIndex];
                    if (chunk != neighborChunk)
                    {
                        neighborChunk.Refresh();
                    }
                }
            }
            HexUnit unit = CellUnits[cellIndex];
            if (unit)
            {
                unit.ValidateLocation();
            }
        }

        /// <summary>
        /// Refresh the world position of a cell.
        /// </summary>
        /// <param name="cellIndex">Cell index.</param>
        public void RefreshCellPosition(int cellIndex)
        {
            Vector3 position = CellPositions[cellIndex];
            position.y = CellData[cellIndex].Elevation * HexMetrics.ElevationStep;
            position.y +=
                (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.ElevationPerturbStrength;
            CellPositions[cellIndex] = position;

            RectTransform rectTransform = _cellUIRects[cellIndex];
            Vector3 uiPosition = rectTransform.localPosition;
            uiPosition.z = -position.y;
            rectTransform.localPosition = uiPosition;
        }

        /// <summary>
        /// Refresh all cells, to be done after generating a map.
        /// </summary>
        public void RefreshAllCells()
        {
            for (int i = 0; i < CellData.Length; i++)
            {
                SearchData[i].searchPhase = 0;
                RefreshCellPosition(i);
                ShaderData.RefreshTerrain(i);
                ShaderData.RefreshVisibility(i);
            }
        }

        /// <summary>
        /// Save the map.
        /// </summary>
        /// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
        public void Save(BinaryWriter writer)
        {
            writer.Write(CellCountX);
            writer.Write(CellCountZ);
            writer.Write(Wrapping);

            for (int i = 0; i < CellData.Length; i++)
            {
                HexCellData data = CellData[i];
                data.Values.Save(writer);
                data.Flags.Save(writer);
            }

            writer.Write(_units.Count);
            for (int i = 0; i < _units.Count; i++)
            {
                _units[i].Save(writer);
            }
        }

        /// <summary>
        /// Load the map.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> to use.</param>
        /// <param name="header">Header version.</param>
        public void Load(BinaryReader reader, int header)
        {
            ClearPath();
            ClearUnits();
            int x = 20, z = 15;
            if (header >= 1)
            {
                x = reader.ReadInt32();
                z = reader.ReadInt32();
            }
            bool wrapping = header >= 5 && reader.ReadBoolean();
            if (x != CellCountX || z != CellCountZ || Wrapping != wrapping)
            {
                if (!CreateMap(x, z, wrapping))
                {
                    return;
                }
            }

            bool originalImmediateMode = _cellShaderData.ImmediateMode;
            _cellShaderData.ImmediateMode = true;

            for (int i = 0; i < CellData.Length; i++)
            {
                HexCellData data = CellData[i];
                data.Values = HexValues.Load(reader, header);
                data.Flags = data.Flags.Load(reader, header);
                CellData[i] = data;
                RefreshCellPosition(i);
                ShaderData.RefreshTerrain(i);
                ShaderData.RefreshVisibility(i);
            }
            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i].Refresh();
            }

            if (header >= 2)
            {
                int unitCount = reader.ReadInt32();
                for (int i = 0; i < unitCount; i++)
                {
                    HexUnit.Load(reader, this);
                }
            }

            _cellShaderData.ImmediateMode = originalImmediateMode;
        }

        /// <summary>
        /// Get a list of cell indices representing the currently visible path.
        /// </summary>
        /// <returns>The current path list, if a visible path exists.</returns>
        public List<int> GetPath()
        {
            if (!_currentPathExists)
            {
                return null;
            }
            List<int> path = ListPool<int>.Get();
            for (int i = _currentPathToIndex;
                i != _currentPathFromIndex;
                i = _searchData[i].pathFrom)
            {
                path.Add(i);
            }
            path.Add(_currentPathFromIndex);
            path.Reverse();
            return path;
        }

        private void SetLabel(int cellIndex, string text) =>
            _cellUIRects[cellIndex].GetComponent<Text>().text = text;

        private void DisableHighlight(int cellIndex) =>
            _cellUIRects[cellIndex].GetChild(0).GetComponent<Image>().enabled =
                false;

        private void EnableHighlight(int cellIndex, Color color)
        {
            Image highlight =
                _cellUIRects[cellIndex].GetChild(0).GetComponent<Image>();
            highlight.color = color;
            highlight.enabled = true;
        }

        /// <summary>
        /// Clear the current path.
        /// </summary>
        public void ClearPath()
        {
            if (_currentPathExists)
            {
                int currentIndex = _currentPathToIndex;
                while (currentIndex != _currentPathFromIndex)
                {
                    SetLabel(currentIndex, null);
                    DisableHighlight(currentIndex);
                    currentIndex = _searchData[currentIndex].pathFrom;
                }
                DisableHighlight(currentIndex);
                _currentPathExists = false;
            }
            else if (_currentPathFromIndex >= 0)
            {
                DisableHighlight(_currentPathFromIndex);
                DisableHighlight(_currentPathToIndex);
            }
            _currentPathFromIndex = _currentPathToIndex = -1;
        }

        private void ShowPath(int speed)
        {
            if (_currentPathExists)
            {
                int currentIndex = _currentPathToIndex;
                while (currentIndex != _currentPathFromIndex)
                {
                    int turn = (_searchData[currentIndex].distance - 1) / speed;
                    SetLabel(currentIndex, turn.ToString());
                    EnableHighlight(currentIndex, Color.white);
                    currentIndex = _searchData[currentIndex].pathFrom;
                }
            }
            EnableHighlight(_currentPathFromIndex, Color.blue);
            EnableHighlight(_currentPathToIndex, Color.red);
        }

        /// <summary>
        /// Try to find a path.
        /// </summary>
        /// <param name="fromCell">Cell to start the search from.</param>
        /// <param name="toCell">Cell to find a path towards.</param>
        /// <param name="unit">Unit for which the path is.</param>
        public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
        {
            ClearPath();
            _currentPathFromIndex = fromCell.Index;
            _currentPathToIndex = toCell.Index;
            _currentPathExists = Search(fromCell, toCell, unit);
            ShowPath(unit.Speed);
        }

        private bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
        {
            int speed = unit.Speed;
            _searchFrontierPhase += 2;
            _searchFrontier ??= new HexCellPriorityQueue(this);
            _searchFrontier.Clear();

            _searchData[fromCell.Index] = new HexCellSearchData
            {
                searchPhase = _searchFrontierPhase
            };
            _searchFrontier.Enqueue(fromCell.Index);
            while (_searchFrontier.TryDequeue(out int currentIndex))
            {
                var current = new HexCell(currentIndex, this);
                int currentDistance = _searchData[currentIndex].distance;
                _searchData[currentIndex].searchPhase += 1;

                if (current == toCell)
                {
                    return true;
                }

                int currentTurn = (currentDistance - 1) / speed;

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    if (!current.TryGetNeighbor(d, out HexCell neighbor))
                    {
                        continue;
                    }
                    HexCellSearchData neighborData = _searchData[neighbor.Index];
                    if (neighborData.searchPhase > _searchFrontierPhase ||
                        !unit.IsValidDestination(neighbor))
                    {
                        continue;
                    }
                    int moveCost = unit.GetMoveCost(current, neighbor, d);
                    if (moveCost < 0)
                    {
                        continue;
                    }

                    int distance = currentDistance + moveCost;
                    int turn = (distance - 1) / speed;
                    if (turn > currentTurn)
                    {
                        distance = turn * speed + moveCost;
                    }

                    if (neighborData.searchPhase < _searchFrontierPhase)
                    {
                        _searchData[neighbor.Index] = new HexCellSearchData
                        {
                            searchPhase = _searchFrontierPhase,
                            distance = distance,
                            pathFrom = currentIndex,
                            heuristic = neighbor.Coordinates.DistanceTo(
                                toCell.Coordinates)
                        };
                        _searchFrontier.Enqueue(neighbor.Index);
                    }
                    else if (distance < neighborData.distance)
                    {
                        _searchData[neighbor.Index].distance = distance;
                        _searchData[neighbor.Index].pathFrom = currentIndex;
                        _searchFrontier.Change(
                            neighbor.Index, neighborData.SearchPriority);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Increase the visibility of all cells relative to a view cell.
        /// </summary>
        /// <param name="fromCell">Cell from which to start viewing.</param>
        /// <param name="range">Visibility range.</param>
        public void IncreaseVisibility(HexCell fromCell, int range)
        {
            List<HexCell> cells = GetVisibleCells(fromCell, range);
            for (int i = 0; i < cells.Count; i++)
            {
                int cellIndex = cells[i].Index;
                if (++_cellVisibility[cellIndex] == 1)
                {
                    HexCell c = cells[i];
                    c.Flags = c.Flags.With(HexFlags.Explored);
                    _cellShaderData.RefreshVisibility(cellIndex);
                }
            }
            ListPool<HexCell>.Add(cells);
        }

        /// <summary>
        /// Decrease the visibility of all cells relative to a view cell.
        /// </summary>
        /// <param name="fromCell">Cell from which to stop viewing.</param>
        /// <param name="range">Visibility range.</param>
        public void DecreaseVisibility(HexCell fromCell, int range)
        {
            List<HexCell> cells = GetVisibleCells(fromCell, range);
            for (int i = 0; i < cells.Count; i++)
            {
                int cellIndex = cells[i].Index;
                if (--_cellVisibility[cellIndex] == 0)
                {
                    _cellShaderData.RefreshVisibility(cellIndex);
                }
            }
            ListPool<HexCell>.Add(cells);
        }

        /// <summary>
        /// Reset visibility of the entire map, viewing from all units.
        /// </summary>
        public void ResetVisibility()
        {
            for (int i = 0; i < _cellVisibility.Length; i++)
            {
                if (_cellVisibility[i] > 0)
                {
                    _cellVisibility[i] = 0;
                    _cellShaderData.RefreshVisibility(i);
                }
            }
            for (int i = 0; i < _units.Count; i++)
            {
                HexUnit unit = _units[i];
                IncreaseVisibility(unit.Location, unit.VisionRange);
            }
        }

        private List<HexCell> GetVisibleCells(HexCell fromCell, int range)
        {
            List<HexCell> visibleCells = ListPool<HexCell>.Get();

            _searchFrontierPhase += 2;
            _searchFrontier ??= new HexCellPriorityQueue(this);
            _searchFrontier.Clear();

            range += fromCell.Values.ViewElevation;
            _searchData[fromCell.Index] = new HexCellSearchData
            {
                searchPhase = _searchFrontierPhase,
                pathFrom = _searchData[fromCell.Index].pathFrom
            };
            _searchFrontier.Enqueue(fromCell.Index);
            HexCoordinates fromCoordinates = fromCell.Coordinates;
            while (_searchFrontier.TryDequeue(out int currentIndex))
            {
                var current = new HexCell(currentIndex, this);
                _searchData[currentIndex].searchPhase += 1;
                visibleCells.Add(current);

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    if (!current.TryGetNeighbor(d, out HexCell neighbor))
                    {
                        continue;
                    }
                    HexCellSearchData currentData = _searchData[neighbor.Index];
                    if (currentData.searchPhase > _searchFrontierPhase ||
                        neighbor.Flags.HasNone(HexFlags.Explorable))
                    {
                        continue;
                    }

                    int distance = _searchData[currentIndex].distance + 1;
                    if (distance + neighbor.Values.ViewElevation > range ||
                        distance > fromCoordinates.DistanceTo(neighbor.Coordinates))
                    {
                        continue;
                    }

                    if (currentData.searchPhase < _searchFrontierPhase)
                    {
                        _searchData[neighbor.Index] = new HexCellSearchData
                        {
                            searchPhase = _searchFrontierPhase,
                            distance = distance,
                            pathFrom = currentData.pathFrom
                        };
                        _searchFrontier.Enqueue(neighbor.Index);
                    }
                    else if (distance < _searchData[neighbor.Index].distance)
                    {
                        _searchData[neighbor.Index].distance = distance;
                        _searchFrontier.Change(
                            neighbor.Index, currentData.SearchPriority);
                    }
                }
            }
            return visibleCells;
        }

        /// <summary>
        /// Center the map given an X position, to facilitate east-west wrapping.
        /// </summary>
        /// <param name="xPosition">X position.</param>
        public void CenterMap(float xPosition)
        {
            int centerColumnIndex = (int)
                (xPosition / (HexMetrics.InnerDiameter * HexMetrics.ChunkSizeX));

            if (centerColumnIndex == _currentCenterColumnIndex)
            {
                return;
            }
            _currentCenterColumnIndex = centerColumnIndex;

            int minColumnIndex = centerColumnIndex - _chunkCountX / 2;
            int maxColumnIndex = centerColumnIndex + _chunkCountX / 2;

            Vector3 position;
            position.y = position.z = 0f;
            for (int i = 0; i < _columns.Length; i++)
            {
                if (i < minColumnIndex)
                {
                    position.x = _chunkCountX *
                        (HexMetrics.InnerDiameter * HexMetrics.ChunkSizeX);
                }
                else if (i > maxColumnIndex)
                {
                    position.x = _chunkCountX *
                        -(HexMetrics.InnerDiameter * HexMetrics.ChunkSizeX);
                }
                else
                {
                    position.x = 0f;
                }
                _columns[i].localPosition = position;
            }
        }
    }
}