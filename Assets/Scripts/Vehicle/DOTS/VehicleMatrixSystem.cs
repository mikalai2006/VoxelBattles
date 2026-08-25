//using Unity.Burst;
//using Unity.Entities;
//using Unity.Transforms;

//[UpdateInGroup(typeof(TransformSystemGroup), OrderFirst = true)]
//[BurstCompile]
//public partial struct VehicleMatrixSystem : ISystem
//{
//    private EntityQuery _matrixStorageQuery;

//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        _matrixStorageQuery = state.GetEntityQuery(ComponentType.ReadWrite<VehicleMatrixElement>());
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        // 1. Гарантируем, что сущность-хранилище матриц существует в мире
//        if (_matrixStorageQuery.IsEmpty)
//        {
//            Entity storage = state.EntityManager.CreateEntity();
//            state.EntityManager.AddBuffer<VehicleMatrixElement>(storage);
//            return;
//        }

//        Entity storageEntity = _matrixStorageQuery.GetSingletonEntity();
//        DynamicBuffer<VehicleMatrixElement> matrixBuffer = SystemAPI.GetBuffer<VehicleMatrixElement>(storageEntity);

//        // Перед каждым кадром очищаем буфер, мы будем заполнять его заново актуальными матрицами
//        matrixBuffer.Clear();

//        // 2. Быстро собираем матрицы всех объектов, которые могут быть родителями
//        // Сюда автоматически попадут и Шасси (у которых есть VehicleRootTag) и Башни (у которых есть VehicleFlatChildData)
//        var ltwLookup = state.GetComponentLookup<LocalToWorld>(true);

//        // Нам нужен список всех сущностей-родителей. Чтобы джобы деталей знали индекс, 
//        // мы временно заменяем это простой логикой: детали будут обращаться к буферу.
//        // Но для рантайм-индексации еще проще — пусть каждый родитель сам хранит свой ID!
//    }
//}
