using System;
using System.Collections.Generic;
using UnityEngine;
using TheFoundersPleas.Common.Pooling;
using TheFoundersPleas.Core.Enums;

namespace TheFoundersPleas.World
{
    public class HexFeatureManager : MonoBehaviour
    {
        [Header("Main Features")]
        [SerializeField] private FeatureItem<PlantType>[] _plantFeatures;
        [SerializeField] private FeatureItem<MineralType>[] _resourceFeatures;
        [SerializeField] private FeatureItem<AnimalType>[] _animalFeatures;
        [SerializeField] private FeatureItem<StructureType>[] _structureFeatures;

        [Header("Additional")]
        [SerializeField] private HexMesh _walls;
        [SerializeField] private Transform _wallTower;
        [SerializeField] private Transform _bridge;

        private Transform _container;

        private Dictionary<int, HashSet<FeatureType>> _placedFeatures;

        private void Awake()
        {
            _placedFeatures = new Dictionary<int, HashSet<FeatureType>>();
        }

        public void Clear()
        {
            if (_container) Destroy(_container.gameObject);

            _container = new GameObject("Container").transform;
            _container.SetParent(transform, false);

            _walls.Clear();

            foreach (var placed in _placedFeatures.Values) placed.Clear();
        }

        public void Apply()
        {
            _walls.Apply();
        }

        public void AddFeature(HexCellData cell, int cellIndex, Vector3 position)
        {
            if (cell.IsSpecial) return;

            int totalUniqueTypesOnCell = 0;
            if (cell.PlantType > 0) totalUniqueTypesOnCell++;
            if (cell.AnimalType > 0) totalUniqueTypesOnCell++;
            if (cell.MineralType > 0) totalUniqueTypesOnCell++;

            if (totalUniqueTypesOnCell == 0) return;

            if (!_placedFeatures.TryGetValue(cellIndex, out var placedUniques))
            {
                placedUniques = new HashSet<FeatureType>();
                _placedFeatures.Add(cellIndex, placedUniques);
            }

            var candidates = ListPool<(Transform prefab, float hash, FeatureType type)>.Get();
            HexHash hashValue = HexMetrics.SampleHashGrid(position);

            if (placedUniques.Count < totalUniqueTypesOnCell)
            {
                if (cell.PlantType > 0 && !placedUniques.Contains(FeatureType.Plant))
                {
                    Transform prefab = PickPrefab(_plantFeatures, cell.PlantType);
                    candidates.Add((prefab, hashValue.A, FeatureType.Plant));
                }
                if (cell.AnimalType > 0 && !placedUniques.Contains(FeatureType.Animal))
                {
                    Transform prefab = PickPrefab(_animalFeatures, cell.AnimalType);
                    candidates.Add((prefab, hashValue.B, FeatureType.Animal));
                }
                if (cell.MineralType > 0 && !placedUniques.Contains(FeatureType.Mineral))
                {
                    Transform prefab = PickPrefab(_resourceFeatures, cell.MineralType);
                    candidates.Add((prefab, hashValue.C, FeatureType.Mineral));
                }
            }
            else
            {
                if (cell.PlantType > 0)
                {
                    Transform prefab = PickPrefab(_plantFeatures, cell.PlantType);
                    candidates.Add((prefab, hashValue.A, FeatureType.Plant));
                }
                if (cell.AnimalType > 0)
                {
                    Transform prefab = PickPrefab(_animalFeatures, cell.AnimalType);
                    candidates.Add((prefab, hashValue.B, FeatureType.Animal));
                }
                if (cell.MineralType > 0)
                {
                    Transform prefab = PickPrefab(_resourceFeatures, cell.MineralType);
                    candidates.Add((prefab, hashValue.C, FeatureType.Mineral));
                }
                candidates.Add((null, hashValue.F, FeatureType.None));
            }

            if (candidates.Count == 0) return;

            (Transform prefab, float hash, FeatureType type) winner = candidates[0];
            for (int i = 1; i < candidates.Count; i++)
            {
                if (candidates[i].hash < winner.hash) winner = candidates[i];
            }

            if (winner.prefab)
            {
                Transform instance = Instantiate(winner.prefab);
                instance.SetLocalPositionAndRotation(HexMetrics.Perturb(position), Quaternion.Euler(0f, 360f * hashValue.E, 0f));
                instance.SetParent(_container, false);
                placedUniques.Add(winner.type);
            }

            ListPool<(Transform prefab, float hash, FeatureType type)>.Add(candidates);
        }

        private Transform PickPrefab<T>(FeatureItem<T>[] collection, T type) where T : Enum
        {
            foreach(var item in collection)
            {
                if(EqualityComparer<T>.Default.Equals(item.FeatureType, type))
                {
                    return item.Prefab;
                }
            }
            return null;
        }

        public void AddWall
        (
            EdgeVertices near, HexCellData nearCell,
            EdgeVertices far, HexCellData farCell,
            bool hasRiver, bool hasRoad
        )
        {
            if (nearCell.Walled != farCell.Walled && 
                !nearCell.IsUnderwater && 
                !farCell.IsUnderwater && 
                nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
            {
                AddWallSegment(near.v1, far.v1, near.v2, far.v2);
                if (hasRiver || hasRoad)
                {
                    AddWallCap(near.v2, far.v2);
                    AddWallCap(far.v4, near.v4);
                }
                else
                {
                    AddWallSegment(near.v2, far.v2, near.v3, far.v3);
                    AddWallSegment(near.v3, far.v3, near.v4, far.v4);
                }
                AddWallSegment(near.v4, far.v4, near.v5, far.v5);
            }
        }

        public void AddWall
        (
            Vector3 c1, HexCellData cell1,
            Vector3 c2, HexCellData cell2,
            Vector3 c3, HexCellData cell3
        )
        {
            if (cell1.Walled)
            {
                if (cell2.Walled)
                {
                    if (!cell3.Walled)
                    {
                        AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                    }
                }
                else if (cell3.Walled)
                {
                    AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
                }
                else
                {
                    AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
                }
            }
            else if (cell2.Walled)
            {
                if (cell3.Walled)
                {
                    AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
                }
                else
                {
                    AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
                }
            }
            else if (cell3.Walled)
            {
                AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
            }
        }

        private void AddWallSegment
        (
            Vector3 nearLeft, Vector3 farLeft,
            Vector3 nearRight, Vector3 farRight,
            bool addTower = false
        )
        {
            nearLeft = HexMetrics.Perturb(nearLeft);
            farLeft = HexMetrics.Perturb(farLeft);
            nearRight = HexMetrics.Perturb(nearRight);
            farRight = HexMetrics.Perturb(farRight);

            Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
            Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

            Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
            Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

            float leftTop = left.y + HexMetrics.WallHeight;
            float rightTop = right.y + HexMetrics.WallHeight;

            Vector3 v1;
            Vector3 v2;
            Vector3 v3;
            Vector3 v4;

            v1 = v3 = left - leftThicknessOffset; ;
            v2 = v4 = right - rightThicknessOffset;
            v3.y = leftTop;
            v4.y = rightTop;
            _walls.AddQuadUnperturbed(v1, v2, v3, v4);

            Vector3 t1 = v3;
            Vector3 t2 = v4;

            v1 = v3 = left + leftThicknessOffset;
            v2 = v4 = right + rightThicknessOffset;
            v3.y = leftTop;
            v4.y = rightTop;
            _walls.AddQuadUnperturbed(v2, v1, v4, v3);

            _walls.AddQuadUnperturbed(t1, t2, v3, v4);

            if (addTower)
            {
                Transform towerInstance = Instantiate(_wallTower);
                towerInstance.transform.localPosition = (left + right) * 0.5f;
                Vector3 rightDirection = right - left;
                rightDirection.y = 0f;
                towerInstance.transform.right = rightDirection;
                towerInstance.SetParent(_container, false);
            }
        }

        private void AddWallSegment
        (
            Vector3 pivot, HexCellData pivotCell,
            Vector3 left, HexCellData leftCell,
            Vector3 right, HexCellData rightCell
        )
        {
            if (pivotCell.IsUnderwater)
                return;

            bool hasLeftWall = !leftCell.IsUnderwater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
            bool hasRighWall = !rightCell.IsUnderwater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

            if (hasLeftWall)
            {
                if (hasRighWall)
                {
                    bool hasTower = false;
                    if (leftCell.Elevation == rightCell.Elevation)
                    {
                        HexHash hash = HexMetrics.SampleHashGrid((pivot + left + right) * (1f / 3f));
                        hasTower = hash.E < HexMetrics.WallTowerThreshold;
                    }
                    AddWallSegment(pivot, left, pivot, right, hasTower);
                }
                else if (leftCell.Elevation < rightCell.Elevation)
                {
                    AddWallWedge(pivot, left, right);
                }
                else
                {
                    AddWallCap(pivot, left);
                }
            }
            else if (hasRighWall)
            {
                if (rightCell.Elevation < leftCell.Elevation)
                {
                    AddWallWedge(right, pivot, left);
                }
                else
                {
                    AddWallCap(right, pivot);
                }
            }
        }

        private void AddWallCap(Vector3 near, Vector3 far)
        {
            near = HexMetrics.Perturb(near);
            far = HexMetrics.Perturb(far);

            Vector3 center = HexMetrics.WallLerp(near, far);
            Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

            Vector3 v1, v2, v3, v4;

            v1 = v3 = center - thickness;
            v2 = v4 = center + thickness;
            v3.y = v4.y = center.y + HexMetrics.WallHeight;
            _walls.AddQuadUnperturbed(v1, v2, v3, v4);
        }

        private void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
        {
            near = HexMetrics.Perturb(near);
            far = HexMetrics.Perturb(far);
            point = HexMetrics.Perturb(point);

            Vector3 center = HexMetrics.WallLerp(near, far);
            Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

            Vector3 v1, v2, v3, v4;
            Vector3 pointTop = point;
            point.y = center.y;

            v1 = v3 = center - thickness;
            v2 = v4 = center + thickness;
            v3.y = v4.y = pointTop.y = center.y + HexMetrics.WallHeight;

            _walls.AddQuadUnperturbed(v1, point, v3, pointTop);
            _walls.AddQuadUnperturbed(point, v2, pointTop, v4);
            _walls.AddTriangleUnperturbed(pointTop, v3, v4);
        }

        public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
        {
            roadCenter1 = HexMetrics.Perturb(roadCenter1);
            roadCenter2 = HexMetrics.Perturb(roadCenter2);

            Transform instance = Instantiate(_bridge);
            instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
            instance.forward = roadCenter2 - roadCenter1;
            float length = Vector3.Distance(roadCenter1, roadCenter2);
            instance.localScale = new Vector3(1f, 1f, length * (1f / HexMetrics.BridgeDesignLength));
            instance.SetParent(_container, false);
        }

        public void AddSpecialFeature(HexCellData cell, Vector3 position)
        {
            Transform prefab = PickPrefab(_structureFeatures, cell.StructureType);
            Transform instance = Instantiate(prefab);
            HexHash hash = HexMetrics.SampleHashGrid(position);
            instance.SetLocalPositionAndRotation(HexMetrics.Perturb(position), Quaternion.Euler(0f, 360f * hash.E, 0f));
            instance.SetParent(_container, false);
        }

        [Serializable]
        public struct FeatureItem<T> where T : Enum
        {
            public T FeatureType;
            public Transform Prefab;
        }
    }
}
