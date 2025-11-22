using UnityEngine;

namespace TheFoundersPleas.World
{
    public class HexMapCreator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private HexGrid _hexGrid;
        [SerializeField] private HexMapGenerator _mapGenerator;
        [SerializeField] private MapGeneratorConfig _config;

        public void CreateMap()
        {
            bool generateMaps = _config.GenerateMaps;
            bool wrapping = _config.Wrapping;

            int width = _config.Width;
            int height = _config.Height;

            if (generateMaps)
            {
                _mapGenerator.GenerateMap(width, height, wrapping);
            }
            else
            {
                _hexGrid.CreateMap(width, height, wrapping);
            }
            HexMapCamera.ValidatePosition();
        }
    }
}