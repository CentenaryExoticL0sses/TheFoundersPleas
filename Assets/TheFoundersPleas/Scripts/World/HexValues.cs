using System.IO;
using TheFoundersPleas.Core.Enums;
using UnityEngine;

namespace TheFoundersPleas.World
{
    /// <summary>
    /// Values that describe the contents of a cell.
    /// </summary>
    [System.Serializable]
    public struct HexValues
    {
        public readonly int Elevation => Get(31, 0) - 15;                           // 5 bits (0-4)
        public readonly int WaterLevel => Get(31, 5);                               // 5 bits (5-9)
        public readonly AnimalType AnimalType => (AnimalType)Get(15, 10);           // 4 bits (10-13)
        public readonly PlantType PlantType => (PlantType)Get(15, 14);              // 4 bits (14-17)
        public readonly MineralType MineralType => (MineralType)Get(7, 18);         // 3 bits (18-20)
        public readonly StructureType StructureType => (StructureType)Get(127, 21); // 7 bits (21-27)
        public readonly TerrainType TerrainType => (TerrainType)Get(15, 28);        // 4 bits (28-31)

        public readonly int ViewElevation => Mathf.Max(Elevation, WaterLevel);
        public readonly bool IsUnderwater => WaterLevel > Elevation;

        public readonly HexValues WithElevation(int value) => With(value + 15, 31, 0);
        public readonly HexValues WithWaterLevel(int value) => With(value, 31, 5);
        public readonly HexValues WithAnimalType(AnimalType value) => With((int)value, 15, 10);
        public readonly HexValues WithPlantType(PlantType value) => With((int)value, 15, 14);
        public readonly HexValues WithMineralType(MineralType value) => With((int)value, 7, 18);
        public readonly HexValues WithStructureType(StructureType index) => With((int)index, 127, 21);
        public readonly HexValues WithTerrainType(TerrainType index) => With((int)index, 15, 28);

        /// <summary>
        /// Seven values stored in 32 bits.
        /// TTTTSSSSSSSMMMPPPPAAAAWWWWWEEEEE
        /// </summary>
        /// <remarks>Not readonly to support hot reloading in Unity.</remarks>
#pragma warning disable IDE0044 // Add readonly modifier
        private int _values;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// Save the values.
        /// </summary>
        /// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
        public readonly void Save(BinaryWriter writer)
        {
            writer.Write((byte)TerrainType);
            writer.Write((byte)(Elevation + 127));
            writer.Write((byte)WaterLevel);
            writer.Write((byte)AnimalType);
            writer.Write((byte)PlantType);
            writer.Write((byte)MineralType);
            writer.Write((byte)StructureType);
        }

        /// <summary>
        /// Load the values.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> to use.</param>
        /// <param name="header">Header version.</param>
        public static HexValues Load(BinaryReader reader, int header)
        {
            HexValues values = default;
            values = values.WithTerrainType((TerrainType)reader.ReadByte());
            int elevation = reader.ReadByte();
            if (header >= 4)
            {
                elevation -= 127;
            }
            values = values.WithElevation(elevation);
            values = values.WithWaterLevel(reader.ReadByte());
            values = values.WithAnimalType((AnimalType)reader.ReadByte());
            values = values.WithPlantType((PlantType)reader.ReadByte());
            values = values.WithMineralType((MineralType)reader.ReadByte());
            return values.WithStructureType((StructureType)reader.ReadByte());
        }

        private readonly int Get(int mask, int shift) => (int)((uint)_values >> shift) & mask;

        private readonly HexValues With(int value, int mask, int shift) => new()
        {
            _values = _values & ~(mask << shift) | (value & mask) << shift
        };
    }
}