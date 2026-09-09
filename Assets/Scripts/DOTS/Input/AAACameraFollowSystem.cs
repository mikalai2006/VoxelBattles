using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[BurstCompile]
public partial struct AAACameraFollowSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Система засыпает и не тратит ресурсы, пока MonoBehaviour-бридж 
        // не создаст синглтон AAA_CameraSettingsSingleton в клиентском мире
        state.RequireForUpdate<AAA_CameraSettingsSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Находим локального игрока с активным тегом управления
        foreach (var (transform, isControlled, isLocalOwner) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRO<AAA_IsControlledTag>, EnabledRefRO<GhostOwnerIsLocal>>())
        {
            // Если тег управления или владения выключен — пропускаем сущность
            if (!isLocalOwner.ValueRO || isControlled.ValueRO.IsActive == false) continue;

            // Получаем доступ на чтение и запись к синглтону настроек камеры
            var cameraSettings = SystemAPI.GetSingletonRW<AAA_CameraSettingsSingleton>();

            // Записываем целевую позицию игрока (MonoBehaviour-бридж сам применит Offset и плавный Lerp)
            cameraSettings.ValueRW.TargetPosition = transform.ValueRO.Position;

            // Локальный игрок на клиенте всегда один. Нашли — выходим из цикла
            break;
        }
    }
}


//using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.NetCode;
//using Unity.Transforms;

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//[UpdateInGroup(typeof(PresentationSystemGroup))]
//[BurstCompile]
//public partial struct AAACameraFollowSystem : ISystem
//{
//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<AAA_CameraSettingsSingleton>();
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        // 1. Итерируемся по всем сущностям, у которых есть IsControlledTag и LocalTransform
//        // (Поскольку компонент Enableable, используем EnabledRefRO для проверки активности)
//        foreach (var (transform, isControlledEnabled, isLocalOwner) in
//            SystemAPI.Query<RefRO<LocalTransform>, EnabledRefRO<IsControlledTag>, EnabledRefRO<GhostOwnerIsLocal>>())
//        {
//            // Проверяем, что тег управления включен И сущность принадлежит локальному клиенту
//            if (!isControlledEnabled.ValueRO || !isLocalOwner.ValueRO) continue;

//            // Если тег выключен у этой сущности, пропускаем её
//            if (!isControlledEnabled.ValueRO) continue;

//            // 2. Создаем и планируем Job для найденной сущности/клиента
//            var job = new CameraFollowJob
//            {
//                TargetPosition = transform.ValueRO.Position
//            };

//            state.Dependency = job.Schedule(state.Dependency);
//        }
//    }


//    //[BurstCompile]
//    //public void OnUpdate(ref SystemState state)
//    //{
//    //    // Если процедурный игрок еще не создан, просто пропускаем кадр
//    //    if (!SystemAPI.HasSingleton<IsControlledTag>()) return;

//    //    Entity targetEntity = SystemAPI.GetSingletonEntity<IsControlledTag>();
//    //    LocalTransform targetTransform = SystemAPI.GetComponent<LocalTransform>(targetEntity);

//    //    // Передаем текущую зависимость кадра, чтобы выстроить правильную очередь Job'ов
//    //    state.Dependency = new CameraFollowJob
//    //    {
//    //        TargetPosition = targetTransform.Position
//    //    }.Schedule(state.Dependency);
//    //}

//    [BurstCompile]
//    private partial struct CameraFollowJob : IJobEntity
//    {
//        public float3 TargetPosition;

//        public void Execute(ref AAA_CameraSettingsSingleton cameraSettings)
//        {
//            cameraSettings.TargetPosition = TargetPosition + cameraSettings.Offset;
//        }
//    }
//}



////using Unity.Burst;
////using Unity.Entities;
////using Unity.Transforms;

//////[UpdateInGroup(typeof(TransformSystemGroup))]

////// Переносим камеру в PresentationSystemGroup. 
////// Она гарантированно выполняется ПОСЛЕ FixedStepSimulationSystemGroup (физики) 
////// и ПОСЛЕ TransformSystemGroup (расчета матриц позиций).
////[UpdateInGroup(typeof(PresentationSystemGroup))]
////[UpdateAfter(typeof(AAAMovementSystem))]
////[BurstCompile]
////public partial struct AAACameraFollowSystem : ISystem
////{
////    private EntityQuery _cameraQuery;
////    private EntityQuery _targetQuery;

////    [BurstCompile]
////    public void OnCreate(ref SystemState state)
////    {
////        _cameraQuery = state.GetEntityQuery(ComponentType.ReadWrite<AAA_CameraSettingsSingleton>());

////        var queryBuilder = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
////            .WithAll<LocalTransform>()
////            .WithAll<IsControlledTag>();

////        _targetQuery = state.GetEntityQuery(queryBuilder);
////        queryBuilder.Dispose();

////        state.RequireForUpdate(_cameraQuery);
////    }

////    [BurstCompile]
////    public void OnUpdate(ref SystemState state)
////    {
////        if (_targetQuery.IsEmpty) return;

////        // Теперь чтение абсолютно безопасно
////        var targetTransform = _targetQuery.GetSingleton<LocalTransform>();
////        var cameraSettings = _cameraQuery.GetSingleton<AAA_CameraSettingsSingleton>();

////        // Рассчитываем целевую позицию для камеры
////        cameraSettings.TargetPosition = targetTransform.Position + cameraSettings.Offset;

////        // Записываем обратно
////        _cameraQuery.SetSingleton(cameraSettings);
////    }
////}
