using Unity.Entities;
using UnityEngine;

public class NetworkVoxelNodeAuthoring : MonoBehaviour
{
    public class Baker : Baker<NetworkVoxelNodeAuthoring>
    {
        public override void Bake(NetworkVoxelNodeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            //// Добавляем unmanaged-компонент, куда сервер при спавне запишет uint хэш модели
            //AddComponent<AAA_VoxelModelRootData>(entity);
            AddComponent<AAA_VoxelModelHeader>(entity);

            AddComponent<AAA_NetworkParent>(entity);

            AddComponent<AAA_PendingNodeToRoot>(entity);
        }
    }
}
