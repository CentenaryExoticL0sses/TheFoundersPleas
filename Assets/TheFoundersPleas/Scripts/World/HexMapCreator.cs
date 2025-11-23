using UnityEngine;

namespace TheFoundersPleas.World
{
    public class HexMapCreator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private MapGeneratorConfig _config;

        private HexGrid _hexGrid;
        private HexMapGenerator _mapGenerator;
        private HexMapCamera _camera;

        public void Initialize(
            HexGrid hexGrid, 
            HexMapGenerator mapGenerator,
            HexMapCamera mapCamera)
        {
            _hexGrid = hexGrid;
            _mapGenerator = mapGenerator;
            _camera = mapCamera;
        }

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
            _camera.ValidatePosition();
        }
    }
}