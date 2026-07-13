using System.Collections.Generic;
using UnityEngine;

namespace TheFoundersPleas.World
{
    /// <summary>
    /// Component that manages cell data used by shaders.
    /// </summary>
    public class HexCellShaderData : MonoBehaviour
    {
        public HexGrid Grid { get; set; }
        public bool ImmediateMode { get; set; }

        private const float _transitionSpeed = 255f;
        private Texture2D _cellTexture;
        private Color32[] _cellTextureData;
        private bool[] _visibilityTransitions;
        private List<int> _transitioningCellIndices = new();
        private bool _needsVisibilityReset;

        private void Awake()
        {
            enabled = false;
        }

        /// <summary>
        /// Initialze the map data.
        /// </summary>
        /// <param name="x">Map X size.</param>
        /// <param name="z">Map Z size.</param>
        public void Initialize(int x, int z)
        {
            if (_cellTexture)
            {
                _cellTexture.Reinitialize(x, z);
            }
            else
            {
                _cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true)
                {
                    filterMode = FilterMode.Point,
                    wrapModeU = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp
                };
                Shader.SetGlobalTexture("_HexCellData", _cellTexture);
            }
            Shader.SetGlobalVector(
                "_HexCellData_TexelSize",
                new Vector4(1f / x, 1f / z, x, z));

            if (_cellTextureData == null || _cellTextureData.Length != x * z)
            {
                _cellTextureData = new Color32[x * z];
                _visibilityTransitions = new bool[x * z];
            }
            else
            {
                for (int i = 0; i < _cellTextureData.Length; i++)
                {
                    _cellTextureData[i] = new Color32(0, 0, 0, 0);
                    _visibilityTransitions[i] = false;
                }
            }

            _transitioningCellIndices.Clear();
            enabled = true;
        }

        /// <summary>
        /// Refresh the terrain data of a cell.
        /// Supports water surfaces up to 30 units high.
        /// </summary>
        /// <param name="cell">Cell with changed terrain type.</param>
        public void RefreshTerrain(int cellIndex)
        {
            HexCellData cell = Grid.CellData[cellIndex];
            Color32 data = _cellTextureData[cellIndex];
            data.b = cell.IsUnderwater ?
                (byte)(cell.WaterSurfaceY * (255f / 30f)) : (byte)0;
            data.a = (byte)cell.TerrainType;
            _cellTextureData[cellIndex] = data;
            enabled = true;
        }

        /// <summary>
        /// Refresh visibility of a cell.
        /// </summary>
        /// <param name="cell">Cell with changed visibility.</param>
        public void RefreshVisibility(int cellIndex)
        {
            if (ImmediateMode)
            {
                _cellTextureData[cellIndex].r = Grid.IsCellVisible(cellIndex) ?
                    (byte)255 : (byte)0;
                _cellTextureData[cellIndex].g = Grid.CellData[cellIndex].IsExplored ?
                    (byte)255 : (byte)0;
            }
            else if (!_visibilityTransitions[cellIndex])
            {
                _visibilityTransitions[cellIndex] = true;
                _transitioningCellIndices.Add(cellIndex);
            }
            enabled = true;
        }

        /// <summary>
        /// Indicate that view elevation data has changed,
        /// requiring a visibility reset.
        /// Supports water surfaces up to 30 units high.
        /// </summary>
        /// <param name="cell">Changed cell.</param>
        public void ViewElevationChanged(int cellIndex)
        {
            HexCellData cell = Grid.CellData[cellIndex];
            _cellTextureData[cellIndex].b = cell.IsUnderwater ?
                (byte)(cell.WaterSurfaceY * (255f / 30f)) : (byte)0;
            _needsVisibilityReset = true;
            enabled = true;
        }

        private void LateUpdate()
        {
            if (_needsVisibilityReset)
            {
                _needsVisibilityReset = false;
                Grid.ResetVisibility();
            }

            int delta = (int)(Time.deltaTime * _transitionSpeed);
            if (delta == 0)
            {
                delta = 1;
            }
            for (int i = 0; i < _transitioningCellIndices.Count; i++)
            {
                if (!UpdateCellData(_transitioningCellIndices[i], delta))
                {
                    int lastIndex = _transitioningCellIndices.Count - 1;
                    _transitioningCellIndices[i--] =
                        _transitioningCellIndices[lastIndex];
                    _transitioningCellIndices.RemoveAt(lastIndex);
                }
            }

            _cellTexture.SetPixels32(_cellTextureData);
            _cellTexture.Apply();
            enabled = _transitioningCellIndices.Count > 0;
        }

        private bool UpdateCellData(int index, int delta)
        {
            Color32 data = _cellTextureData[index];
            bool stillUpdating = false;

            if (Grid.CellData[index].IsExplored && data.g < 255)
            {
                stillUpdating = true;
                int t = data.g + delta;
                data.g = t >= 255 ? (byte)255 : (byte)t;
            }

            if (Grid.IsCellVisible(index))
            {
                if (data.r < 255)
                {
                    stillUpdating = true;
                    int t = data.r + delta;
                    data.r = t >= 255 ? (byte)255 : (byte)t;
                }
            }
            else if (data.r > 0)
            {
                stillUpdating = true;
                int t = data.r - delta;
                data.r = t < 0 ? (byte)0 : (byte)t;
            }

            if (!stillUpdating)
            {
                _visibilityTransitions[index] = false;
            }
            _cellTextureData[index] = data;
            return stillUpdating;
        }
    }
}