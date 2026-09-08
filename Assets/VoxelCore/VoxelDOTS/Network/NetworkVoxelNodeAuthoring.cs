using Unity.Entities;
using UnityEngine;

public class NetworkVoxelNodeAuthoring : MonoBehaviour
{
    public class Baker : Baker<NetworkVoxelNodeAuthoring>
    {
        public override void Bake(NetworkVoxelNodeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<ChunkIndexComponent>(entity);

            AddComponent<VoxelModelHeader>(entity);

            AddComponent<NetworkParent>(entity);

            AddComponent<PendingNodeToRoot>(entity);
        }
    }
}
