using Unity.Entities;
using UnityEngine;

public class NetworkVoxelChunkAuthoring : MonoBehaviour
{
    public class Baker : Baker<NetworkVoxelChunkAuthoring>
    {
        public override void Bake(NetworkVoxelChunkAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            //// ====================================================================
            //// КРИТИЧЕСКИЙ ФИКС ДЛЯ РЕПЛИКАЦИИ NON-GHOST ЧАНКОВ В UNITY 6
            //// Добавляем нативный системный компонент Netcode. Он сообщает сетевому ядру, 
            //// что этот чанк является дочерним non-ghost элементом внутри LinkedEntityGroup.
            //// Без этого компонента сервер никогда не отправит чанки клиенту!
            //// ====================================================================
            //AddComponent<GhostChildEntity>(entity);
            //// ====================================================================


            // 1. ЖЕСТКОЕ ЗАПЕКАНИЕ ЛОКАЛЬНОГО БУФЕРА МАСКИ РАЗРУШЕНИЙ
            // В Entities 1.4.7 вызов AddBuffer гарантирует, что при создании сущности 
            // из префаба на клиенте и сервере под маску 512 ulong сразу зарезервируется unmanaged-память.
            //var maskBuffer =
            AddBuffer<LocalChunkDestructionMask>(entity);
            // ====================================================================
            // ЖЕЛЕЗОБЕТОННЫЙ SAFE ФИКС КРАША ДЕСЕРИАЛИЗАЦИИ NETCODE:
            // Мы обязаны принудительно задать размер буфера прямо в префабе при бейке!
            // Теперь дефолтный Baseline чанка на клиенте сразу родится с длиной 512 элементов.
            // Автогенерируемый код Netcode больше не споткнется о пустой массив, 
            // джоба десериализации не упадет, и Ghost Map сопоставит ID идеально ровно.
            // ====================================================================
            //maskBuffer.ResizeUninitialized(512);
            // ====================================================================

            // 2. ДОБАВЛЯЕМ РАНТАЙМ-МЕТАДАННЫЕ ЧАНКА (Индексы и связь с моделью)
            AddComponent<ChunkIndexComponent>(entity);
            AddComponent<VoxelModelHeader>(entity);

            AddComponent<NetworkParent>(entity);

            //// 3. КОМПОНЕНТЫ-ПЕРЕКЛЮЧАТЕЛИ АКТИВНОСТИ И СТЕЙТЫ
            //AddComponent<ChunkActiveState>(entity);
            //AddComponent<ChunkPhysicsActiveState>(entity);
            //// По умолчанию тушим стейты. Их активирует клиентская система мешинга, 
            //// когда чанк будет полностью распакован и готов к отрисовке.
            //SetComponentEnabled<ChunkActiveState>(entity, false);
            //SetComponentEnabled<ChunkPhysicsActiveState>(entity, false);

            //// 4. ТЕГ КЛИЕНТСКОГО РЕНДЕРА
            //AddComponent<ClientRenderState>(entity);

            //// Добавляем временные буфера
            //var vertexBuffer = AddBuffer<ChunkVertexElement>(entity);
            //var indexBuffer = AddBuffer<ChunkIndexElement>(entity);
            //// ====================================================================
            //// ЖЕЛЕЗОБЕТОННЫЙ SAFE ФИКС КРАША INDEXOUTOFRANGE:
            //// Задаем буферам префаба максимальный кап СТРОГО ОДИН РАЗ при спавне чанка!
            //// Теперь каждый чанк в мультиплеере сразу рождается с длиной 16384 и 24576 ячеек.
            //// Массивы больше никогда не будут нулевыми, С++ ядро физики MeshBuilder 
            //// прочитает их идеально без ошибок, а блокировка BufferTypeHandle исчезнет!
            //// ====================================================================
            //vertexBuffer.Resize(16384, NativeArrayOptions.ClearMemory);
            //indexBuffer.Resize(24576, NativeArrayOptions.ClearMemory);
            //// ====================================================================

            AddComponent<ChunkColliderNeedCreate>(entity);
            SetComponentEnabled<ChunkColliderNeedCreate>(entity, false);
            AddComponent<ChunkColliderNeedApply>(entity);
            SetComponentEnabled<ChunkColliderNeedApply>(entity, false);

            //AddComponent<ChunkColliderData>(entity);
            //SetComponentEnabled<ChunkColliderData>(entity, false);

            // данные для меша
            AddComponent<ChunkMeshNeedCreate>(entity);
            SetComponentEnabled<ChunkMeshNeedCreate>(entity, false);
            AddComponent<ChunkMeshNeedApply>(entity);
            SetComponentEnabled<ChunkMeshNeedApply>(entity, false);

            //AddComponent<ChunkMeshData>(entity);
            //SetComponentEnabled<ChunkMeshData>(entity, false);

            AddComponent<ChunkMeshLink>(entity);
        }
    }
}
