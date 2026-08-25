//using Unity.Burst;
//using Unity.Burst.Intrinsics;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;

//[UpdateInGroup(typeof(TransformSystemGroup), OrderFirst = true)]
//// Вариант А: Выполнять строго ПОСЛЕ того, как трансформы обновились
//[UpdateAfter(typeof(LocalToWorldSystem))]
////[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)] // Только для клиента
//[BurstCompile]
//public partial struct VehicleFlatHierarchySystem : ISystem
//{
//    // ИСПРАВЛЕНО: Кэшируем лукап мировых матриц в полях системы, чтобы не создавать его в OnUpdate
//    private ComponentLookup<LocalToWorld> _localToWorldLookup;

//    private ComponentTypeHandle<LocalTransform> _localTransformHandle;
//    private ComponentTypeHandle<VehicleFlatChildData> _childDataHandle;
//    private ComponentTypeHandle<WheelVisualRotation> _wheelVisualRotationHandle;
//    private EntityQuery _query;

//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        // Инициализируем лукап один раз при создании в режиме Только для Чтения (true)
//        _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);

//        _localTransformHandle = state.GetComponentTypeHandle<LocalTransform>(false);
//        _childDataHandle = state.GetComponentTypeHandle<VehicleFlatChildData>(false);
//        _wheelVisualRotationHandle = state.GetComponentTypeHandle<WheelVisualRotation>(false);

//        _query = new EntityQueryBuilder(Allocator.Temp)
//            .WithAllRW<LocalTransform>()
//            .WithAll<VehicleFlatChildData>()
//            .Build(ref state);
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        // ИСПРАВЛЕНО: Инкрементально обновляем лукап и хэндлы типов.
//        // Это полностью убирает рантайм-варнинги валидатора о создании Lookup объектов.
//        _localToWorldLookup.Update(ref state);
//        _localTransformHandle.Update(ref state);
//        _childDataHandle.Update(ref state);
//        _wheelVisualRotationHandle.Update(ref state);

//        // Быстро собираем все сущности деталей в массивы
//        NativeArray<Entity> childEntities = _query.ToEntityArray(Allocator.TempJob);
//        NativeArray<VehicleFlatChildData> childDataArray = _query.ToComponentDataArray<VehicleFlatChildData>(Allocator.TempJob);

//        // Создаем временный словарь (HashMap) для кэширования мировых матриц родителей
//        NativeParallelHashMap<Entity, float4x4> parentMatrices = new NativeParallelHashMap<Entity, float4x4>(childEntities.Length, Allocator.TempJob);

//        // Заполняем кэш матриц на главном потоке на основе обновленного лукапа (работает мгновенно)
//        for (int i = 0; i < childDataArray.Length; i++)
//        {
//            Entity parent = childDataArray[i].ParentEntity;
//            if (!parentMatrices.ContainsKey(parent) && _localToWorldLookup.HasComponent(parent))
//            {
//                parentMatrices.TryAdd(parent, _localToWorldLookup[parent].Value);
//            }
//        }

//        var job = new SimpleFollowJob
//        {
//            ParentMatrices = parentMatrices.AsReadOnly(),
//            LocalTransformHandle = _localTransformHandle,
//            ChildDataHandle = _childDataHandle,
//            WheelVisualRotationHandle = _wheelVisualRotationHandle
//        };

//        // Запускаем последовательный джоб на Burst
//        var jobHandle = job.Schedule(_query, state.Dependency);

//        // Безопасно очищаем временную нативную память по завершению работы джоба
//        childEntities.Dispose(jobHandle);
//        childDataArray.Dispose(jobHandle);
//        parentMatrices.Dispose(jobHandle);

//        state.Dependency = jobHandle;
//    }
//}

//[BurstCompile]
//public struct SimpleFollowJob : IJobChunk
//{
//    [ReadOnly] public NativeParallelHashMap<Entity, float4x4>.ReadOnly ParentMatrices;

//    public ComponentTypeHandle<LocalTransform> LocalTransformHandle;
//    [ReadOnly] public ComponentTypeHandle<VehicleFlatChildData> ChildDataHandle;

//    //public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//    //{
//    //    var transforms = chunk.GetNativeArray(ref LocalTransformHandle);
//    //    var childDataArray = chunk.GetNativeArray(ref ChildDataHandle);

//    //    // ПРОХОД 1: Синхронизируем Башни и Колеса (Level 0, зависят от Шасси)
//    //    for (int i = 0; i < chunk.Count; i++)
//    //    {
//    //        var childData = childDataArray[i];
//    //        if (childData.HierarchyLevel != 0) continue;
//    //        if (!ParentMatrices.ContainsKey(childData.ParentVehicle)) continue;

//    //        float4x4 parentMatrix = ParentMatrices[childData.ParentVehicle];

//    //        LocalTransform t = transforms[i];
//    //        t.Position = math.transform(parentMatrix, childData.LocalOffset);
//    //        t.Rotation = math.quaternion(parentMatrix);
//    //        transforms[i] = t;
//    //    }
//    // ДОБАВЛЕНО: Хэндл для чтения углов вращения колеса
//    [ReadOnly] public ComponentTypeHandle<WheelVisualRotation> WheelVisualRotationHandle;

//    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//    {
//        var transforms = chunk.GetNativeArray(ref LocalTransformHandle);
//        var childDataArray = chunk.GetNativeArray(ref ChildDataHandle);

//        // Проверяем, есть ли в этом чанке вообще компонент вращения колес
//        bool hasRotation = chunk.Has(ref WheelVisualRotationHandle);
//        var rotations = hasRotation ? chunk.GetNativeArray(ref WheelVisualRotationHandle) : default;

//        // ПРОХОД 1: Синхронизируем Башни и Колеса (Level 0, зависят от Шасси)
//        //for (int i = 0; i < chunk.Count; i++)
//        //{
//        //    var childData = childDataArray[i];
//        //    if (childData.HierarchyLevel != 0) continue;
//        //    if (!ParentMatrices.ContainsKey(childData.ParentVehicle)) continue;

//        //    float4x4 parentMatrix = ParentMatrices[childData.ParentVehicle];

//        //    LocalTransform t = transforms[i];
//        //    t.Position = math.transform(parentMatrix, childData.LocalOffset);

//        //    // ИСПРАВЛЕНО: Если это колесо и у него есть данные вращения
//        //    if (hasRotation)
//        //    {
//        //        WheelVisualRotation wheelRot = rotations[i];

//        //        // Создаем локальное вращение колеса: 
//        //        // Сначала крутим вокруг оси X (качение), затем вокруг Y (руль)
//        //        quaternion localWheelRot = math.mul(
//        //            quaternion.AxisAngle(new float3(0, 1, 0), wheelRot.SteerAngle), // Руль
//        //            quaternion.AxisAngle(new float3(1, 0, 0), wheelRot.SpinAngle)  // Качение вперед
//        //        );

//        //        // Итоговое мировое вращение = Вращение корпуса машины * Локальное вращение колеса
//        //        t.Rotation = math.mul(math.quaternion(parentMatrix), localWheelRot);
//        //    }
//        //    else
//        //    {
//        //        // Если это деталь без вращения (например, жесткая броня или башня)
//        //        t.Rotation = math.quaternion(parentMatrix);
//        //    }

//        //    transforms[i] = t;
//        //}
//        // ПРОХОД 1: Синхронизируем Башни и Колеса (Level 0)
//        for (int i = 0; i < chunk.Count; i++)
//        {
//            var childData = childDataArray[i];
//            if (childData.HierarchyLevel != 0) continue;
//            if (!ParentMatrices.ContainsKey(childData.ParentEntity)) continue;

//            float4x4 parentMatrix = ParentMatrices[childData.ParentEntity];

//            LocalTransform t = transforms[i];
//            t.Position = math.transform(parentMatrix, childData.LocalOffset);

//            // ПРАВИЛЬНОЕ ОБЪЕДИНЕНИЕ ВРАЩЕНИЙ:
//            if (hasRotation)
//            {
//                WheelVisualRotation wheelRot = rotations[i];

//                // 1. Создаем локальное вращение колеса
//                // Математический порядок перемножения кватернионов: Справа налево!
//                // Сначала колесо крутится вокруг своей оси качения (X), 
//                // а затем весь этот узел поворачивается рулевой рейкой влево/вправо (Y)
//                quaternion localWheelRot = math.mul(
//                    quaternion.AxisAngle(new float3(0f, 1f, 0f), wheelRot.SteerAngle), // Поворот руля (Y)
//                    quaternion.AxisAngle(new float3(1f, 0f, 0f), wheelRot.SpinAngle)   // Качение (X)
//                );

//                // 2. Умножаем вращение машины на локальное вращение колеса
//                t.Rotation = math.mul(math.quaternion(parentMatrix), localWheelRot);
//            }
//            else
//            {
//                // Если это не колесо, а жесткая деталь (например, башня танка), оставляем только вращение кузова
//                t.Rotation = math.quaternion(parentMatrix);
//            }

//            transforms[i] = t;
//        }

//        // ПРОХОД 2: Синхронизируем Дула (Level 1, зависят от Башен)
//        for (int i = 0; i < chunk.Count; i++)
//        {
//            var childData = childDataArray[i];
//            if (childData.HierarchyLevel != 1) continue;
//            if (!ParentMatrices.ContainsKey(childData.ParentEntity)) continue;

//            float4x4 parentMatrix = ParentMatrices[childData.ParentEntity];

//            LocalTransform t = transforms[i];
//            t.Position = math.transform(parentMatrix, childData.LocalOffset);
//            t.Rotation = math.quaternion(parentMatrix);
//            transforms[i] = t;
//        }
//    }
//}
