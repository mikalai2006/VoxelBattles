using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode; // Не забываем директиву
using Unity.Physics;
using Unity.Transforms;

// ====================================================================
// ЖЕЛЕЗОБЕТОННЫЙ ФИКС СОРТИРОВКИ:
// Помещаем систему движения прямо внутрь главной PhysicsSystemGroup.
// Теперь мы находимся на одном поле с физическим ядром, и атрибут UpdateBefore
// легально сможет выстроить порядок выполнения без варнингов в консоли!
// ====================================================================
//[UpdateInGroup(typeof(PhysicsSystemGroup))]
//[UpdateBefore(typeof(PhysicsSimulationGroup))] // Выполняем строго ДО фазы обсчета коллизий
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct AAAMovementSystem : ISystem
{
    // Объявляем unmanaged lookup-кэш компонентов для чтения флага активности
    private ComponentLookup<AAA_IsControlledTag> _controlTagLookup;


    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AAA_MovementComponent>();
        state.RequireForUpdate<NetworkTime>();

        //// Теперь система движения не начнет крутить джобы, 
        //// пока в ECS-мире не инициализируется хотя бы один физический индекс!
        //state.RequireForUpdate<PhysicsWorldIndex>();

        // Инициализируем хранилище lookup при создании системы
        _controlTagLookup = state.GetComponentLookup<AAA_IsControlledTag>(true); // true = ReadOnly

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // КРИТИЧЕСКИ ВАЖНО ДЛЯ BURST: Обновляем lookup-данные актуальным состоянием памяти ТЕКУЩЕГО кадра
        _controlTagLookup.Update(ref state);

        float frameDeltaTime = SystemAPI.Time.DeltaTime;

        // В вашей версии Netcode просто берем синглтон времени симуляции
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();

        // В зависимости от того, где выполняется логика (клиент/сервер), 
        // Netcode рекомендует использовать текущий PredictingTick
        var currentTick = networkTime.ServerTick;

        state.Dependency = new MovementParallelJob
        {
            FixedDeltaTime = frameDeltaTime,
            CurrentTick = currentTick,

            ControlTagLookup = _controlTagLookup
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    private partial struct MovementParallelJob : IJobEntity
    {
        public float FixedDeltaTime;
        public NetworkTick CurrentTick;

        // Внедряем кэш внутрь джобы с атрибутом ReadOnly
        [ReadOnly] public ComponentLookup<AAA_IsControlledTag> ControlTagLookup;

        public void Execute(
            Entity entity,
            GhostInstance ghostInstance,
            ref PhysicsVelocity velocity,
            ref AAA_MovementComponent movement,
            ref LocalTransform transform,
            in DynamicBuffer<InputBufferData<AAA_InputComponent>> inputBuffer,
            in PhysicsMass mass)
        {
            // БЕЗОПАСНАЯ И АСИНХРОННАЯ ПРОВЕРКА ДЛЯ NETCODE:
            // Читаем сетевой флаг из lookup по текущей Entity без вызова структурных конфликтов
            bool isCurrentVehicleActive = ControlTagLookup.HasComponent(entity) && ControlTagLookup[entity].IsActive;
            //bool isCurrentVehicleActive = controlTag.IsActive;
            if (!isCurrentVehicleActive || mass.InverseMass == 0f)
            {
                //velocity.Angular = float3.zero;
                //velocity.Linear = float3.zero;
                return;
            }

            InputBufferData<AAA_InputComponent> bufferWrapper;
            if (!inputBuffer.GetDataAtTick(CurrentTick, out bufferWrapper))
            {
                bufferWrapper = default;
            }
            AAA_InputComponent inputData = bufferWrapper.InternalInput;

            //if (math.all(inputData.MoveInput == float2.zero)) { return; }

            // ====================================================================
            // 1. АНТИ-ХАОС ФИКС: ЗАПРЕЩАЕМ ФИЗИКЕ КРУТИТЬ КУЗОВ
            // ====================================================================
            // Мгновенно обнуляем угловую скорость. Земля и кочки больше НЕ смогут 
            // закрутить машину юлой или перевернуть её. Вращением теперь рулит ТОЛЬКО наш slerp.
            velocity.Angular = float3.zero;

            float3 _velocity = velocity.Linear;

            // Защита от сильных сетевых прыжков вверх при рассинхронах Netcode
            if (velocity.Linear.y > 3.0f)
            {
                _velocity.y = 3.0f;
            }

            // ====================================================================
            // 2. РАСЧЕТ ЛИНЕЙНОЙ СКОРОСТИ (ВАША ЛОГИКА БЕЗ ИЗМЕНЕНИЙ)
            // ====================================================================
            //float3 targetDirection = new float3(inputData.MoveInput.x, 0, inputData.MoveInput.y);

            // Распаковываем 1 байт битовой маски в чистый float2 вектор
            float2 moveVector = VoxelInputPackingUtility.UnpackBitsToFloat2(inputData.ButtonsMask);

            // Идеально собираем финальный 3D-вектор направления движения машины
            float3 targetDirection = new float3(moveVector.x, 0, moveVector.y);

            if (math.lengthsq(targetDirection) > 1f)
            {
                targetDirection = math.normalize(targetDirection);
            }

            float3 targetVelocity = targetDirection * movement.MaxSpeed;
            float3 currentHorizontalVelocity = new float3(velocity.Linear.x, 0f, velocity.Linear.z);
            float3 velocityChange = targetVelocity - currentHorizontalVelocity;

            float rate = math.lengthsq(targetDirection) > 0.001f ? movement.Acceleration : movement.Deceleration;
            float3 velocityStep = velocityChange * rate * FixedDeltaTime;

            if (math.lengthsq(velocityStep) > math.lengthsq(velocityChange))
            {
                velocityStep = velocityChange;
            }

            _velocity.x += velocityStep.x;
            _velocity.z += velocityStep.z;

            ////if (velocity.Linear.x < 0.01f && velocity.Linear.x > -0.0000001f) velocity.Linear.x = 0f;
            ////if (velocity.Linear.y < 0.01f && velocity.Linear.y > -0.0000001f) velocity.Linear.y = 0f;
            ////if (velocity.Linear.z < 0.01f && velocity.Linear.z > -0.0000001f) velocity.Linear.z = 0f;
            //// 1. Порог мертвой зоны (0.02f — идеальный баланс между точностью и сетью)
            //const float velocityDeadzone = 0.002f;

            //// 2. Проверяем линейную скорость (Linear Velocity)
            //// Если длина вектора в квадрате меньше порога — жестко обнуляем
            //if (math.lengthsq(velocity.Linear) < (velocityDeadzone * velocityDeadzone))
            //{
            //    _velocity = float3.zero;
            //}

            velocity.Linear = _velocity;
            //// 3. Проверяем угловую скорость (Angular Velocity)
            //// Микро-покачивания машины на неровностях сетки вокселей тоже спамят в сеть!
            //if (math.lengthsq(velocity.Angular) < (velocityDeadzone * velocityDeadzone))
            //{
            //    velocity.Angular = float3.zero;
            //}

            // ====================================================================
            // 3. ПРАВИЛЬНЫЙ И БЕЗОПАСНЫЙ ПОВОРОТ КУЗОВА
            // ====================================================================
            if (math.lengthsq(targetDirection) > 0.001f)
            {
                // Направление, куда машина ДОЛЖНА смотреть (строго на плоскости, без наклонов по Y)
                quaternion targetRotation = quaternion.LookRotation(targetDirection, math.up());

                // Плавно разворачиваем трансформ машины туда, куда жмет игрок
                float rotationSpeed = 10f;
                transform.Rotation = math.slerp(transform.Rotation, targetRotation, rotationSpeed * FixedDeltaTime);
            }
            else
            {
                // Если игрок отпустил кнопки, мы принудительно выравниваем кузов относительно горизонта,
                // чтобы машина не оставалась стоять накренившейся после кочек.
                float3 forwardHeading = transform.Forward();
                forwardHeading.y = 0f; // убираем наклон вверх-вниз

                if (math.lengthsq(forwardHeading) > 0.001f)
                {
                    quaternion flatRotation = quaternion.LookRotation(math.normalize(forwardHeading), math.up());
                    transform.Rotation = math.slerp(transform.Rotation, flatRotation, 5.0f * FixedDeltaTime);
                }
            }

            movement.CurrentVelocity = velocity.Linear;
        }
    }


    //[BurstCompile]
    //private partial struct MovementParallelJob : IJobEntity
    //{
    //    public float FixedDeltaTime;
    //    public NetworkTick CurrentTick;

    //    public void Execute(
    //        ref PhysicsVelocity velocity,
    //        ref AAA_MovementComponent movement,
    //        ref LocalTransform transform,
    //        in DynamicBuffer<InputBufferData<AAA_InputComponent>> inputBuffer,
    //        in PhysicsMass mass,
    //        in IsControlledTag controlTag)
    //    {
    //        bool isCurrentVehicleActive = controlTag.IsActive;
    //        if (mass.InverseMass == 0f) return;

    //        InputBufferData<AAA_InputComponent> bufferWrapper;
    //        if (!isCurrentVehicleActive || !inputBuffer.GetDataAtTick(CurrentTick, out bufferWrapper))
    //        {
    //            bufferWrapper = default;
    //        }

    //        AAA_InputComponent inputData = bufferWrapper.InternalInput;

    //        // ====================================================================
    //        // УМНЫЙ СЕТЕВОЙ СТАБИЛИЗАТОР (Сохраняет гравитацию)
    //        // ====================================================================
    //        // Если скорость направлена вниз (падение) — мы её вообще не трогаем (гравитация работает).
    //        // Если физика пытается резко выстрелить машину вверх из-за бага предсказания
    //        // (скорость вверх больше разумного лимита, например, 3.0f), мы её мягко ограничиваем.
    //        if (velocity.Linear.y > 3.0f)
    //        {
    //            // Ограничиваем максимальный импульс прыжка/выталкивания из земли
    //            velocity.Linear.y = 3.0f;
    //        }

    //        // Заменяем жесткое зануление Angular на демпфирование (машина сможет плавно наклоняться 
    //        // на склонах, но не будет хаотично вращаться волчком при откатах сети)
    //        float angularDamping = 0.8f;
    //        velocity.Angular *= angularDamping;


    //        // Логика движения
    //        float3 targetDirection = new float3(inputData.MoveInput.x, 0, inputData.MoveInput.y);
    //        if (math.lengthsq(targetDirection) > 1f)
    //        {
    //            targetDirection = math.normalize(targetDirection);
    //        }

    //        float3 targetVelocity = targetDirection * movement.MaxSpeed;
    //        float3 currentHorizontalVelocity = new float3(velocity.Linear.x, 0f, velocity.Linear.z);
    //        float3 velocityChange = targetVelocity - currentHorizontalVelocity;

    //        float rate = math.lengthsq(targetDirection) > 0.001f ? movement.Acceleration : movement.Deceleration;
    //        float3 velocityStep = velocityChange * rate * FixedDeltaTime;

    //        if (math.lengthsq(velocityStep) > math.lengthsq(velocityChange))
    //        {
    //            velocityStep = velocityChange;
    //        }

    //        velocity.Linear.x += velocityStep.x;
    //        velocity.Linear.z += velocityStep.z;

    //        // Поворот через интерполяцию трансформирования (раз мы занулили Angular)
    //        if (math.lengthsq(targetDirection) > 0.001f)
    //        {
    //            quaternion targetRotation = quaternion.LookRotation(targetDirection, math.up());
    //            float rotationSpeed = 10f;
    //            transform.Rotation = math.slerp(transform.Rotation, targetRotation, rotationSpeed * FixedDeltaTime);
    //        }

    //        movement.CurrentVelocity = velocity.Linear;
    //    }
    //}


    //[BurstCompile]
    //private partial struct MovementParallelJob : IJobEntity
    //{
    //    public float FixedDeltaTime;
    //    public NetworkTick CurrentTick;

    //    public void Execute(
    //        ref PhysicsVelocity velocity,
    //        ref AAA_MovementComponent movement,
    //        ref LocalTransform transform,
    //        in DynamicBuffer<InputBufferData<AAA_InputComponent>> inputBuffer,
    //        ref PhysicsMass mass, // ИЗМЕНЕНО: сделали 'ref', чтобы заблокировать заносы кузова
    //        in IsControlledTag controlTag)
    //    {
    //        bool isCurrentVehicleActive = controlTag.IsActive;

    //        if (mass.InverseMass == 0f) return;

    //        InputBufferData<AAA_InputComponent> bufferWrapper;

    //        if (!isCurrentVehicleActive || !inputBuffer.GetDataAtTick(CurrentTick, out bufferWrapper))
    //        {
    //            bufferWrapper = default;
    //        }

    //        AAA_InputComponent inputData = bufferWrapper.InternalInput;

    //        // 1. ЖЕЛЕЗОБЕТОННЫЙ ФИКС ВРАЩЕНИЯ КУЗОВА:
    //        // Зануляем инерцию по осям X и Z. Теперь земля, колеса и кочки 
    //        // физически НЕ смогут наклонить кузов машины набок или перевернуть её.
    //        mass.InverseInertia.x = 0f;
    //        mass.InverseInertia.z = 0f;

    //        // Гарантируем, что физика не накопит паразитное вращение по X и Z
    //        velocity.Angular.x = 0f;
    //        velocity.Angular.z = 0f;

    //        // Логика расчета направления
    //        float3 targetDirection = new float3(inputData.MoveInput.x, 0, inputData.MoveInput.y);

    //        if (math.lengthsq(targetDirection) > 1f)
    //        {
    //            targetDirection = math.normalize(targetDirection);
    //        }

    //        float3 targetVelocity = targetDirection * movement.MaxSpeed;
    //        float3 currentHorizontalVelocity = new float3(velocity.Linear.x, 0f, velocity.Linear.z);
    //        float3 velocityChange = targetVelocity - currentHorizontalVelocity;

    //        float rate = math.lengthsq(targetDirection) > 0.001f ? movement.Acceleration : movement.Deceleration;
    //        float3 velocityStep = velocityChange * rate * FixedDeltaTime;

    //        if (math.lengthsq(velocityStep) > math.lengthsq(velocityChange))
    //        {
    //            velocityStep = velocityChange;
    //        }

    //        velocity.Linear.x += velocityStep.x;
    //        velocity.Linear.z += velocityStep.z;

    //        // 2. ИСПРАВЛЕННЫЙ ПОВОРОТ ДЛЯ Unity Physics:
    //        if (math.lengthsq(targetDirection) > 0.001f)
    //        {
    //            // Вычисляем угол, в который машина должна смотреть
    //            float targetAngle = math.atan2(targetDirection.x, targetDirection.z);

    //            // Вычисляем текущий угол машины на плоскости Y
    //            float currentAngle = math.atan2(transform.Forward().x, transform.Forward().z);

    //            // Находим кратчайшую разницу между углами (в радианах)
    //            float angleDifference = math.atan2(math.sin(targetAngle - currentAngle), math.cos(targetAngle - currentAngle));

    //            // Переводим разницу углов в угловую скорость по оси Y
    //            // Больше разница — быстрее крутится. Умножаем на коэффициент скорости (например, 8.0f)
    //            float rotationSpeed = 8.0f;
    //            velocity.Angular.y = angleDifference * rotationSpeed;
    //        }
    //        else
    //        {
    //            // Если игрок никуда не жмет, машина должна мгновенно прекратить вращение по Y
    //            velocity.Angular.y = 0f;
    //        }

    //        movement.CurrentVelocity = velocity.Linear;
    //    }
    //}

    //[BurstCompile]
    //private partial struct MovementParallelJob : IJobEntity
    //{
    //    public float FixedDeltaTime;
    //    public NetworkTick CurrentTick;

    //    public void Execute(
    //        ref PhysicsVelocity velocity,
    //        ref AAA_MovementComponent movement,
    //        ref LocalTransform transform,
    //        in DynamicBuffer<InputBufferData<AAA_InputComponent>> inputBuffer,
    //        in PhysicsMass mass,
    //        in IsControlledTag controlTag)
    //    {
    //        // ====================================================================
    //        // ГЛАВНЫЙ СЕТЕВОЙ ФИЛЬТР:
    //        // Если сервер прислал по сети Snapshot, что у этой конкретной машины 
    //        // поле IsActive равно false, джоба мгновенно прекращает разгон!
    //        // Она начнет плавно тормозить эту машину, убирая рассинхрон клавиатуры.
    //        // ====================================================================
    //        bool isCurrentVehicleActive = controlTag.IsActive;

    //        if (mass.InverseMass == 0f)
    //        {
    //            return;
    //        }

    //        // ИСПРАВЛЕНИЕ ОШИБКИ: В вашей версии метод называется GetDataAtTick
    //        // Он выполняет ту же safe-роль: находит нужный кадр ввода в истории
    //        // 1. Создаем переменную правильного типа буфера Netcode
    //        InputBufferData<AAA_InputComponent> bufferWrapper;

    //        // 2. Вызываем метод, передавая обертку в качестве out-параметра
    //        if (!isCurrentVehicleActive || !inputBuffer.GetDataAtTick(CurrentTick, out bufferWrapper))
    //        {
    //            // Если ввод для текущего тика еще не получен, обнуляем обертку
    //            bufferWrapper = default;
    //        }

    //        // 3. Извлекаем вашу чистую структуру ввода из поля Value обертки
    //        AAA_InputComponent inputData = bufferWrapper.InternalInput;

    //        // Логика движения остается вашей без изменений:
    //        float3 targetDirection = new float3(inputData.MoveInput.x, 0, inputData.MoveInput.y);

    //        if (math.lengthsq(targetDirection) > 1f)
    //        {
    //            targetDirection = math.normalize(targetDirection);
    //        }

    //        float3 targetVelocity = targetDirection * movement.MaxSpeed;
    //        float3 currentHorizontalVelocity = new float3(velocity.Linear.x, 0f, velocity.Linear.z);
    //        float3 velocityChange = targetVelocity - currentHorizontalVelocity;

    //        float rate = math.lengthsq(targetDirection) > 0.001f ? movement.Acceleration : movement.Deceleration;
    //        float3 velocityStep = velocityChange * rate * FixedDeltaTime;

    //        if (math.lengthsq(velocityStep) > math.lengthsq(velocityChange))
    //        {
    //            velocityStep = velocityChange;
    //        }

    //        velocity.Linear.x += velocityStep.x;
    //        velocity.Linear.z += velocityStep.z;

    //        if (math.lengthsq(targetDirection) > 0.001f)
    //        {
    //            quaternion targetRotation = quaternion.LookRotation(targetDirection, math.up());
    //            float rotationSpeed = 10f;
    //            transform.Rotation = math.slerp(transform.Rotation, targetRotation, rotationSpeed * FixedDeltaTime);
    //        }

    //        //// ====================================================================
    //        //// ЖЕЛЕЗОБЕТОННЫЙ ФИКС ДЛЯ ИЕРАРХИИ ЧАНКОВ ПРИ ФИЗИКЕ:
    //        //// Мы вручную прибавляем физическую скорость к компоненту LocalTransform родителя.
    //        //// Это заставит встроенную ParentSystem увидеть перемещение машины 
    //        //// и автоматически сдвинуть все привязанные чанки вслед за ней!
    //        //// ====================================================================
    //        //transform.Position += velocity.Linear * FixedDeltaTime;
    //        //// ====================================================================


    //        movement.CurrentVelocity = velocity.Linear;
    //    }
    //}
}


//using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Physics;
//using Unity.Physics.Systems;
//using Unity.Transforms; // Обязательно добавляем для LocalTransform

//[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
//[BurstCompile]
//public partial struct AAAMovementSystem : ISystem
//{
//    private EntityQuery _inputQuery;

//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        _inputQuery = state.GetEntityQuery(ComponentType.ReadOnly<InputStateSingleton>());
//        state.RequireForUpdate(_inputQuery);
//        state.RequireForUpdate<IsControlledTag>();
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        var inputSingleton = _inputQuery.GetSingleton<InputStateSingleton>();
//        float3 targetDirection = new float3(inputSingleton.MoveInput.x, 0, inputSingleton.MoveInput.y);

//        if (math.lengthsq(targetDirection) > 1f)
//        {
//            targetDirection = math.normalize(targetDirection);
//        }

//        float frameDeltaTime = SystemAPI.Time.DeltaTime;

//        state.Dependency = new MovementParallelJob
//        {
//            TargetDirection = targetDirection,
//            FixedDeltaTime = frameDeltaTime
//        }.ScheduleParallel(state.Dependency);
//    }

//    [WithAll(typeof(IsControlledTag))]
//    [BurstCompile]
//    private partial struct MovementParallelJob : IJobEntity
//    {
//        public float3 TargetDirection;
//        public float FixedDeltaTime;

//        public void Execute(
//            ref PhysicsVelocity velocity,
//            ref AAA_MovementComponent movement,
//            ref LocalTransform transform,
//            in PhysicsMass mass)
//        {
//            // Если тело статично, ничего не делаем
//            if (mass.InverseMass == 0f) return;

//            // 1. Рассчитываем целевую горизонтальную скорость
//            float3 targetVelocity = TargetDirection * movement.MaxSpeed;

//            // 2. Берем ТЕКУЩУЮ горизонтальную скорость, которую физика уже скорректировала после столкновений
//            float3 currentHorizontalVelocity = new float3(velocity.Linear.x, 0f, velocity.Linear.z);

//            // 3. Находим разницу между тем, что мы хотим, и тем, что есть сейчас
//            float3 velocityChange = targetVelocity - currentHorizontalVelocity;

//            // Выбираем темп разгона или торможения
//            float rate = math.lengthsq(TargetDirection) > 0.001f ? movement.Acceleration : movement.Deceleration;

//            // Рассчитываем шаг изменения скорости за этот кадр
//            float3 velocityStep = velocityChange * rate * FixedDeltaTime;

//            // Ограничиваем шаг, чтобы не проскочить целевую скорость
//            if (math.lengthsq(velocityStep) > math.lengthsq(velocityChange))
//            {
//                velocityStep = velocityChange;
//            }

//            // 4. ВАЖНО: Применяем изменения ТОЛЬКО к осям X и Z. 
//            // Ось Y (гравитацию, падение, прыжки) мы ВООБЩЕ НЕ ТРОГАЕМ.
//            velocity.Linear.x += velocityStep.x;
//            velocity.Linear.z += velocityStep.z;

//            // --- БЛОК ПОВОРОТА ЧЕРЕЗ LOCALTRANSFORM ---
//            if (math.lengthsq(TargetDirection) > 0.001f)
//            {
//                quaternion targetRotation = quaternion.LookRotation(TargetDirection, math.up());
//                float rotationSpeed = 10f;
//                transform.Rotation = math.slerp(transform.Rotation, targetRotation, rotationSpeed * FixedDeltaTime);
//            }

//            // Записываем текущую скорость для анимаций или логики
//            movement.CurrentVelocity = velocity.Linear;

//            //// Блокируем угловое вращение физики, чтобы персонаж не падал лицом в землю
//            //velocity.Angular = float3.zero;
//        }
//    }

//}

////using Unity.Burst;
////using Unity.Entities;
////using Unity.Mathematics;
////using Unity.Physics;
////using Unity.Physics.Systems;

////[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
////[BurstCompile]
////public partial struct AAAMovementSystem : ISystem
////{
////    private EntityQuery _inputQuery;

////    [BurstCompile]
////    public void OnCreate(ref SystemState state)
////    {
////        _inputQuery = state.GetEntityQuery(ComponentType.ReadOnly<InputStateSingleton>());
////        state.RequireForUpdate(_inputQuery);
////        state.RequireForUpdate<IsControlledTag>();
////    }

////    [BurstCompile]
////    public void OnUpdate(ref SystemState state)
////    {
////        var inputSingleton = _inputQuery.GetSingleton<InputStateSingleton>();
////        float3 targetDirection = new float3(inputSingleton.MoveInput.x, 0, inputSingleton.MoveInput.y);

////        if (math.lengthsq(targetDirection) > 1f)
////        {
////            targetDirection = math.normalize(targetDirection);
////        }

////        float frameDeltaTime = SystemAPI.Time.DeltaTime;

////        state.Dependency = new MovementParallelJob
////        {
////            TargetDirection = targetDirection,
////            FixedDeltaTime = frameDeltaTime
////        }.ScheduleParallel(state.Dependency);
////    }

////    [WithAll(typeof(IsControlledTag))]
////    [BurstCompile]
////    private partial struct MovementParallelJob : IJobEntity
////    {
////        public float3 TargetDirection;
////        public float FixedDeltaTime;

////        public void Execute(ref PhysicsVelocity velocity, ref AAA_MovementComponent movement, in PhysicsMass mass)
////        {
////            if (mass.InverseMass == 0f) return;

////            if (math.lengthsq(TargetDirection) > 0.001f && math.lengthsq(velocity.Linear) < 0.001f)
////            {
////                velocity.Linear.y = -0.01f;
////            }

////            float3 targetVelocity = TargetDirection * movement.MaxSpeed;
////            float3 currentHorizontalVelocity = new float3(velocity.Linear.x, 0f, velocity.Linear.z);

////            float3 velocityChange = targetVelocity - currentHorizontalVelocity;
////            float rate = math.lengthsq(TargetDirection) > 0.001f ? movement.Acceleration : movement.Deceleration;

////            float3 velocityStep = velocityChange * rate * FixedDeltaTime;

////            if (math.lengthsq(velocityStep) > math.lengthsq(velocityChange))
////            {
////                velocityStep = velocityChange;
////            }

////            velocity.Linear.x += velocityStep.x;
////            velocity.Linear.z += velocityStep.z;

////            movement.CurrentVelocity = velocity.Linear;
////            velocity.Angular = float3.zero;
////        }
////    }
////}



//////using Unity.Burst;
//////using Unity.Entities;
//////using Unity.Mathematics;
//////using Unity.Transforms;

//////[UpdateInGroup(typeof(TransformSystemGroup))]
//////[BurstCompile]
//////public partial struct AAAMovementSystem : ISystem
//////{
//////    private EntityQuery _inputQuery;

//////    [BurstCompile]
//////    public void OnCreate(ref SystemState state)
//////    {
//////        _inputQuery = state.GetEntityQuery(ComponentType.ReadOnly<InputStateSingleton>());
//////        state.RequireForUpdate(_inputQuery);
//////    }

//////    [BurstCompile]
//////    public void OnUpdate(ref SystemState state)
//////    {
//////        var inputSingleton = _inputQuery.GetSingleton<InputStateSingleton>();
//////        float3 targetDirection = new float3(inputSingleton.MoveInput.x, 0, inputSingleton.MoveInput.y);

//////        if (math.lengthsq(targetDirection) > 1f)
//////        {
//////            targetDirection = math.normalize(targetDirection);
//////        }

//////        var movementJob = new AAAControlledMovementJob
//////        {
//////            DeltaTime = SystemAPI.Time.DeltaTime,
//////            TargetDirection = targetDirection
//////        };

//////        // ПРАВИЛЬНО ДЛЯ AAA: Передаем зависимости и сохраняем их обратно в state.Dependency
//////        // Это сообщает AAACameraFollowSystem, что нужно дождаться окончания работы этого Job'а
//////        state.Dependency = movementJob.ScheduleParallel(state.Dependency);
//////    }
//////}


//////// Атрибут [WithAll] на уровне структуры жестко фильтрует сущности на уровне ядер процессора
//////[WithAll(typeof(IsControlledTag))]
//////[BurstCompile]
//////public partial struct AAAControlledMovementJob : IJobEntity
//////{
//////    public float DeltaTime;
//////    public float3 TargetDirection;

//////    // Теперь метод чистый, без ошибочного параметра tag
//////    private void Execute(ref LocalTransform transform, ref AAA_MovementComponent movement)
//////    {
//////        float3 targetVelocity = TargetDirection * movement.MaxSpeed;

//////        if (math.lengthsq(TargetDirection) > 0.001f)
//////        {
//////            movement.CurrentVelocity = math.lerp(movement.CurrentVelocity, targetVelocity, movement.Acceleration * DeltaTime);
//////        }
//////        else
//////        {
//////            movement.CurrentVelocity = math.lerp(movement.CurrentVelocity, float3.zero, movement.Deceleration * DeltaTime);
//////            if (math.lengthsq(movement.CurrentVelocity) < 0.01f) movement.CurrentVelocity = float3.zero;
//////        }

//////        transform.Position += movement.CurrentVelocity * DeltaTime;
//////    }
//////}
