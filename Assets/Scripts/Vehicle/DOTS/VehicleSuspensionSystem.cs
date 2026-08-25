using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
[RequireMatchingQueriesForUpdate]
[BurstCompile]
public partial struct VehicleSuspensionSystem : ISystem
{
    private ComponentLookup<LocalToWorld> _ltwLookup;
    private ComponentLookup<PhysicsVelocity> _velocityLookup;
    private ComponentLookup<PhysicsMass> _massLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _ltwLookup = state.GetComponentLookup<LocalToWorld>(false);
        _velocityLookup = state.GetComponentLookup<PhysicsVelocity>(false);
        _massLookup = state.GetComponentLookup<PhysicsMass>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.PhysicsWorld.CollisionWorld;

        _ltwLookup.Update(ref state);
        _velocityLookup.Update(ref state);
        _massLookup.Update(ref state);

        var calcJob = new CalculateSuspensionJob
        {
            CollisionWorld = collisionWorld,
            LocalToWorldLookup = _ltwLookup,
            VelocityLookup = _velocityLookup,
            MassLookup = _massLookup,
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        // ИСПРАВЛЕНО: Заменяем .ScheduleParallel на .Schedule().
        // Это полностью убирает рантайм-панику бродфейса Broadphase.StaticTree.Nodes
        state.Dependency = calcJob.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct CalculateSuspensionJob : IJobEntity
{
    [ReadOnly] public CollisionWorld CollisionWorld;
    [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
    [ReadOnly] public ComponentLookup<PhysicsVelocity> VelocityLookup;
    [ReadOnly] public ComponentLookup<PhysicsMass> MassLookup;

    public float DeltaTime;

    public void Execute(ref VehicleSuspensionComponent suspension, in VehicleFlatChildData childData)
    {
        Entity chassis = childData.ParentEntity;

        suspension.ForceToApply = float3.zero;
        suspension.WorldApplyPosition = float3.zero;

        if (!LocalToWorldLookup.HasComponent(chassis) ||
            !VelocityLookup.HasComponent(chassis) ||
            !MassLookup.HasComponent(chassis)) return;

        LocalToWorld chassisLtw = LocalToWorldLookup[chassis];
        float4x4 parentMatrix = chassisLtw.Value;

        float3 chassisUp = chassisLtw.Up;

        // 1. ПЕРЕНОС ТОЧКИ СТАРТА:
        // Твоя точка childData.LocalOffset находится в ЦЕНТРЕ колеса.
        // Мы берем её и искусственно сдвигаем ВВЕРХ вдоль оси кузова на высоту подвески (RideHeight).
        // Теперь виртуальная точка крепления (днище кузова) находится в правильном месте!
        float3 wheelCenterWorld = math.transform(parentMatrix, childData.LocalOffset);
        float3 suspensionAnchorWorld = wheelCenterWorld + (chassisUp * suspension.RideHeight);

        // 2. РАСЧЕТ ПОЛНОЙ ДЛИНЫ:
        // Максимальное расстояние от днища (suspensionAnchorWorld) до земли в расслабленном состоянии
        // должно быть строго равно: Желаемая высота подвески + Радиус колеса.
        float maxSuspensionLength = suspension.RideHeight + suspension.WheelRadius;

        // Даем запас лучу в 2.0 метра вниз, чтобы он ловил землю заранее
        float rayLength = maxSuspensionLength + 2.0f;

        float3 rayStart = suspensionAnchorWorld;
        float3 rayEnd = suspensionAnchorWorld - (chassisUp * rayLength);

        RaycastInput raycastInput = new RaycastInput
        {
            Start = rayStart,
            End = rayEnd,
            Filter = new CollisionFilter
            {
                BelongsTo = 1 << 2,
                CollidesWith = 1 << 0, // Твоя категория земли (0)
                GroupIndex = 0
            }
        };

        if (CollisionWorld.CastRay(raycastInput, out RaycastHit hit))
        {
            // Дистанция от виртуального днища до земли
            float hitDistance = hit.Fraction * rayLength;

            // Величина сжатия пружины (если hitDistance меньше maxSuspensionLength, compression > 0)
            float currentCompression = maxSuspensionLength - hitDistance;

            if (currentCompression > 0f)
            {
                PhysicsVelocity chassisVelocity = VelocityLookup[chassis];
                PhysicsMass massComponent = MassLookup[chassis];

                // Рассчитываем скорость кузова именно в точке виртуального крепления подвески
                float3 worldChassisCenter = chassisLtw.Position;
                float3 wheelOffsetFromCenter = suspensionAnchorWorld - worldChassisCenter;

                float3 pointVelocity = chassisVelocity.Linear + math.cross(chassisVelocity.Angular, wheelOffsetFromCenter);
                float suspensionVelocity = math.dot(pointVelocity, chassisUp);

                float totalMass = 1.0f / massComponent.InverseMass;
                float massPerWheel = totalMass / 4f;

                // Коэффициенты жесткости и демпфирования
                float k = massPerWheel * math.pow(2f * math.PI * suspension.Frequency, 2f);
                float c = 2f * massPerWheel * (2f * math.PI * suspension.Frequency) * suspension.DampingRatio;

                // Формула силы подвески
                float springForce = (currentCompression * k) - (suspensionVelocity * c);
                if (springForce < 0f) springForce = 0f;

                // Жесткий линейный отбойник (Анти-Брюхо)
                // Если расстояние до земли стало меньше, чем радиус колеса, значит кузов лег на пузо.
                // Резко наращиваем силу, чтобы вытолкнуть машину вверх.
                if (hitDistance < suspension.WheelRadius)
                {
                    float bottomOutRatio = 1.0f - (hitDistance / math.max(0.01f, suspension.WheelRadius));
                    springForce += (totalMass * 9.81f) * bottomOutRatio * 4.0f;
                }

                // Ограничение максимальной силы, чтобы не взорвать физику (5-кратный вес машины)
                float maxForceLimit = (totalMass * 9.81f) * 5.0f;
                if (springForce > maxForceLimit) springForce = maxForceLimit;

                if (springForce > 0f)
                {
                    suspension.ForceToApply = chassisUp * springForce;
                    // Силу прикладываем в точке виртуального днища для правильного распределения веса
                    suspension.WorldApplyPosition = suspensionAnchorWorld;
                }
            }
        }

#if UNITY_EDITOR
        // Отрисовка: линия начнется НАД колесом (на уровне днища) и пойдет вниз сквозь колесо к земле
        UnityEngine.Color rayColor = !suspension.ForceToApply.Equals(float3.zero) ? UnityEngine.Color.green : UnityEngine.Color.red;
        UnityEngine.Debug.DrawLine(rayStart, hit.Position.Equals(float3.zero) ? rayEnd : hit.Position, rayColor, 2);
#endif
    }

}

