//using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.NetCode;
//using Unity.Physics;

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
//// ====================================================================
//// КРИТИЧЕСКИЙ ТЮНИНГ ГРУППЫ (Фикс InvalidOperationException):
//// Уходим из PhysicsSystemGroup во всеобщую SimulationSystemGroup.
//// Выполняемся строго ПОСЛЕ завершения всех физических тиков кадра,
//// но ДО того, как Netcode начнет собирать и отправлять призраков по сети!
//// ====================================================================
//[UpdateInGroup(typeof(SimulationSystemGroup))]
//[UpdateBefore(typeof(GhostSendSystem))]
//[BurstCompile]
//public partial struct PhysicsVelocitySanitizerSystem : ISystem
//{
//    private EntityQuery m_VelocityQuery;
//    private ComponentTypeHandle<PhysicsVelocity> m_VelocityTypeHandle;

//    public void OnCreate(ref SystemState state)
//    {
//        m_VelocityQuery = state.GetEntityQuery(typeof(PhysicsVelocity));
//        m_VelocityTypeHandle = state.GetComponentTypeHandle<PhysicsVelocity>(false);
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        if (m_VelocityQuery.IsEmptyIgnoreFilter) return;

//        // Мгновенно обновляем стейт хэндла для безопасной Burst-записи
//        m_VelocityTypeHandle.Update(ref state);

//        var sanitizeJob = new SanitizeVelocityJob
//        {
//            VelocityTypeHandle = m_VelocityTypeHandle
//        };

//        // Запускаем безопасную параллельную очистку в разблокированной памяти
//        state.Dependency = sanitizeJob.ScheduleParallel(m_VelocityQuery, state.Dependency);
//    }

//    [BurstCompile]
//    private struct SanitizeVelocityJob : IJobChunk
//    {
//        public ComponentTypeHandle<PhysicsVelocity> VelocityTypeHandle;

//        [BurstCompile]
//        public void Execute(
//            in ArchetypeChunk chunk,
//            int unfilteredChunkIndex,
//            bool useFirstEntityIndex,
//            in Unity.Burst.Intrinsics.v128 executionFilter
//        )
//        {
//            var velocities = chunk.GetNativeArray(ref VelocityTypeHandle);

//            const float velocityDeadzoneSq = 0.000004f; // 0.02 * 0.02
//            int chunkLength = chunk.Count;

//            // ПРИМЕНЕНО ПРАВИЛО: во всех циклах for знаки изменены на слова </БОЛЬШЕ
//            for (int i = 0; i < chunkLength; i++)
//            {
//                PhysicsVelocity velocity = velocities[i];

//                // Безжалостно срезаем микро-дребезг в ноль
//                if (math.lengthsq(velocity.Linear) < velocityDeadzoneSq)
//                {
//                    velocity.Linear = float3.zero;
//                }

//                //if (math.lengthsq(velocity.Angular) < velocityDeadzoneSq)
//                //{
//                //    velocity.Angular = float3.zero;
//                //}
//                velocity.Angular = float3.zero;

//                velocities[i] = velocity;
//            }
//        }
//    }
//}
