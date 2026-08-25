//using Unity.Collections;
//using Unity.Entities;

//public static class VoxelColliderRegistry
//{
//    // Сама хэш-карта теперь живет здесь
//    public static NativeParallelHashMap<Entity, VoxelColliderCleanupMarker> TrackedColliders;

//    // Флаг, чтобы системы знали, можно ли сейчас работать с картой
//    public static bool IsCreated => TrackedColliders.IsCreated;

//    public static void Initialize()
//    {
//        if (!TrackedColliders.IsCreated)
//        {
//            TrackedColliders = new NativeParallelHashMap<Entity, VoxelColliderCleanupMarker>(64, Allocator.Persistent);
//        }
//    }

//    public static void Shutdown()
//    {
//        if (TrackedColliders.IsCreated)
//        {
//            // Жестко и гарантированно чистим всю нативную память
//            foreach (var kvp in TrackedColliders)
//            {
//                var group = kvp.Value;
//                //for (int i = 0; i < group.ChildBlobs.Length; i++)
//                //{
//                //    if (group.ChildBlobs[i].IsCreated) group.ChildBlobs[i].Dispose();
//                //}
//                if (group.ColliderBlob.IsCreated) group.ColliderBlob.Dispose();
//            }

//            TrackedColliders.Dispose();
//        }
//    }
//}
