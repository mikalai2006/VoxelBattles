using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(VehicleSuspensionSystem))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
[BurstCompile]
public partial struct ApplySuspensionForceSystem : ISystem
{
    private EntityQuery _wheelQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _wheelQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<VehicleSuspensionComponent, VehicleFlatChildData>()
            .Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int wheelCount = _wheelQuery.CalculateEntityCount();
        if (wheelCount == 0) return;

        // 1. ИСПРАВЛЕНО: Используем Allocator.TempJob для детерминированного времени жизни
        var wheelsSuspension = _wheelQuery.ToComponentDataArray<VehicleSuspensionComponent>(Allocator.TempJob);
        var wheelsChildData = _wheelQuery.ToComponentDataArray<VehicleFlatChildData>(Allocator.TempJob);

        // Хэш-мап для быстрой группировки: Ключ = Родитеская Машина, Значение = Индекс колеса в массиве
        var vehicleToWheelsMap = new NativeParallelMultiHashMap<Entity, int>(wheelCount, Allocator.TempJob);

        // Строим карту соответствия колес к машинам за линейное время O(N)
        for (int i = 0; i < wheelCount; i++)
        {
            vehicleToWheelsMap.Add(wheelsChildData[i].ParentEntity, i);
        }

        // 2. ИСПРАВЛЕНО: Берем правильный физический дельта-тайм из FixedStepSimulationSystemGroup
        // (или напрямую Time.DeltaTime, но внутри джоба/системы fixed-группы он должен быть фиксированным)
        float fixedDeltaTime = SystemAPI.Time.DeltaTime;

        var applyJob = new AccumulateAndApplyForcesJob
        {
            WheelsSuspension = wheelsSuspension.AsReadOnly(),
            VehicleToWheelsMap = vehicleToWheelsMap.AsReadOnly(),
            DeltaTime = fixedDeltaTime
        };

        // ТАК КАК данные сгруппированы в хэш-мап и джоб только читает из нее, 
        // теперь мы можем безопасно запускать ScheduleParallel для обработки КОРПУСОВ!
        var jobHandle = applyJob.ScheduleParallel(state.Dependency);

        // Безопасная очистка временных массивов после выполнения джоба
        wheelsSuspension.Dispose(jobHandle);
        wheelsChildData.Dispose(jobHandle);
        vehicleToWheelsMap.Dispose(jobHandle);

        state.Dependency = jobHandle;
    }
}

[BurstCompile]
[WithAll(typeof(VehicleRootTag))]
public partial struct AccumulateAndApplyForcesJob : IJobEntity
{
    [ReadOnly] public NativeArray<VehicleSuspensionComponent>.ReadOnly WheelsSuspension;
    [ReadOnly] public NativeParallelMultiHashMap<Entity, int>.ReadOnly VehicleToWheelsMap;
    [ReadOnly] public float DeltaTime;

    public void Execute(Entity chassisEntity, ref PhysicsVelocity chassisVelocity, in LocalToWorld chassisLtw, in PhysicsMass massComponent)
    {
        // 1. Точный мировой центр масс
        float3 centerOfMassWorld = chassisLtw.Position + math.mul(chassisLtw.Rotation, massComponent.CenterOfMass);

        // Временные переменные для стабилизатора (Anti-Roll Bar)
        // Для простоты разделим колеса по локальной координате X (левые и правые)
        float leftTotalCompression = 0f;
        float rightTotalCompression = 0f;
        int leftWheelsCount = 0;
        int rightWheelsCount = 0;

        // ПЕРВЫЙ ПРОХОД: Считаем разницу сжатия для стабилизатора поперечной устойчивости
        if (VehicleToWheelsMap.TryGetFirstValue(chassisEntity, out int wheelIndex, out var iterator))
        {
            do
            {
                VehicleSuspensionComponent suspension = WheelsSuspension[wheelIndex];
                // Переводим точку крепления в локальные координаты машины
                float3 localPos = math.transform(math.inverse(chassisLtw.Value), suspension.WorldApplyPosition);

                // Обычный замер: левое колесо (X < 0) или правое (X > 0)
                // Примечание: Для идеального расчета здесь нужно знать PreviousCompression из прошлого кадра,
                // но для симуляции крена достаточно оценить разницу сил.
                if (localPos.x < -0.1f)
                {
                    leftTotalCompression += (math.length(suspension.ForceToApply) > 0) ? 1f : 0f; // Упрощенный коэффициент
                    leftWheelsCount++;
                }
                else if (localPos.x > 0.1f)
                {
                    rightTotalCompression += (math.length(suspension.ForceToApply) > 0) ? 1f : 0f;
                    rightWheelsCount++;
                }
            }
            while (VehicleToWheelsMap.TryGetNextValue(out wheelIndex, ref iterator));
        }

        // Вычисляем силу стабилизатора (Anti-Roll Force)
        // Она стремится уравнять силы между левым и правым бортом, заставляя машину наклоняться при наезде ОДНИМ бортом
        float antiRollStiffness = 15000f; // Настройте жесткость стабилизатора
        float antiRollForceMagnitude = (leftTotalCompression - rightTotalCompression) * antiRollStiffness;

        // ВТОРОЙ ПРОХОД: Применяем силы и рассчитываем крутящий момент
        if (VehicleToWheelsMap.TryGetFirstValue(chassisEntity, out wheelIndex, out iterator))
        {
            do
            {
                VehicleSuspensionComponent suspension = WheelsSuspension[wheelIndex];
                if (math.all(suspension.ForceToApply == float3.zero)) continue;

                float3 localPos = math.transform(math.inverse(chassisLtw.Value), suspension.WorldApplyPosition);
                float3 finalForce = suspension.ForceToApply;

                // Добавляем влияние стабилизатора:
                // Если левый борт сжат сильнее правого, то левое колесо получает силу ВНИЗ (ослабление),
                // а правое колесо получает силу ВВЕРХ, что заставляет кузов наклоняться (подворачиваться) вслед за кочкой
                if (localPos.x < -0.1f)
                    finalForce -= chassisLtw.Up * antiRollForceMagnitude;
                else if (localPos.x > 0.1f)
                    finalForce += chassisLtw.Up * antiRollForceMagnitude;

                float3 impulseVector = finalForce * DeltaTime;

                // КРИТИЧЕСКИ ВАЖНО: Плечо рычага должно считаться строго до РЕАЛЬНОЙ точки контакта колеса с кочкой,
                // а не до абстрактной точки подвески. suspension.WorldApplyPosition должен быть hit.Position из Raycast!
                float3 leverArm = suspension.WorldApplyPosition - centerOfMassWorld;

                // 1. Линейная скорость (толкает вверх)
                chassisVelocity.Linear += impulseVector * massComponent.InverseMass;

                // 2. Угловая скорость (Вращает кузов)
                float3 torqueImpulse = math.cross(leverArm, impulseVector);

                // Проекция на тензор инерции
                float3 angularChange = math.mul(massComponent.InertiaOrientation, torqueImpulse);
                angularChange *= massComponent.InverseInertia;
                chassisVelocity.Angular += math.mul(math.inverse(massComponent.InertiaOrientation), angularChange);
            }
            while (VehicleToWheelsMap.TryGetNextValue(out wheelIndex, ref iterator));
        }

        // --- КОРРЕКЦИЯ ОГРАНИЧИТЕЛЕЙ ---

        // Ограничитель локального взлета (Анти-Ракета)
        float3 chassisUp = chassisLtw.Up;
        float localVerticalVel = math.dot(chassisVelocity.Linear, chassisUp);

        if (localVerticalVel > 6f)
        {
            float excessVelocity = localVerticalVel - 6f;
            localVerticalVel -= excessVelocity * 0.7f;

            float3 horizontalVel = chassisVelocity.Linear - (chassisUp * math.dot(chassisVelocity.Linear, chassisUp));
            chassisVelocity.Linear = horizontalVel + (chassisUp * localVerticalVel);
        }

        // ИСПРАВЛЕНО: Предыдущий демпфер (chassisVelocity.Angular *= 0.92f) тупо гасил ВСЕ вращение равномерно.
        // Из-за этого машина не могла нормально крениться (силы подвески не успевали провернуть кузов).
        // Заменим его на селективный демпфер, который гасит угловую скорость только по осям X (крен) и Z (тангаж) 
        // и только если они превышают разумные пределы, оставляя подвеске свободу для "подворачивания".

        float3 localAngularVel = math.rotate(math.inverse(chassisLtw.Rotation), chassisVelocity.Angular);

        // Слегка сглаживаем паразитное желеобразное раскачивание, не убивая физический наклон
        localAngularVel.x *= 0.96f; // Мягкое затухание наклона вперед/назад (Pitch)
        localAngularVel.z *= 0.94f; // Мягкое затухание крена влево/вправо (Roll)

        // Возвращаем угловую скорость в мировой формат
        chassisVelocity.Angular = math.rotate(chassisLtw.Rotation, localAngularVel);
    }
}



//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Physics;
//using Unity.Physics.Systems;
//using Unity.Transforms;

//[UpdateInGroup(typeof(PhysicsSystemGroup))]
//[UpdateAfter(typeof(VehicleSuspensionSystem))]
//[UpdateBefore(typeof(PhysicsSimulationGroup))]
//public partial struct ApplySuspensionForceSystem : ISystem
//{
//    private EntityQuery _wheelQuery;

//    // Отключаем Burst для OnCreate, так как создание Query на главном потоке — чисто управляемая операция
//    [BurstCompile(DisableDirectCall = true)]
//    public void OnCreate(ref SystemState state)
//    {
//        _wheelQuery = new EntityQueryBuilder(Allocator.Temp)
//            .WithAll<VehicleSuspensionComponent, VehicleFlatChildData>()
//            .Build(ref state);
//    }

//    [BurstCompile(DisableDirectCall = true)]
//    public void OnUpdate(ref SystemState state)
//    {
//        // Извлекаем массивы данных колес во временную память кадра
//        var wheelsSuspension = _wheelQuery.ToComponentDataArray<VehicleSuspensionComponent>(state.WorldUpdateAllocator);
//        var wheelsChildData = _wheelQuery.ToComponentDataArray<VehicleFlatChildData>(state.WorldUpdateAllocator);

//        // Инициализируем джобу для обработки корпусов
//        var applyJob = new AccumulateAndApplyForcesJob
//        {
//            WheelsSuspension = wheelsSuspension,
//            WheelsChildData = wheelsChildData,
//            DeltaTime = SystemAPI.Time.DeltaTime
//        };

//        // Последовательное выполнение защищает от гонки данных на PhysicsVelocity
//        state.Dependency = applyJob.Schedule(state.Dependency);
//    }
//}

//[BurstCompile]
//[WithAll(typeof(VehicleRootTag))]
//public partial struct AccumulateAndApplyForcesJob : IJobEntity
//{
//    [ReadOnly] public NativeArray<VehicleSuspensionComponent> WheelsSuspension;
//    [ReadOnly] public NativeArray<VehicleFlatChildData> WheelsChildData;
//    public float DeltaTime;

//    public void Execute(Entity chassisEntity, ref PhysicsVelocity chassisVelocity, in LocalToWorld chassisLtw, in PhysicsMass massComponent)
//    {
//        // ПРАВИЛЬНЫЙ РАСЧЕТ ЦЕНТРА МАСС В МИРОВЫХ КООРДИНАТАХ:
//        // Смещаем мировую позицию сущности на локальный центр масс, повернутый по вращению корпуса
//        float3 centerOfMassWorld = chassisLtw.Position + math.mul(chassisLtw.Rotation, massComponent.CenterOfMass);

//        // Проходим по всем колесам конкретного шасси
//        for (int i = 0; i < WheelsChildData.Length; i++)
//        {
//            if (WheelsChildData[i].ParentVehicle != chassisEntity) continue;

//            VehicleSuspensionComponent suspension = WheelsSuspension[i];

//            if (!math.all(suspension.ForceToApply == float3.zero))
//            {
//                // Переводим силу в импульс за кадр
//                float3 impulseVector = suspension.ForceToApply * DeltaTime;

//                // Находим плечо рычага: расстояние от мирового центра масс до точки крепления подвески
//                float3 leverArm = suspension.WorldApplyPosition - centerOfMassWorld;

//                // 1. Линейная скорость: толкает машину вверх
//                chassisVelocity.Linear += impulseVector * massComponent.InverseMass;

//                // 2. Угловая скорость (Крутящий момент):
//                // Векторное произведение дает перпендикулярную ось, вокруг которой должен накрениться кузов
//                float3 torqueImpulse = math.cross(leverArm, impulseVector);

//                // Переводим крутящий момент в угловую скорость с учетом тензора инерции машины
//                float3 angularChange = math.mul(massComponent.InertiaOrientation, torqueImpulse);
//                angularChange *= massComponent.InverseInertia;
//                chassisVelocity.Angular += math.mul(math.inverse(massComponent.InertiaOrientation), angularChange);
//            }
//        }

//        // Ограничитель локального взлета (Анти-Ракета)
//        float3 chassisUp = chassisLtw.Up;
//        float localVerticalVel = math.dot(chassisVelocity.Linear, chassisUp);

//        if (localVerticalVel > 6f)
//        {
//            float excessVelocity = localVerticalVel - 6f;
//            localVerticalVel -= excessVelocity * 0.7f;

//            float3 horizontalVel = chassisVelocity.Linear - (chassisUp * math.dot(chassisVelocity.Linear, chassisUp));
//            chassisVelocity.Linear = horizontalVel + (chassisUp * localVerticalVel);
//        }

//        // Гасим лишние колебания вращения (Демпфер кузова), чтобы машина не раскачивалась как желе
//        chassisVelocity.Angular *= 0.92f;
//    }
//}


