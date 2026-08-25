using System.Collections.Generic;
using Mikalai2006.VoxelBase;
using UnityEngine;

namespace Mikalai2006.VoxelMap {
    public class Tile3DGroup : MonoBehaviour, IHealthed
    {
        [SerializeField] List<Tile3D> tiles;
        public List<Tile3D> Tiles => tiles;
        //[SerializeField] protected VoxelMeshRender[] voxelMeshRenders;
        [SerializeField] protected List<VoxelMeshRender> voxelMeshRenders;
        [SerializeField] protected ContainerData containerData;

        void Awake()
        {
            tiles = new List<Tile3D>();
            voxelMeshRenders = new List<VoxelMeshRender>();
        }

        void RefreshChildrenVoxelMeshRenderer()
        {
            voxelMeshRenders.Clear();

            //List<VoxelMeshRender> _temp = new List<VoxelMeshRender>();

            foreach (var tile in tiles)
            {
                VoxelMeshRender[] vms = tile.transform.GetComponentsInChildren<VoxelMeshRender>();

                //_temp.AddRange(vms);
                voxelMeshRenders.AddRange(vms);
            }

            //voxelMeshRenders = _temp.ToArray();

        }

        public void AddTile(Tile3D tile)
        {
            tiles.Add(tile);

            RefreshChildrenVoxelMeshRenderer();
        }

        public void RemoveTile(Tile3D tile)
        {
            tiles.Remove(tile);

            RefreshChildrenVoxelMeshRenderer();
        }

        public void RefreshHP()
        {
            // анализируем данные о разрушениях и обновляем данные уровня здоровья группы объектов.
            var result = new ContainerData();

            foreach (var vm in voxelMeshRenders)
            {
                if (vm.Containers != null)
                {
                    for (int i = 0; i < vm.Containers.Length; i++)
                    {
                        result.countVoxelsDestructible += vm.Containers[i].ContainerData.countVoxelsDestructible;
                        result.countVoxels += vm.Containers[i].ContainerData.countVoxels;
                    }
                }
            }

            result.levelDestruction = (float)result.countVoxelsDestructible / result.countVoxels;

            containerData = result;

            CheckDestroyBottomTiles();
        }

        private void CheckDestroyBottomTiles()
        {
            // float startTime = Time.realtimeSinceStartup;
            // var destroyTiles = tiles.Where(t => !t.isActive);

            // for (int i = 0; i < destroyTiles.Count(); i++)
            // {
            //     var oneColumnTiles = tiles.Where(t => 
            //         t.transform.localPosition.x == destroyTiles.ElementAt(i).transform.localPosition.x &&
            //         t.transform.localPosition.z == destroyTiles.ElementAt(i).transform.localPosition.z
            //     );
            //     for (int j = 0; j < oneColumnTiles.Count(); j++)
            //     {
            //         Vector3Int cacheLocalPosition = Vector3Int.RoundToInt(oneColumnTiles.ElementAt(j).transform.localPosition);
            //         oneColumnTiles.ElementAt(j).transform.localPosition =
            //             new Vector3Int(cacheLocalPosition.x, cacheLocalPosition.y - 1, cacheLocalPosition.z);
            //     }
            // }
            // Debug.Log($"Time CheckDestroyBottomTiles: {(Time.realtimeSinceStartup - startTime) * 1000f}.");

        }

        public void OnSaveDestroyVoxels(List<RemoveVoxel> voxels, DataDetail dataDetail)
        {
            //Debug.LogWarning("OnSaveDestroyVoxels не реализована!");
        }
    }
}