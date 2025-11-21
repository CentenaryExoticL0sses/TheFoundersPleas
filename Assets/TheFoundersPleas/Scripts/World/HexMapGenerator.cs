using System.Collections.Generic;
using TheFoundersPleas.Common.Pooling;
using TheFoundersPleas.Core.Enums;
using UnityEngine;

namespace TheFoundersPleas.World
{
    /// <summary>
    /// Component that generates hex maps.
    /// </summary>
    public class HexMapGenerator : MonoBehaviour
    {
        [SerializeField] private HexGrid _grid;

        [SerializeField] private bool _useFixedSeed;

        [SerializeField] private int _seed;

        [SerializeField, Range(0f, 0.5f)] private float _jitterProbability = 0.25f;

        [SerializeField, Range(20, 200)] private int _chunkSizeMin = 30;

        [SerializeField, Range(20, 200)] private int _chunkSizeMax = 100;

        [SerializeField, Range(0f, 1f)] private float _highRiseProbability = 0.25f;

        [SerializeField, Range(0f, 0.4f)] private float _sinkProbability = 0.2f;

        [SerializeField, Range(5, 95)] private int _landPercentage = 50;

        [SerializeField, Range(1, 5)] private int _waterLevel = 3;

        [SerializeField, Range(-4, 0)] private int _elevationMinimum = -2;

        [SerializeField, Range(6, 10)] private int _elevationMaximum = 8;

        [SerializeField, Range(0, 10)] private int _mapBorderX = 5;

        [SerializeField, Range(0, 10)] private int _mapBorderZ = 5;

        [SerializeField, Range(0, 10)] private int _regionBorder = 5;

        [SerializeField, Range(1, 4)] private int _regionCount = 1;

        [SerializeField, Range(0, 100)] private int _erosionPercentage = 50;

        [SerializeField, Range(0f, 1f)] private float _startingMoisture = 0.1f;

        [SerializeField, Range(0f, 1f)] private float _evaporationFactor = 0.5f;

        [SerializeField, Range(0f, 1f)] private float _precipitationFactor = 0.25f;

        [SerializeField, Range(0f, 1f)] private float _runoffFactor = 0.25f;

        [SerializeField, Range(0f, 1f)] private float _seepageFactor = 0.125f;

        [SerializeField] private HexDirection _windDirection = HexDirection.NW;

        [SerializeField, Range(1f, 10f)] private float _windStrength = 4f;

        [SerializeField, Range(0, 20)] private int _riverPercentage = 10;

        [SerializeField, Range(0f, 1f)] private float _extraLakeProbability = 0.25f;

        [SerializeField, Range(0f, 1f)] private float _lowTemperature = 0f;

        [SerializeField, Range(0f, 1f)] private float _highTemperature = 1f;

        [SerializeField] private HemisphereMode _hemisphere;

        [SerializeField, Range(0f, 1f)] private float _temperatureJitter = 0.1f;

        private HexCellPriorityQueue _searchFrontier;
        private int _searchFrontierPhase;
        private int _cellCount; 
        private int _landCells;
        private int _temperatureJitterChannel;

        private List<MapRegion> _regions;

        private List<ClimateData> _climate = new();
        private List<ClimateData> _nextClimate = new();
        private List<HexDirection> _flowDirections = new();

        private static readonly float[] _temperatureBands = { 0.1f, 0.3f, 0.6f };
        private static readonly float[] _moistureBands = { 0.12f, 0.28f, 0.85f };

        private static readonly Biome[] _biomes = {
            new(0, 0), new(4, 0), new(4, 0), new(4, 0),
            new(0, 0), new(2, 0), new(2, 1), new(2, 2),
            new(0, 0), new(1, 0), new(1, 1), new(1, 2),
            new(0, 0), new(1, 1), new(1, 2), new(1, 3)
        };

        /// <summary>
        /// Generate a random hex map.
        /// </summary>
        /// <param name="x">X size of the map.</param>
        /// <param name="z">Z size of the map.</param>
        /// <param name="wrapping">Whether east-west wrapping is enabled.</param>
        public void GenerateMap(int x, int z, bool wrapping)
        {
            Random.State originalRandomState = Random.state;
            if (!_useFixedSeed)
            {
                _seed = Random.Range(0, int.MaxValue);
                _seed ^= (int)System.DateTime.Now.Ticks;
                _seed ^= (int)Time.unscaledTime;
                _seed &= int.MaxValue;
            }
            Random.InitState(_seed);

            _cellCount = x * z;
            _grid.CreateMap(x, z, wrapping);
            _searchFrontier ??= new HexCellPriorityQueue(_grid);
            for (int i = 0; i < _cellCount; i++)
            {
                _grid.CellData[i].Values = _grid.CellData[i].Values.WithWaterLevel(
                    _waterLevel);
            }
            CreateRegions();
            CreateLand();
            ErodeLand();
            CreateClimate();
            CreateRivers();
            SetTerrainType();
            _grid.RefreshAllCells();

            Random.state = originalRandomState;
        }

        private void CreateRegions()
        {
            if (_regions == null)
            {
                _regions = new();
            }
            else
            {
                _regions.Clear();
            }

            int borderX = _grid.Wrapping ? _regionBorder : _mapBorderX;
            MapRegion region;
            switch (_regionCount)
            {
                default:
                    if (_grid.Wrapping)
                    {
                        borderX = 0;
                    }
                    region.XMin = borderX;
                    region.XMax = _grid.CellCountX - borderX;
                    region.ZMin = _mapBorderZ;
                    region.ZMax = _grid.CellCountZ - _mapBorderZ;
                    _regions.Add(region);
                    break;
                case 2:
                    if (Random.value < 0.5f)
                    {
                        region.XMin = borderX;
                        region.XMax = _grid.CellCountX / 2 - _regionBorder;
                        region.ZMin = _mapBorderZ;
                        region.ZMax = _grid.CellCountZ - _mapBorderZ;
                        _regions.Add(region);
                        region.XMin = _grid.CellCountX / 2 + _regionBorder;
                        region.XMax = _grid.CellCountX - borderX;
                        _regions.Add(region);
                    }
                    else
                    {
                        if (_grid.Wrapping)
                        {
                            borderX = 0;
                        }
                        region.XMin = borderX;
                        region.XMax = _grid.CellCountX - borderX;
                        region.ZMin = _mapBorderZ;
                        region.ZMax = _grid.CellCountZ / 2 - _regionBorder;
                        _regions.Add(region);
                        region.ZMin = _grid.CellCountZ / 2 + _regionBorder;
                        region.ZMax = _grid.CellCountZ - _mapBorderZ;
                        _regions.Add(region);
                    }
                    break;
                case 3:
                    region.XMin = borderX;
                    region.XMax = _grid.CellCountX / 3 - _regionBorder;
                    region.ZMin = _mapBorderZ;
                    region.ZMax = _grid.CellCountZ - _mapBorderZ;
                    _regions.Add(region);
                    region.XMin = _grid.CellCountX / 3 + _regionBorder;
                    region.XMax = _grid.CellCountX * 2 / 3 - _regionBorder;
                    _regions.Add(region);
                    region.XMin = _grid.CellCountX * 2 / 3 + _regionBorder;
                    region.XMax = _grid.CellCountX - borderX;
                    _regions.Add(region);
                    break;
                case 4:
                    region.XMin = borderX;
                    region.XMax = _grid.CellCountX / 2 - _regionBorder;
                    region.ZMin = _mapBorderZ;
                    region.ZMax = _grid.CellCountZ / 2 - _regionBorder;
                    _regions.Add(region);
                    region.XMin = _grid.CellCountX / 2 + _regionBorder;
                    region.XMax = _grid.CellCountX - borderX;
                    _regions.Add(region);
                    region.ZMin = _grid.CellCountZ / 2 + _regionBorder;
                    region.ZMax = _grid.CellCountZ - _mapBorderZ;
                    _regions.Add(region);
                    region.XMin = borderX;
                    region.XMax = _grid.CellCountX / 2 - _regionBorder;
                    _regions.Add(region);
                    break;
            }
        }

        private void CreateLand()
        {
            int landBudget = Mathf.RoundToInt(_cellCount * _landPercentage * 0.01f);
            _landCells = landBudget;
            for (int guard = 0; guard < 10000; guard++)
            {
                bool sink = Random.value < _sinkProbability;
                for (int i = 0; i < _regions.Count; i++)
                {
                    MapRegion region = _regions[i];
                    int chunkSize = Random.Range(_chunkSizeMin, _chunkSizeMax - 1);
                    if (sink)
                    {
                        landBudget = SinkTerrain(chunkSize, landBudget, region);
                    }
                    else
                    {
                        landBudget = RaiseTerrain(chunkSize, landBudget, region);
                        if (landBudget == 0)
                        {
                            return;
                        }
                    }
                }
            }
            if (landBudget > 0)
            {
                Debug.LogWarning(
                    "Failed to use up " + landBudget + " land budget.");
                _landCells -= landBudget;
            }
        }

        private int RaiseTerrain(int chunkSize, int budget, MapRegion region)
        {
            _searchFrontierPhase += 1;
            int firstCellIndex = GetRandomCellIndex(region);
            _grid.SearchData[firstCellIndex] = new HexCellSearchData
            {
                searchPhase = _searchFrontierPhase
            };
            _searchFrontier.Enqueue(firstCellIndex);
            HexCoordinates center = _grid.CellData[firstCellIndex].Coordinates;

            int rise = Random.value < _highRiseProbability ? 2 : 1;
            int size = 0;
            while (size < chunkSize && _searchFrontier.TryDequeue(out int index))
            {
                HexCellData current = _grid.CellData[index];
                int originalElevation = current.Elevation;
                int newElevation = originalElevation + rise;
                if (newElevation > _elevationMaximum)
                {
                    continue;
                }
                _grid.CellData[index].Values =
                    current.Values.WithElevation(newElevation);
                if (originalElevation < _waterLevel &&
                    newElevation >= _waterLevel && --budget == 0
                )
                {
                    break;
                }
                size += 1;

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    if (_grid.TryGetCellIndex(
                        current.Coordinates.Step(d), out int neighborIndex) &&
                        _grid.SearchData[neighborIndex].searchPhase <
                            _searchFrontierPhase)
                    {
                        _grid.SearchData[neighborIndex] = new HexCellSearchData
                        {
                            searchPhase = _searchFrontierPhase,
                            distance = _grid.CellData[neighborIndex].Coordinates.
                                DistanceTo(center),
                            heuristic = Random.value < _jitterProbability ? 1 : 0
                        };
                        _searchFrontier.Enqueue(neighborIndex);
                    }
                }
            }
            _searchFrontier.Clear();
            return budget;
        }

        private int SinkTerrain(int chunkSize, int budget, MapRegion region)
        {
            _searchFrontierPhase += 1;
            int firstCellIndex = GetRandomCellIndex(region);
            _grid.SearchData[firstCellIndex] = new HexCellSearchData
            {
                searchPhase = _searchFrontierPhase
            };
            _searchFrontier.Enqueue(firstCellIndex);
            HexCoordinates center = _grid.CellData[firstCellIndex].Coordinates;

            int sink = Random.value < _highRiseProbability ? 2 : 1;
            int size = 0;
            while (size < chunkSize && _searchFrontier.TryDequeue(out int index))
            {
                HexCellData current = _grid.CellData[index];
                int originalElevation = current.Elevation;
                int newElevation = current.Elevation - sink;
                if (newElevation < _elevationMinimum)
                {
                    continue;
                }
                _grid.CellData[index].Values =
                    current.Values.WithElevation(newElevation);
                if (originalElevation >= _waterLevel &&
                    newElevation < _waterLevel
                )
                {
                    budget += 1;
                }
                size += 1;

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    if (_grid.TryGetCellIndex(
                        current.Coordinates.Step(d), out int neighborIndex) &&
                        _grid.SearchData[neighborIndex].searchPhase <
                            _searchFrontierPhase)
                    {
                        _grid.SearchData[neighborIndex] = new HexCellSearchData
                        {
                            searchPhase = _searchFrontierPhase,
                            distance = _grid.CellData[neighborIndex].Coordinates.
                                DistanceTo(center),
                            heuristic = Random.value < _jitterProbability ? 1 : 0
                        };
                        _searchFrontier.Enqueue(neighborIndex);
                    }
                }
            }
            _searchFrontier.Clear();
            return budget;
        }

        private void ErodeLand()
        {
            List<int> erodibleIndices = ListPool<int>.Get();
            for (int i = 0; i < _cellCount; i++)
            {
                if (IsErodible(i, _grid.CellData[i].Elevation))
                {
                    erodibleIndices.Add(i);
                }
            }

            int targetErodibleCount =
                (int)(erodibleIndices.Count * (100 - _erosionPercentage) * 0.01f);

            while (erodibleIndices.Count > targetErodibleCount)
            {
                int index = Random.Range(0, erodibleIndices.Count);
                int cellIndex = erodibleIndices[index];
                HexCellData cell = _grid.CellData[cellIndex];
                int targetCellIndex = GetErosionTarget(cellIndex, cell.Elevation);

                _grid.CellData[cellIndex].Values = cell.Values =
                    cell.Values.WithElevation(cell.Elevation - 1);

                HexCellData targetCell = _grid.CellData[targetCellIndex];
                _grid.CellData[targetCellIndex].Values = targetCell.Values =
                    targetCell.Values.WithElevation(targetCell.Elevation + 1);

                if (!IsErodible(cellIndex, cell.Elevation))
                {
                    int lastIndex = erodibleIndices.Count - 1;
                    erodibleIndices[index] = erodibleIndices[lastIndex];
                    erodibleIndices.RemoveAt(lastIndex);
                }

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    if (_grid.TryGetCellIndex(
                        cell.Coordinates.Step(d), out int neighborIndex) &&
                        _grid.CellData[neighborIndex].Elevation ==
                            cell.Elevation + 2 &&
                        !erodibleIndices.Contains(neighborIndex))
                    {
                        erodibleIndices.Add(neighborIndex);
                    }
                }

                if (IsErodible(targetCellIndex, targetCell.Elevation) &&
                    !erodibleIndices.Contains(targetCellIndex))
                {
                    erodibleIndices.Add(targetCellIndex);
                }

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    if (_grid.TryGetCellIndex(
                        targetCell.Coordinates.Step(d), out int neighborIndex) &&
                        neighborIndex != cellIndex &&
                        _grid.CellData[neighborIndex].Elevation ==
                            targetCell.Elevation + 1 &&
                        !IsErodible(
                            neighborIndex, _grid.CellData[neighborIndex].Elevation))
                    {
                        erodibleIndices.Remove(neighborIndex);
                    }
                }
            }

            ListPool<int>.Add(erodibleIndices);
        }

        private bool IsErodible(int cellIndex, int cellElevation)
        {
            int erodibleElevation = cellElevation - 2;
            HexCoordinates coordinates = _grid.CellData[cellIndex].Coordinates;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                if (_grid.TryGetCellIndex(
                    coordinates.Step(d), out int neighborIndex) &&
                    _grid.CellData[neighborIndex].Elevation <= erodibleElevation)
                {
                    return true;
                }
            }
            return false;
        }

        private int GetErosionTarget(int cellIndex, int cellElevation)
        {
            List<int> candidates = ListPool<int>.Get();
            int erodibleElevation = cellElevation - 2;
            HexCoordinates coordinates = _grid.CellData[cellIndex].Coordinates;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                if (_grid.TryGetCellIndex(
                    coordinates.Step(d), out int neighborIndex) &&
                    _grid.CellData[neighborIndex].Elevation <= erodibleElevation
                )
                {
                    candidates.Add(neighborIndex);
                }
            }
            int target = candidates[Random.Range(0, candidates.Count)];
            ListPool<int>.Add(candidates);
            return target;
        }

        private void CreateClimate()
        {
            _climate.Clear();
            _nextClimate.Clear();
            var initialData = new ClimateData
            {
                Moisture = _startingMoisture
            };
            var clearData = new ClimateData();
            for (int i = 0; i < _cellCount; i++)
            {
                _climate.Add(initialData);
                _nextClimate.Add(clearData);
            }

            for (int cycle = 0; cycle < 40; cycle++)
            {
                for (int i = 0; i < _cellCount; i++)
                {
                    EvolveClimate(i);
                }
                (_nextClimate, _climate) = (_climate, _nextClimate);
            }
        }

        private void EvolveClimate(int cellIndex)
        {
            HexCellData cell = _grid.CellData[cellIndex];
            ClimateData cellClimate = _climate[cellIndex];

            if (cell.IsUnderwater)
            {
                cellClimate.Moisture = 1f;
                cellClimate.Clouds += _evaporationFactor;
            }
            else
            {
                float evaporation = cellClimate.Moisture * _evaporationFactor;
                cellClimate.Moisture -= evaporation;
                cellClimate.Clouds += evaporation;
            }

            float precipitation = cellClimate.Clouds * _precipitationFactor;
            cellClimate.Clouds -= precipitation;
            cellClimate.Moisture += precipitation;

            float cloudMaximum = 1f - cell.ViewElevation / (_elevationMaximum + 1f);
            if (cellClimate.Clouds > cloudMaximum)
            {
                cellClimate.Moisture += cellClimate.Clouds - cloudMaximum;
                cellClimate.Clouds = cloudMaximum;
            }

            HexDirection mainDispersalDirection = _windDirection.Opposite();
            float cloudDispersal = cellClimate.Clouds * (1f / (5f + _windStrength));
            float runoff = cellClimate.Moisture * _runoffFactor * (1f / 6f);
            float seepage = cellClimate.Moisture * _seepageFactor * (1f / 6f);
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                if (!_grid.TryGetCellIndex(
                    cell.Coordinates.Step(d), out int neighborIndex))
                {
                    continue;
                }
                ClimateData neighborClimate = _nextClimate[neighborIndex];
                if (d == mainDispersalDirection)
                {
                    neighborClimate.Clouds += cloudDispersal * _windStrength;
                }
                else
                {
                    neighborClimate.Clouds += cloudDispersal;
                }

                int elevationDelta = _grid.CellData[neighborIndex].ViewElevation -
                    cell.ViewElevation;
                if (elevationDelta < 0)
                {
                    cellClimate.Moisture -= runoff;
                    neighborClimate.Moisture += runoff;
                }
                else if (elevationDelta == 0)
                {
                    cellClimate.Moisture -= seepage;
                    neighborClimate.Moisture += seepage;
                }

                _nextClimate[neighborIndex] = neighborClimate;
            }

            ClimateData nextCellClimate = _nextClimate[cellIndex];
            nextCellClimate.Moisture += cellClimate.Moisture;
            if (nextCellClimate.Moisture > 1f)
            {
                nextCellClimate.Moisture = 1f;
            }
            _nextClimate[cellIndex] = nextCellClimate;
            _climate[cellIndex] = new ClimateData();
        }

        private void CreateRivers()
        {
            List<int> riverOrigins = ListPool<int>.Get();
            for (int i = 0; i < _cellCount; i++)
            {
                HexCellData cell = _grid.CellData[i];
                if (cell.IsUnderwater)
                {
                    continue;
                }
                ClimateData data = _climate[i];
                float weight =
                    data.Moisture * (cell.Elevation - _waterLevel) /
                    (_elevationMaximum - _waterLevel);
                if (weight > 0.75f)
                {
                    riverOrigins.Add(i);
                    riverOrigins.Add(i);
                }
                if (weight > 0.5f)
                {
                    riverOrigins.Add(i);
                }
                if (weight > 0.25f)
                {
                    riverOrigins.Add(i);
                }
            }

            int riverBudget = Mathf.RoundToInt(_landCells * _riverPercentage * 0.01f);
            while (riverBudget > 0 && riverOrigins.Count > 0)
            {
                int index = Random.Range(0, riverOrigins.Count);
                int lastIndex = riverOrigins.Count - 1;
                int originIndex = riverOrigins[index];
                HexCellData origin = _grid.CellData[originIndex];
                riverOrigins[index] = riverOrigins[lastIndex];
                riverOrigins.RemoveAt(lastIndex);

                if (!origin.HasRiver)
                {
                    bool isValidOrigin = true;
                    for (HexDirection d = HexDirection.NE;
                        d <= HexDirection.NW; d++)
                    {
                        if (_grid.TryGetCellIndex(
                            origin.Coordinates.Step(d), out int neighborIndex) &&
                            (_grid.CellData[neighborIndex].HasRiver ||
                                _grid.CellData[neighborIndex].IsUnderwater))
                        {
                            isValidOrigin = false;
                            break;
                        }
                    }
                    if (isValidOrigin)
                    {
                        riverBudget -= CreateRiver(originIndex);
                    }
                }
            }

            if (riverBudget > 0)
            {
                Debug.LogWarning("Failed to use up river budget.");
            }

            ListPool<int>.Add(riverOrigins);
        }

        private int CreateRiver(int originIndex)
        {
            int length = 1;
            int cellIndex = originIndex;
            HexCellData cell = _grid.CellData[cellIndex];
            HexDirection direction = HexDirection.NE;
            while (!cell.IsUnderwater)
            {
                int minNeighborElevation = int.MaxValue;
                _flowDirections.Clear();
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    if (!_grid.TryGetCellIndex(
                        cell.Coordinates.Step(d), out int neighborIndex))
                    {
                        continue;
                    }
                    HexCellData neighbor = _grid.CellData[neighborIndex];

                    if (neighbor.Elevation < minNeighborElevation)
                    {
                        minNeighborElevation = neighbor.Elevation;
                    }

                    if (neighborIndex == originIndex || neighbor.HasIncomingRiver)
                    {
                        continue;
                    }

                    int delta = neighbor.Elevation - cell.Elevation;
                    if (delta > 0)
                    {
                        continue;
                    }

                    if (neighbor.HasOutgoingRiver)
                    {
                        _grid.CellData[cellIndex].Flags = cell.Flags.WithRiverOut(d);
                        _grid.CellData[neighborIndex].Flags =
                            neighbor.Flags.WithRiverIn(d.Opposite());
                        return length;
                    }

                    if (delta < 0)
                    {
                        _flowDirections.Add(d);
                        _flowDirections.Add(d);
                        _flowDirections.Add(d);
                    }
                    if (length == 1 ||
                        d != direction.Next2() && d != direction.Previous2())
                    {
                        _flowDirections.Add(d);
                    }
                    _flowDirections.Add(d);
                }

                if (_flowDirections.Count == 0)
                {
                    if (length == 1)
                    {
                        return 0;
                    }

                    if (minNeighborElevation >= cell.Elevation)
                    {
                        cell.Values = cell.Values.WithWaterLevel(
                            minNeighborElevation);
                        if (minNeighborElevation == cell.Elevation)
                        {
                            cell.Values = cell.Values.WithElevation(
                                minNeighborElevation - 1);
                        }
                        _grid.CellData[cellIndex].Values = cell.Values;
                    }
                    break;
                }

                direction = _flowDirections[Random.Range(0, _flowDirections.Count)];
                cell.Flags = cell.Flags.WithRiverOut(direction);
                _grid.TryGetCellIndex(
                    cell.Coordinates.Step(direction), out int outIndex);
                _grid.CellData[outIndex].Flags =
                    _grid.CellData[outIndex].Flags.WithRiverIn(direction.Opposite());

                length += 1;

                if (minNeighborElevation >= cell.Elevation &&
                    Random.value < _extraLakeProbability)
                {
                    cell.Values = cell.Values.WithWaterLevel(cell.Elevation);
                    cell.Values = cell.Values.WithElevation(cell.Elevation - 1);
                }
                _grid.CellData[cellIndex] = cell;
                cellIndex = outIndex;
                cell = _grid.CellData[cellIndex];
            }
            return length;
        }

        private void SetTerrainType()
        {
            _temperatureJitterChannel = Random.Range(0, 4);
            int rockDesertElevation =
                _elevationMaximum - (_elevationMaximum - _waterLevel) / 2;

            for (int i = 0; i < _cellCount; i++)
            {
                HexCellData cell = _grid.CellData[i];
                float temperature = DetermineTemperature(i, cell);
                float moisture = _climate[i].Moisture;
                if (!cell.IsUnderwater)
                {
                    int t = 0;
                    for (; t < _temperatureBands.Length; t++)
                    {
                        if (temperature < _temperatureBands[t])
                        {
                            break;
                        }
                    }
                    int m = 0;
                    for (; m < _moistureBands.Length; m++)
                    {
                        if (moisture < _moistureBands[m])
                        {
                            break;
                        }
                    }
                    Biome cellBiome = _biomes[t * 4 + m];

                    if (cellBiome.Terrain == 0)
                    {
                        if (cell.Elevation >= rockDesertElevation)
                        {
                            cellBiome.Terrain = 3;
                        }
                    }
                    else if (cell.Elevation == _elevationMaximum)
                    {
                        cellBiome.Terrain = 4;
                    }

                    if (cellBiome.Terrain == 4)
                    {
                        cellBiome.Plant = 0;
                    }
                    else if (cellBiome.Plant < 3 && cell.HasRiver)
                    {
                        cellBiome.Plant += 1;
                    }
                    _grid.CellData[i].Values = cell.Values.
                        WithTerrainType((TerrainType)cellBiome.Terrain).
                        WithMineralType((MineralType)cellBiome.Plant);
                }
                else
                {
                    int terrain;
                    if (cell.Elevation == _waterLevel - 1)
                    {
                        int cliffs = 0, slopes = 0;
                        for (HexDirection d = HexDirection.NE;
                            d <= HexDirection.NW; d++)
                        {
                            if (!_grid.TryGetCellIndex(
                                cell.Coordinates.Step(d), out int neighborIndex))
                            {
                                continue;
                            }
                            int delta = _grid.CellData[neighborIndex].Elevation -
                                cell.WaterLevel;
                            if (delta == 0)
                            {
                                slopes += 1;
                            }
                            else if (delta > 0)
                            {
                                cliffs += 1;
                            }
                        }

                        if (cliffs + slopes > 3)
                        {
                            terrain = 1;
                        }
                        else if (cliffs > 0)
                        {
                            terrain = 3;
                        }
                        else if (slopes > 0)
                        {
                            terrain = 0;
                        }
                        else
                        {
                            terrain = 1;
                        }
                    }
                    else if (cell.Elevation >= _waterLevel)
                    {
                        terrain = 1;
                    }
                    else if (cell.Elevation < 0)
                    {
                        terrain = 3;
                    }
                    else
                    {
                        terrain = 2;
                    }

                    if (terrain == 1 && temperature < _temperatureBands[0])
                    {
                        terrain = 2;
                    }
                    _grid.CellData[i].Values =
                        cell.Values.WithTerrainType((Core.Enums.TerrainType)terrain);
                }
            }
        }

        private float DetermineTemperature(int cellIndex, HexCellData cell)
        {
            float latitude = (float)cell.Coordinates.Z /
                _grid.CellCountZ;
            if (_hemisphere == HemisphereMode.Both)
            {
                latitude *= 2f;
                if (latitude > 1f)
                {
                    latitude = 2f - latitude;
                }
            }
            else if (_hemisphere == HemisphereMode.North)
            {
                latitude = 1f - latitude;
            }

            float temperature =
                Mathf.LerpUnclamped(_lowTemperature, _highTemperature, latitude);

            temperature *= 1f -
                (cell.ViewElevation - _waterLevel) /
                (_elevationMaximum - _waterLevel + 1f);

            float jitter = HexMetrics.SampleNoise(
                _grid.CellPositions[cellIndex] * 0.1f)[_temperatureJitterChannel];

            temperature += (jitter * 2f - 1f) * _temperatureJitter;

            return temperature;
        }

        private int GetRandomCellIndex(MapRegion region) => _grid.GetCellIndex(
            Random.Range(region.XMin, region.XMax),
            Random.Range(region.ZMin, region.ZMax));

        public enum HemisphereMode { Both, North, South }

        private struct MapRegion
        {
            public int XMin; 
            public int XMax; 
            public int ZMin; 
            public int ZMax;
        }

        private struct ClimateData
        {
            public float Clouds;
            public float Moisture;
        }

        private struct Biome
        {
            public int Terrain;
            public int Plant;

            public Biome(int terrain, int plant)
            {
                Terrain = terrain;
                Plant = plant;
            }
        }
    }
}