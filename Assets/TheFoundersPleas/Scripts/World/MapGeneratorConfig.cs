using UnityEngine;
using static TheFoundersPleas.World.HexMapGenerator;

namespace TheFoundersPleas.World
{
    [CreateAssetMenu(fileName = "New MapGeneratorConfig", menuName = "World/Map Generator Config")]
    public class MapGeneratorConfig : ScriptableObject
    {
        [field: SerializeField] public int Width { get; set; }
        [field: SerializeField] public int Height { get; set; }
        [field: SerializeField] public bool GenerateMaps { get; set; }
        [field: SerializeField] public bool Wrapping { get; set; }
        [field: SerializeField] public bool UseFixedSeed { get; set; }
        [field: SerializeField] public int Seed { get; set; }
        [field: SerializeField, Range(0f, 0.5f)] public float JitterProbability { get; set; } = 0.25f;
        [field: SerializeField, Range(20, 200)] public int ChunkSizeMin { get; set; } = 30;
        [field: SerializeField, Range(20, 200)] public int ChunkSizeMax { get; set; } = 100;
        [field: SerializeField, Range(0f, 1f)] public float HighRiseProbability { get; set; } = 0.25f;
        [field: SerializeField, Range(0f, 0.4f)] public float SinkProbability { get; set; } = 0.2f;
        [field: SerializeField, Range(5, 95)] public int LandPercentage { get; set; } = 50;
        [field: SerializeField, Range(1, 5)] public int WaterLevel { get; set; } = 3;
        [field: SerializeField, Range(-4, 0)] public int ElevationMinimum { get; set; } = -2;
        [field: SerializeField, Range(6, 10)] public int ElevationMaximum { get; set; } = 8;
        [field: SerializeField, Range(0, 10)] public int MapBorderX { get; set; } = 5;
        [field: SerializeField, Range(0, 10)] public int MapBorderZ { get; set; } = 5;
        [field: SerializeField, Range(0, 10)] public int RegionBorder { get; set; } = 5;
        [field: SerializeField, Range(1, 4)] public int RegionCount { get; set; } = 1;
        [field: SerializeField, Range(0, 100)] public int ErosionPercentage { get; set; } = 50;
        [field: SerializeField, Range(0f, 1f)] public float StartingMoisture { get; set; } = 0.1f;
        [field: SerializeField, Range(0f, 1f)] public float EvaporationFactor { get; set; } = 0.5f;
        [field: SerializeField, Range(0f, 1f)] public float PrecipitationFactor { get; set; } = 0.25f;
        [field: SerializeField, Range(0f, 1f)] public float RunoffFactor { get; set; } = 0.25f;
        [field: SerializeField, Range(0f, 1f)] public float SeepageFactor { get; set; } = 0.125f;
        [field: SerializeField] public HexDirection WindDirection { get; set; } = HexDirection.NW;
        [field: SerializeField, Range(1f, 10f)] public float WindStrength { get; set; } = 4f;
        [field: SerializeField, Range(0, 20)] public int RiverPercentage { get; set; } = 10;
        [field: SerializeField, Range(0f, 1f)] public float ExtraLakeProbability { get; set; } = 0.25f;
        [field: SerializeField, Range(0f, 1f)] public float LowTemperature { get; set; } = 0f;
        [field: SerializeField, Range(0f, 1f)] public float HighTemperature { get; set; } = 1f;
        [field: SerializeField] public HemisphereMode Hemisphere { get; set; }
        [field: SerializeField, Range(0f, 1f)] public float TemperatureJitter { get; set; } = 0.1f;
        [field: SerializeField] public float[] TemperatureBands { get; set; } = { 0.1f, 0.3f, 0.6f };
        [field: SerializeField] public float[] MoistureBands { get; set; } = { 0.12f, 0.28f, 0.85f };
        [field: SerializeField]
        public Biome[] Biomes { get; set; } = {
        new(0, 0), new(4, 0), new(4, 0), new(4, 0),
        new(0, 0), new(2, 0), new(2, 1), new(2, 2),
        new(0, 0), new(1, 0), new(1, 1), new(1, 2),
        new(0, 0), new(1, 1), new(1, 2), new(1, 3)
    };
    }
}