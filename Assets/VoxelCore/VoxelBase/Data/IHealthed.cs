using System.Collections.Generic;

namespace Mikalai2006.VoxelBase
{
    public interface IHealthed
    {
        /// <summary>
        /// Проводит опрос всех дочерних объектов Container на уровень разрушения.
        /// </summary>
        void RefreshHP();
        void OnSaveDestroyVoxels(List<RemoveVoxel> voxels, DataDetail dataDetail);
    };
}