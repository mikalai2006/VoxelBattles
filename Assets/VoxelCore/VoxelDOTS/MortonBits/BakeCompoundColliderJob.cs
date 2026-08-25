//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Physics;

//[BurstCompile]
//public struct BakeCompoundColliderJob : IJob
//{
//    // Принимаем плоский массив блобов от всех чанков автомобиля
//    [ReadOnly] public NativeArray<BlobAssetReference<Collider>> ChildChunkColliders;
//    [ReadOnly] public NativeArray<float3> ChildLocalOffsets;

//    // Выходной массив на 1 элемент для корневого составного коллайдера
//    public NativeArray<BlobAssetReference<Collider>> OutputRootCollider;

//    [BurstCompile]
//    public void Execute()
//    {
//        int validCollidersCount = 0;

//        // Считаем, сколько чанков реально смогли выпечь геометрию
//        for (int i = 0; i < ChildChunkColliders.Length; i++)
//        {
//            if (ChildChunkColliders[i].IsCreated)
//            {
//                validCollidersCount++;
//            }
//        }

//        if (validCollidersCount == 0) return;

//        // Выделяем память под массив составных частей коллайдера (Используем точный тип со скриншота!)
//        var compoundInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(validCollidersCount, Allocator.Temp);
//        int currentInstanceIdx = 0;

//        for (int i = 0; i < ChildChunkColliders.Length; i++)
//        {
//            if (!ChildChunkColliders[i].IsCreated) continue;

//            // ====================================================================
//            // ЖЕЛЕЗОБЕТОННЫЙ AAA-ФИКС ТИПОВ ДАННЫХ ДЛЯ UNITY 6
//            // Принудительно кастуем BlobAssetReference к ожидаемому структурой типу
//            // ====================================================================
//            compoundInstances[currentInstanceIdx] = new CompoundCollider.ColliderBlobInstance
//            {
//                // Поле в Unity 6 может называться Collider или ColliderRef в зависимости от минорного патча.
//                // Если Collider выдаст ошибку, просто допишите Ref: ColliderRef = ...
//                Collider = (BlobAssetReference<Collider>)ChildChunkColliders[i],
//                CompoundFromChild = new RigidTransform(quaternion.identity, ChildLocalOffsets[i])
//            };
//            // ====================================================================

//            currentInstanceIdx++;
//        }

//        // Атомарно выпекаем финальный CompoundCollider
//        OutputRootCollider[0] = CompoundCollider.Create(compoundInstances);
//        compoundInstances.Dispose();
//    }
//}
