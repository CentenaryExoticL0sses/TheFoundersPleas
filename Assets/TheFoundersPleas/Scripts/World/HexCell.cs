using TheFoundersPleas.Core.Enums;
using UnityEngine;

namespace TheFoundersPleas.World
{
    /// <summary>
    /// Struct that identifies a hex cell.
    /// </summary>
    [System.Serializable]
    public struct HexCell
    {
        /// <summary>
        /// Hexagonal coordinates unique to the cell.
        /// </summary>
        public readonly HexCoordinates Coordinates =>
            _grid.CellData[_index].Coordinates;

        /// <summary>
        /// Unique global index of the cell.
        /// </summary>
        public readonly int Index => _index;

        /// <summary>
        /// Local position of this cell.
        /// </summary>
        public readonly Vector3 Position => _grid.CellPositions[_index];

        /// <summary>
        /// Unit currently occupying the cell, if any.
        /// </summary>
        public readonly HexUnit Unit
        {
            get => _grid.CellUnits[_index];
            set => _grid.CellUnits[_index] = value;
        }

        /// <summary>
        /// Flags of the cell.
        /// </summary>
        public readonly HexFlags Flags
        {
            get => _grid.CellData[_index].Flags;
            set => _grid.CellData[_index].Flags = value;
        }

        /// <summary>
        /// Values of the cell.
        /// </summary>
        public readonly HexValues Values
        {
            get => _grid.CellData[_index].Values;
            set => _grid.CellData[_index].Values = value;
        }

#pragma warning disable IDE0044 // Add readonly modifier
        private int _index;
        private HexGrid _grid;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// Creates a cell given an index and grid.
        /// </summary>
        /// <param name="index">Index of the cell.</param>
        /// <param name="grid">Grid the cell is a part of.</param>
        public HexCell(int index, HexGrid grid)
        {
            this._index = index;
            this._grid = grid;
        }

        /// <summary>
        /// Set the elevation level.
        /// </summary>
        /// <param name="elevation">Elevation level.</param>
        public readonly void SetElevation(int elevation)
        {
            if (Values.Elevation != elevation)
            {
                Values = Values.WithElevation(elevation);
                _grid.ShaderData.ViewElevationChanged(_index);
                _grid.RefreshCellPosition(_index);
                ValidateRivers();
                HexFlags flags = Flags;
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    if (flags.HasRoad(d))
                    {
                        HexCell neighbor = GetNeighbor(d);
                        if (Mathf.Abs(elevation - neighbor.Values.Elevation) > 1)
                        {
                            RemoveRoad(d);
                        }
                    }
                }
                _grid.RefreshCellWithDependents(_index);
            }
        }

        /// <summary>
        /// Set the water level.
        /// </summary>
        /// <param name="waterLevel">Water level.</param>
        public readonly void SetWaterLevel(int waterLevel)
        {
            if (Values.WaterLevel != waterLevel)
            {
                Values = Values.WithWaterLevel(waterLevel);
                _grid.ShaderData.ViewElevationChanged(_index);
                ValidateRivers();
                _grid.RefreshCellWithDependents(_index);
            }
        }

        /// <summary>
        /// Set the animal type.
        /// </summary>
        /// <param name="value">Animal type.</param>
        public readonly void SetAnimalType(AnimalType value)
        {
            if (Values.AnimalType != value)
            {
                Values = Values.WithAnimalType(value);
                Refresh();
            }
        }

        /// <summary>
        /// Set the plant type.
        /// </summary>
        /// <param name="value">Plant type.</param>
        public readonly void SetPlantType(PlantType value)
        {
            if (Values.PlantType != value)
            {
                Values = Values.WithPlantType(value);
                Refresh();
            }
        }

        /// <summary>
        /// Set the mineral type.
        /// </summary>
        /// <param name="value">Mineral type.</param>
        public readonly void SetMineralType(MineralType value)
        {
            if (Values.MineralType != value)
            {
                Values = Values.WithMineralType(value);
                Refresh();
            }
        }

        /// <summary>
        /// Set the structure type.
        /// </summary>
        /// <param name="value">Structure type.</param>
        public readonly void SetStructureType(StructureType value)
        {
            if (Values.StructureType != value &&
                Flags.HasNone(HexFlags.River))
            {
                Values = Values.WithStructureType(value);
                RemoveRoads();
                Refresh();
            }
        }

        /// <summary>
        /// Set whether the cell is walled.
        /// </summary>
        /// <param name="walled">Whether the cell is walled.</param>
        public readonly void SetWalled(bool walled)
        {
            HexFlags flags = Flags;
            HexFlags newFlags = walled ?
                flags.With(HexFlags.Walled) : flags.Without(HexFlags.Walled);
            if (flags != newFlags)
            {
                Flags = newFlags;
                _grid.RefreshCellWithDependents(_index);
            }
        }

        /// <summary>
        /// Set the terrain type.
        /// </summary>
        /// <param name="value">Terrain type index.</param>
        public readonly void SetTerrainType(TerrainType value)
        {
            if (Values.TerrainType != value)
            {
                Values = Values.WithTerrainType(value);
                _grid.ShaderData.RefreshTerrain(_index);
            }
        }

        /// <summary>
        /// Get one of the neighbor cells. Only valid if that neighbor exists.
        /// </summary>
        /// <param name="direction">Neighbor direction relative to the cell.</param>
        /// <returns>Neighbor cell, if it exists.</returns>
        public readonly HexCell GetNeighbor(HexDirection direction) =>
            _grid.GetCell(Coordinates.Step(direction));

        /// <summary>
        /// Try to get one of the neighbor cells.
        /// </summary>
        /// <param name="direction">Neighbor direction relative to the cell.</param>
        /// <param name="cell">The neighbor cell, if it exists.</param>
        /// <returns>Whether the neighbor exists.</returns>
        public readonly bool TryGetNeighbor(
            HexDirection direction, out HexCell cell) =>
            _grid.TryGetCell(Coordinates.Step(direction), out cell);

        private readonly void RemoveIncomingRiver()
        {
            if (Flags.HasAny(HexFlags.RiverIn))
            {
                HexCell neighbor = GetNeighbor(Flags.RiverInDirection());
                Flags = Flags.Without(HexFlags.RiverIn);
                neighbor.Flags = neighbor.Flags.Without(HexFlags.RiverOut);
                neighbor.Refresh();
                Refresh();
            }
        }

        private readonly void RemoveOutgoingRiver()
        {
            if (Flags.HasAny(HexFlags.RiverOut))
            {
                HexCell neighbor = GetNeighbor(Flags.RiverOutDirection());
                Flags = Flags.Without(HexFlags.RiverOut);
                neighbor.Flags = neighbor.Flags.Without(HexFlags.RiverIn);
                neighbor.Refresh();
                Refresh();
            }
        }

        /// <summary>
        /// Clear the cell of rivers.
        /// </summary>
        public readonly void RemoveRiver()
        {
            RemoveIncomingRiver();
            RemoveOutgoingRiver();
        }

        private static bool CanRiverFlow(HexValues from, HexValues to) =>
            from.Elevation >= to.Elevation || from.WaterLevel == to.Elevation;

        /// <summary>
        /// Set the outgoing river.
        /// </summary>
        /// <param name="direction">River direction.</param>
        public readonly void SetOutgoingRiver(HexDirection direction)
        {
            if (Flags.HasRiverOut(direction))
            {
                return;
            }

            HexCell neighbor = GetNeighbor(direction);
            if (!CanRiverFlow(Values, neighbor.Values))
            {
                return;
            }

            RemoveOutgoingRiver();
            if (Flags.HasRiverIn(direction))
            {
                RemoveIncomingRiver();
            }

            Flags = Flags.WithRiverOut(direction);
            Values = Values.WithStructureType(0);
            neighbor.RemoveIncomingRiver();
            neighbor.Flags = neighbor.Flags.WithRiverIn(direction.Opposite());
            neighbor.Values = neighbor.Values.WithStructureType(0);

            RemoveRoad(direction);
        }

        /// <summary>
        /// Add a road in the given direction.
        /// </summary>
        /// <param name="direction">Road direction.</param>
        public readonly void AddRoad(HexDirection direction)
        {
            HexFlags flags = Flags;
            HexCell neighbor = GetNeighbor(direction);
            if (
                !flags.HasRoad(direction) && !flags.HasRiver(direction) &&
                Values.StructureType == 0 && neighbor.Values.StructureType == 0 &&
                Mathf.Abs(Values.Elevation - neighbor.Values.Elevation) <= 1
            )
            {
                Flags = flags.WithRoad(direction);
                neighbor.Flags = neighbor.Flags.WithRoad(direction.Opposite());
                neighbor.Refresh();
                Refresh();
            }
        }

        /// <summary>
        /// Clear the cell of roads.
        /// </summary>
        public readonly void RemoveRoads()
        {
            HexFlags flags = Flags;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                if (flags.HasRoad(d))
                {
                    RemoveRoad(d);
                }
            }
        }

        private readonly void ValidateRivers()
        {
            HexFlags flags = Flags;
            if (flags.HasAny(HexFlags.RiverOut) &&
                !CanRiverFlow(Values, GetNeighbor(flags.RiverOutDirection()).Values)
            )
            {
                RemoveOutgoingRiver();
            }
            if (flags.HasAny(HexFlags.RiverIn) &&
                !CanRiverFlow(GetNeighbor(flags.RiverInDirection()).Values, Values))
            {
                RemoveIncomingRiver();
            }
        }

        private readonly void RemoveRoad(HexDirection direction)
        {
            Flags = Flags.WithoutRoad(direction);
            HexCell neighbor = GetNeighbor(direction);
            neighbor.Flags = neighbor.Flags.WithoutRoad(direction.Opposite());
            neighbor.Refresh();
            Refresh();
        }

        private readonly void Refresh() => _grid.RefreshCell(_index);

        /// <inheritdoc/>
        public readonly override bool Equals(object obj) =>
            obj is HexCell cell && this == cell;

        /// <inheritdoc/>
        public readonly override int GetHashCode() =>
            _grid != null ? _index.GetHashCode() ^ _grid.GetHashCode() : 0;

        /// <summary>
        /// A cell counts as true if it is part of a grid.
        /// </summary>
        /// <param name="cell">The cell to check.</param>
        public static implicit operator bool(HexCell cell) => cell._grid != null;

        public static bool operator ==(HexCell a, HexCell b) =>
            a._index == b._index && a._grid == b._grid;

        public static bool operator !=(HexCell a, HexCell b) =>
            a._index != b._index || a._grid != b._grid;
    }
}