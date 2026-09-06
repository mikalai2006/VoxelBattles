using Unity.Entities;
using Unity.Networking.Transport;

public struct CustomPipelineSingleton : IComponentData
{
    public NetworkPipeline ReliableFragmentedPipeline;
}

// Пример создания пайплайна (в коде инициализации Transport / Bootstrap)
public static class NetworkPipelineFactory
{
    public static NetworkPipeline CreateReliableFragmentedPipeline(ref NetworkDriver driver)
    {
        // Создаем классическую цепочку: Надежная доставка с гарантией порядка (Reliable + Sequenced) + Фрагментация
        return driver.CreatePipeline(
            //typeof(FragmentationPipelineStage),
            typeof(ReliableSequencedPipelineStage)
        );
    }
}
