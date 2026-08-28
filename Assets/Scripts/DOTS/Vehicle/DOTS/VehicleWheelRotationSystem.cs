//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Physics;
//using Unity.Transforms;

//[UpdateInGroup(typeof(TransformSystemGroup), OrderFirst = true)]
//[UpdateBefore(typeof(VehicleFlatHierarchySystem))]
//public partial struct VehicleWheelRotationSystem : ISystem
//{
//    private EntityQuery _vehicleQuery;
//    private EntityQuery _wheelQuery;

//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        _vehicleQuery = new EntityQueryBuilder(Allocator.Temp)
//            .WithAll<PhysicsVelocity, LocalToWorld>()
//            .Build(ref state);

//        _wheelQuery = new EntityQueryBuilder(Allocator.Temp)
//            .WithAllRW<WheelVisualRotation>()
//            .WithAll<VehicleSuspensionComponent, VehicleFlatChildData>()
//            .Build(ref state);
//    }

//    // Отключаем Burst для OnUpdate, чтобы работать с управляемым API новой системы ввода
//    [BurstCompile(DisableDirectCall = true)]
//    public void OnUpdate(ref SystemState state)
//    {
//        int vehicleCount = _vehicleQuery.CalculateEntityCount();
//        int wheelCount = _wheelQuery.CalculateEntityCount();
//        if (vehicleCount == 0 || wheelCount == 0) return;

//        // 1. БЕЗОПАСНЫЙ СБОР ВВОДА ЧЕРЕЗ НОВЫЙ INPUT SYSTEM
//        float currentSteerInput = 0f;

//        // Получаем текущую активную клавиатуру
//        var keyboard = UnityEngine.InputSystem.Keyboard.current;
//        if (keyboard != null)
//        {
//            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) currentSteerInput = 1f;
//            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) currentSteerInput = -1f;
//        }

//        // 2. Сбор данных машин и колес в массивы (ваш старый рабочий код)
//        var vehicles = _vehicleQuery.ToEntityArray(Allocator.TempJob);
//        var velocities = _vehicleQuery.ToComponentDataArray<PhysicsVelocity>(Allocator.TempJob);


//        // Примечание: Если у вас ввод идет через Action Asset новой системы ввода, 
//        // вы можете просто подставить сюда вашу переменную: currentSteerInput = moveAction.ReadValue<Vector2>().x;

//        // 2. Сбор данных машин и колес в массивы
//        //var vehicles = _vehicleQuery.ToEntityArray(Allocator.TempJob);
//        //var velocities = _vehicleQuery.ToComponentDataArray<PhysicsVelocity>(Allocator.TempJob);
//        var ltws = _vehicleQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

//        var visualArray = _wheelQuery.ToComponentDataArray<WheelVisualRotation>(Allocator.TempJob);
//        var suspensionArray = _wheelQuery.ToComponentDataArray<VehicleSuspensionComponent>(Allocator.TempJob);
//        var childDataArray = _wheelQuery.ToComponentDataArray<VehicleFlatChildData>(Allocator.TempJob);

//        var wheelSpeeds = new NativeArray<VehicleSpeedData>(wheelCount, Allocator.TempJob);

//        for (int i = 0; i < wheelCount; i++)
//        {
//            Entity parent = childDataArray[i].ParentEntity;
//            VehicleSpeedData speedData = default;

//            for (int j = 0; j < vehicleCount; j++)
//            {
//                if (vehicles[j] == parent)
//                {
//                    speedData.LinearVelocity = velocities[j].Linear;
//                    speedData.ForwardDirection = ltws[j].Forward;
//                    break;
//                }
//            }

//            wheelSpeeds[i] = speedData;
//        }

//        // 3. ПЕРЕДАЕМ ИНПУТ В ДЖОБ
//        var wheelJob = new SafeParallelWheelJob
//        {
//            VisualArray = visualArray,
//            SuspensionArray = suspensionArray.AsReadOnly(),
//            WheelSpeeds = wheelSpeeds.AsReadOnly(),
//            SteerInput = currentSteerInput, // Передали готовое число, джоб больше не лезет в движок за инпутом
//            DeltaTime = SystemAPI.Time.DeltaTime
//        };

//        var jobHandle = wheelJob.Schedule(wheelCount, 16, state.Dependency);

//        jobHandle.Complete();
//        _wheelQuery.CopyFromComponentDataArray(visualArray);

//        // Очистка памяти
//        vehicles.Dispose();
//        velocities.Dispose();
//        ltws.Dispose();
//        visualArray.Dispose();
//        suspensionArray.Dispose();
//        childDataArray.Dispose();
//        wheelSpeeds.Dispose();

//        state.Dependency = jobHandle;
//    }
//}

//public struct VehicleSpeedData
//{
//    public float3 LinearVelocity;
//    public float3 ForwardDirection;
//}

//[BurstCompile]
//public struct SafeParallelWheelJob : IJobParallelFor
//{
//    public NativeArray<WheelVisualRotation> VisualArray;
//    [ReadOnly] public NativeArray<VehicleSuspensionComponent>.ReadOnly SuspensionArray;
//    [ReadOnly] public NativeArray<VehicleSpeedData>.ReadOnly WheelSpeeds;

//    // Принимаем готовое значение ввода
//    public float SteerInput;
//    public float DeltaTime;

//    public void Execute(int index)
//    {
//        VehicleSpeedData vehicleData = WheelSpeeds[index];
//        VehicleSuspensionComponent suspension = SuspensionArray[index];
//        WheelVisualRotation visual = VisualArray[index];

//        // 1. Расчет КАТЕНИЯ колеса вперед/назад
//        float forwardSpeed = math.dot(vehicleData.LinearVelocity, vehicleData.ForwardDirection);
//        float radius = math.max(0.05f, suspension.WheelRadius);
//        float deltaSpin = (forwardSpeed * DeltaTime) / radius;
//        visual.SpinAngle = (visual.SpinAngle + deltaSpin) % (2f * math.PI);

//        // 2. Расчет ПОВОРОТА РУЛЯ (Steering) из готового безопасного инпута
//        if (visual.IsSteerable)
//        {
//            float maxSteerAngle = 0.6f; // ~35 градусов максимального поворота
//            float targetSteer = SteerInput * maxSteerAngle;

//            float steerSpeed = 4.0f; // Плавность поворота колес
//            visual.SteerAngle = math.lerp(visual.SteerAngle, targetSteer, DeltaTime * steerSpeed);
//        }
//        else
//        {
//            visual.SteerAngle = 0f;
//        }

//        VisualArray[index] = visual;
//    }
//}


////using Unity.Burst;
////using Unity.Collections;
////using Unity.Entities;
////using Unity.Mathematics;
////using Unity.Physics;
////using Unity.Transforms;

////[UpdateInGroup(typeof(TransformSystemGroup), OrderFirst = true)]
////[UpdateBefore(typeof(VehicleFlatHierarchySystem))]
////[BurstCompile]
////public partial struct VehicleWheelRotationSystem : ISystem
////{
////    private EntityQuery _vehicleQuery;

////    [BurstCompile]
////    public void OnCreate(ref SystemState state)
////    {
////        // Запрос для сбора данных только с родительских машин
////        _vehicleQuery = new EntityQueryBuilder(Allocator.Temp)
////            .WithAll<PhysicsVelocity, LocalToWorld>()
////            .Build(ref state);
////    }

////    [BurstCompile]
////    public void OnUpdate(ref SystemState state)
////    {
////        int vehicleCount = _vehicleQuery.CalculateEntityCount();
////        if (vehicleCount == 0) return;

////        // 1. Создаем структуру данных для хранения информации о машине
////        // Выделяем память типа TempJob — она живет строго до конца работы джоба
////        var vehicleDataMap = new NativeParallelHashMap<Entity, VehicleSpeedData>(vehicleCount, Allocator.TempJob);

////        // 2. БЕЗОПАСНЫЙ СБОР ДАННЫХ: Собираем скорости машин на главном потоке.
////        // Это работает мгновенно и гарантирует отсутствие Race Conditions.
////        foreach (var (velocity, ltw, entity) in
////                 SystemAPI.Query<PhysicsVelocity, LocalToWorld>().WithEntityAccess())
////        {
////            vehicleDataMap.TryAdd(entity, new VehicleSpeedData
////            {
////                LinearVelocity = velocity.Linear,
////                ForwardDirection = ltw.Forward
////            });
////        }

////        // 3. ЗАПУСК ПАРАЛЛЕЛЬНОГО ДЖОБА КОЛЕС:
////        // Теперь колеса параллельно читают из изолированной карты, что на 100% безопасно.
////        var wheelJob = new SafeWheelRotationJob
////        {
////            VehicleDataMap = vehicleDataMap.AsReadOnly(),
////            DeltaTime = SystemAPI.Time.DeltaTime
////        };

////        var jobHandle = wheelJob.ScheduleParallel(state.Dependency);

////        // Безопасно освобождаем память словаря сразу после того, как джоб закончит работу
////        vehicleDataMap.Dispose(jobHandle);

////        state.Dependency = jobHandle;
////    }
////}

////// Легковесная структура для передачи данных между машиной и колесом
////public struct VehicleSpeedData
////{
////    public float3 LinearVelocity;
////    public float3 ForwardDirection;
////}

////[BurstCompile]
////public partial struct SafeWheelRotationJob : IJobEntity
////{
////    // Атрибут [ReadOnly] здесь работает идеально, так как NativeParallelHashMap поддерживает 
////    // параллельное чтение из множества потоков без вызова ошибок валидатора.
////    [ReadOnly] public NativeParallelHashMap<Entity, VehicleSpeedData>.ReadOnly VehicleDataMap;
////    public float DeltaTime;

////    private void Execute(ref WheelVisualRotation visual, in VehicleSuspensionComponent suspension, in VehicleFlatChildData childData)
////    {
////        Entity chassis = childData.ParentVehicle;

////        // Безопасно извлекаем данные о скорости нужной машины из карты
////        if (!VehicleDataMap.TryGetValue(chassis, out VehicleSpeedData vehicleData)) return;

////        // Вычисляем скорость машины вдоль её продольной оси (вперед/назад)
////        float forwardSpeed = math.dot(vehicleData.LinearVelocity, vehicleData.ForwardDirection);
////        float radius = math.max(0.05f, suspension.WheelRadius);

////        // На сколько радиан прокрутилось колесо за кадр
////        float deltaSpin = (forwardSpeed * DeltaTime) / radius;

////        // Обновляем угол и зацикливаем его в пределах 0...2*PI
////        visual.SpinAngle = (visual.SpinAngle + deltaSpin) % (2f * math.PI);
////    }
////}

