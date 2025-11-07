using TheFoundersPleas.Core.Enums;

namespace TheFoundersPleas.World
{
    /// <summary>
    /// Container struct for bundled hex cell data.
    /// </summary>
    [System.Serializable]
    public struct HexCellData
    {
        /// <summary>
        /// Cell flags.
        /// </summary>
        public HexFlags Flags;

        /// <summary>
        /// Cell values.
        /// </summary>
        public HexValues Values;

        /// <summary>
        /// Cell coordinates.
        /// </summary>
        public HexCoordinates Coordinates;

        /// <summary>
        /// Surface elevation level.
        /// </summary>
        public readonly int Elevation => Values.Elevation;

        /// <summary>
        /// Water elevation level.
        /// </summary>
        public readonly int WaterLevel => Values.WaterLevel;

        /// <summary>
        /// Terrain type index.
        /// </summary>
        public readonly TerrainType TerrainType => Values.TerrainType;

        /// <summary>
        /// Urban feature level.
        /// </summary>
        public readonly AnimalType AnimalType => Values.AnimalType;

        /// <summary>
        /// Farm feature level.
        /// </summary>
        public readonly PlantType PlantType => Values.PlantType;

        /// <summary>
        /// Plant feature level.
        /// </summary>
        public readonly MineralType MineralType => Values.MineralType;

        /// <summary>
        /// Special feature index.
        /// </summary>
        public readonly StructureType StructureType => Values.StructureType;

        /// <summary>
        /// Whether the cell is considered inside a walled region.
        /// </summary>
        public readonly bool Walled => Flags.HasAny(HexFlags.Walled);

        /// <summary>
        /// Whether the cell contains roads.
        /// </summary>
        public readonly bool HasRoads => Flags.HasAny(HexFlags.Roads);

        /// <summary>
        /// Whether the cell counts as explored.
        /// </summary>
        public readonly bool IsExplored => Flags.HasAll(HexFlags.Explored | HexFlags.Explorable);

        /// <summary>
        /// Whether the cell contains a special feature.
        /// </summary>
        public readonly bool IsSpecial => Values.StructureType > 0;

        /// <summary>
        /// Whether the cell counts as underwater,
        /// which is when water is higher than surface.
        /// </summary>
        public readonly bool IsUnderwater => Values.WaterLevel > Values.Elevation;

        /// <summary>
        /// Whether there is an incoming river.
        /// </summary>
        public readonly bool HasIncomingRiver => Flags.HasAny(HexFlags.RiverIn);

        /// <summary>
        /// Whether there is an outgoing river.
        /// </summary>
        public readonly bool HasOutgoingRiver => Flags.HasAny(HexFlags.RiverOut);

        /// <summary>
        /// Whether there is a river, either incoming, outgoing, or both.
        /// </summary>
        public readonly bool HasRiver => Flags.HasAny(HexFlags.River);

        /// <summary>
        /// Whether a river begins or ends in the cell.
        /// </summary>
        public readonly bool HasRiverBeginOrEnd => HasIncomingRiver != HasOutgoingRiver;

        /// <summary>
        /// Incoming river direction, if applicable.
        /// </summary>
        public readonly HexDirection IncomingRiver => Flags.RiverInDirection();

        /// <summary>
        /// Outgoing river direction, if applicable.
        /// </summary>
        /// 
        public readonly HexDirection OutgoingRiver => Flags.RiverOutDirection();

        /// <summary>
        /// Vertical positions the the stream bed, if applicable.
        /// </summary>
        public readonly float StreamBedY =>
            (Values.Elevation + HexMetrics.streamBedElevationOffset) *
            HexMetrics.elevationStep;

        /// <summary>
        /// Vertical position of the river's surface, if applicable.
        /// </summary>
        public readonly float RiverSurfaceY =>
            (Values.Elevation + HexMetrics.waterElevationOffset) *
            HexMetrics.elevationStep;

        /// <summary>
        /// Vertical position of the water surface, if applicable.
        /// </summary>
        public readonly float WaterSurfaceY =>
            (Values.WaterLevel + HexMetrics.waterElevationOffset) *
            HexMetrics.elevationStep;

        /// <summary>
        /// Elevation at which the cell is visible.
        /// Highest of surface and water level.
        /// </summary>
        public readonly int ViewElevation =>
            Elevation >= WaterLevel ? Elevation : WaterLevel;

        // <summary>
        /// Get the <see cref="HexEdgeType"/> based on this and another cell.
        /// </summary>
        /// <param name="otherCell">Other cell to consider as neighbor.</param>
        /// <returns><see cref="HexEdgeType"/> between cells.</returns>
        public readonly HexEdgeType GetEdgeType(HexCellData otherCell) =>
            HexMetrics.GetEdgeType(Values.Elevation, otherCell.Values.Elevation);

        /// <summary>
        /// Whether an incoming river goes through a specific cell edge.
        /// </summary>
        /// <param name="direction">Edge direction relative to the cell.</param>
        /// <returns>Whether an incoming river goes through the edge.</returns>
        public readonly bool HasIncomingRiverThroughEdge(HexDirection direction) =>
            Flags.HasRiverIn(direction);

        /// <summary>
        /// Whether a river goes through a specific cell edge.
        /// </summary>
        /// <param name="direction">Edge direction relative to the cell.</param>
        /// <returns>Whether a river goes through the edge.</returns>
        public readonly bool HasRiverThroughEdge(HexDirection direction) =>
            Flags.HasRiverIn(direction) || Flags.HasRiverOut(direction);

        /// <summary>
        /// Whether a road goes through a specific cell edge.
        /// </summary>
        /// <param name="direction">Edge direction relative to cell.</param>
        /// <returns>Whether a road goes through the edge.</returns>
        public readonly bool HasRoadThroughEdge(HexDirection direction) =>
            Flags.HasRoad(direction);
    }
}