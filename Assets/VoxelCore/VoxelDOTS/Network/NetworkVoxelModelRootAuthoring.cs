using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class NetworkVoxelModelRootAuthoring : MonoBehaviour
{
    public class Baker : Baker<NetworkVoxelModelRootAuthoring>
    {
        public override void Bake(NetworkVoxelModelRootAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // 1. Маркируем сущность как сетевой корень воксельного объекта
            AddComponent<VoxelModelRootTag>(entity);

            // 2. Добавляем unmanaged-компонент, куда сервер при спавне запишет uint хэш модели
            AddComponent<VoxelModelRootData>(entity);

            AddComponent<AAA_InputComponent>(entity);

            AddComponent<AAA_MovementComponent>(entity);

            // ВАЖНО: Буфер LinkedEntityGroup для иерархии репликации Netcode 
            // добавит на этот префаб автоматически, когда вы настроите его как Ghost в инспекторе.

            // 1. ЖЕСТКО ДОБАВЛЯЕМ КОМПОНЕНТ ПРИ ЗАПЕКАНИИ. 
            // Запекаем компонент в архетип префаба со значением false по умолчанию
            AddComponent(entity, new IsControlledTag { IsActive = false });


            ////===============Physics==========================================
            //// Создаем крошечную пустую сферу, чтобы компонент PhysicsCollider был валидным
            //var sphereGeometry = new SphereGeometry
            //{
            //    Center = float3.zero,
            //    Radius = 0.05f // Сделаем чуть меньше, чтобы она не мешала визуально при спавне
            //};
            //var defaultColliderBlob = Unity.Physics.SphereCollider.Create(sphereGeometry, CollisionFilter.Default);

            //// ВНИМАНИЕ: ДОБАВЬТЕ ЭТУ СТРОКУ СРАЗУ ПОСЛЕ CREATE!
            //// Этот метод регистрирует BlobAsset в системе сборки мусора инкрементального запекания Unity.
            //// Теперь при каждом изменении префаба редактор сам корректно вычистит старую сферу.
            //AddBlobAsset(ref defaultColliderBlob, out _);

            //AddComponent(entity, new PhysicsCollider { Value = defaultColliderBlob });

            //// ИСПРАВЛЕНИЕ: Рассчитываем правильную начальную массу на основе этой сферы!
            //// Метод CreateDynamic создаст валидные, не нулевые значения InertiaTensor и InverseMass.
            //// 1000.0f — это целевой вес машины (или задайте любой стандартный вес).
            //var defaultMassProperties = defaultColliderBlob.Value.MassProperties;
            //var initialPhysicsMass = PhysicsMass.CreateDynamic(defaultMassProperties, 1000.0f);
            //initialPhysicsMass.CenterOfMass = float3.zero;

            //// Запекаем ЖЕСТКО валидную массу вместо пустых нулей:
            //AddComponent(entity, initialPhysicsMass);

            //// Скорость оставляем пустой (нули для скорости безопасны)
            //AddComponent(entity, new PhysicsVelocity());

            //AddSharedComponent(entity, new PhysicsWorldIndex { Value = 0 });
            ////================================================================

            //===================Physics==========================================
            // Мы НЕ вызываем SphereCollider.Create() и НЕ выделяем неуправляемую память в редакторе!
            // Пустой дефолтный PhysicsCollider (Null-ссылка) абсолютно безопасен для архетипа.
            AddComponent(entity, new PhysicsCollider { Value = default });

            // Задаем валидную начальную массу динамического тела вручную.
            // Это полностью защищает LocalTransform от NaN при первом спавне машины на клиенте.
            var safeInitialMass = new PhysicsMass
            {
                Transform = RigidTransform.identity,
                InverseMass = 0,//1.0f / 1000.0f, // Задаем условный вес в 1000 кг (1 / масса)
                InverseInertia = new float3(1.0f, 1.0f, 1.0f), // Безопасный единичный тензор инерции, защищающий от деления на ноль
                CenterOfMass = float3.zero
            };

            AddComponent(entity, safeInitialMass);

            // Скорость оставляем пустой
            AddComponent(entity, new PhysicsVelocity());

            // Фиксируем индекс физического мира
            AddSharedComponent(entity, new PhysicsWorldIndex { Value = 0 });
            //====================================================================

            //AddComponent(entity, new VoxelColliderCleanupMarker
            //{
            //    //ColliderBlob = defaultColliderBlob,
            //});
        }
    }
}
