using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public enum VoxelMaterialType : byte
{
    Brick,
    Wood,
    Platform,
    Wall
}

public class VoxelAudioManager : MonoBehaviour
{
    public static VoxelAudioManager Instance { get; private set; }

    [Header("3D Слой Кирпича (Hollywood)")]
    [SerializeField] private AudioClip[] brickImpactClips;
    [SerializeField] private AudioClip[] brickCrunchClips;
    [SerializeField] private AudioClip[] brickDebrisClips;

    [Header("3D Слой Дерева (Premium Wood)")]
    [SerializeField] private AudioClip[] woodSnapClips;
    [SerializeField] private AudioClip[] woodSplinterClips;
    [SerializeField] private AudioClip[] woodBodyClips;

    [Header("3D Слой Платформы")]
    [SerializeField] private AudioClip[] platformClips;

    [Header("3D Слой стены")]
    [SerializeField] private AudioClip[] wallClips;

    [Header("Настройки Пула")]
    [SerializeField] private int poolSize = 64;
    [SerializeField] private AudioSource audioSourcePrefab;

    private Queue<AudioSource> pool = new Queue<AudioSource>();
    // Словарь для запоминания времени последнего проигрывания каждого клипа
    private readonly Dictionary<AudioClip, float> _lastPlayTimes = new Dictionary<AudioClip, float>();
    // МИНИМАЛЬНЫЙ ИНТЕРВАЛ между одинаковыми звуками (в секундах)
    // 0.04f означает, что этот клип прозвучит максимум 25 раз в секунду, что спасет движок
    private const float MinSoundInterval = 0.04f;

    private void Awake()
    {
        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = Instantiate(audioSourcePrefab, transform);
            source.gameObject.SetActive(true);
            pool.Enqueue(source);
        }
    }

    public void PlayOtherSound3DAsync(Vector3 worldPosition, AudioClip clip, float damageScale = 1.0f)
    {
        // Запускаем 3D-воспроизведение слоев
        Play3DLayer(clip, worldPosition, volume: 1.0f * damageScale, minPitch: 0.92f, maxPitch: 1.08f);
    }

    /// <summary>
    /// Универсальный асинхронный 3D-взрыв вокселей с поддержкой материалов.
    /// </summary>
    public async UniTaskVoid PlayDestructionSound3DAsync(Vector3 worldPosition, VoxelMaterialType materialType, float damageScale = 1.0f)
    {
        if (pool.Count < 3) return;

        if (damageScale > 1)
        {
            damageScale = 1;
        }

        AudioClip layer1, layer2, layer3;
        int delayLayer2, delayLayer3;

        // Динамически выбираем аудиопакет в зависимости от материала вокселя
        if (materialType == VoxelMaterialType.Wood)
        {
            layer1 = woodSnapClips[Random.Range(0, woodSnapClips.Length)];
            layer2 = woodSplinterClips[Random.Range(0, woodSplinterClips.Length)];
            layer3 = woodBodyClips[Random.Range(0, woodBodyClips.Length)];

            delayLayer2 = 25; // Дерево трещит быстрее, задержки меньше
            delayLayer3 = 15;
        } else if (materialType == VoxelMaterialType.Platform)
        {
            layer1 = platformClips[Random.Range(0, woodSnapClips.Length)];
            layer2 = null;
            layer3 = null;

            delayLayer2 = 25; // Дерево трещит быстрее, задержки меньше
            delayLayer3 = 15;

        } else if (materialType == VoxelMaterialType.Wall)
        {
            layer1 = wallClips[Random.Range(0, woodSnapClips.Length)];
            layer2 = null;
            layer3 = null;

            delayLayer2 = 25; // Дерево трещит быстрее, задержки меньше
            delayLayer3 = 15;

        }
        else // Brick
        {
            layer1 = brickImpactClips[Random.Range(0, brickImpactClips.Length)];
            layer2 = null; // brickCrunchClips[Random.Range(0, brickCrunchClips.Length)];
            layer3 = brickDebrisClips[Random.Range(0, brickDebrisClips.Length)];

            delayLayer2 = 40;
            delayLayer3 = 20;
        }

        // Запускаем 3D-воспроизведение слоев
        Play3DLayer(layer1, worldPosition, volume: 1.0f * damageScale, minPitch: 0.92f, maxPitch: 1.08f);

        if (layer2 != null)
        {
            await UniTask.Delay(delayLayer2);
            Play3DLayer(layer2, worldPosition, volume: 0.85f * damageScale, minPitch: 0.90f, maxPitch: 1.10f);
        }

        if (layer3 != null)
        {
            await UniTask.Delay(delayLayer3);
            Play3DLayer(layer3, worldPosition, volume: 0.70f * damageScale, minPitch: 0.95f, maxPitch: 1.15f);
        }
    }

    //public void Play3DLayer(AudioClip clip, Vector3 worldPosition, float volume, float minPitch, float maxPitch)
    //{
    //    // 1. Быстрый игнор лавинообразного спама (Ухо не заметит разницы, а движок спасен)
    //    if (clip == null || pool.Count == 0) return;

    //    if (_lastPlayTimes.TryGetValue(clip, out float lastTime))
    //    {
    //        if (Time.time - lastTime < MinSoundInterval) return;
    //        _lastPlayTimes[clip] = Time.time;
    //    }
    //    else
    //    {
    //        _lastPlayTimes.Add(clip, Time.time);
    //    }

    //    // 2. Битовая валидация float-значений
    //    if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0f) volume = 0f;
    //    else if (volume > 1f) volume = 1f;

    //    if (float.IsNaN(minPitch) || float.IsInfinity(minPitch)) minPitch = 1f;
    //    if (float.IsNaN(maxPitch) || float.IsInfinity(maxPitch)) maxPitch = 1f;

    //    // 3. Забираем AudioSource из пула (он всегда активен на сцене)
    //    AudioSource source = pool.Dequeue();

    //    // 4. КАРДИНАЛЬНОЕ РЕШЕНИЕ: Запускаем через PlayOneShot!
    //    // Метод .PlayOneShot() не создает полноценный аудио-поток, а просто "подмешивает"
    //    // звук в уже существующий канал. Это исключает деление на ноль в FMOD.
    //    source.pitch = Random.Range(minPitch, maxPitch);
    //    source.PlayOneShot(clip, volume);

    //    // 5. Возвращаем в пул сразу в следующем кадре! 
    //    // Так как PlayOneShot позволяет одному AudioSource воспроизводить несколько звуков одновременно,
    //    // нам не нужно ждать окончания клипа — источник свободен для пула мгновенно.
    //    ReturnToPoolImmediate(source).Forget();
    //}

    //private async UniTaskVoid ReturnToPoolImmediate(AudioSource source)
    //{
    //    // Даем Unity пропустить 1 кадр для стабилизации аудиомикшера
    //    await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

    //    if (source != null)
    //    {
    //        pool.Enqueue(source);
    //    }
    //}

    public void Play3DLayer(AudioClip clip, Vector3 worldPosition, float volume, float minPitch, float maxPitch)
    {
        //Debug.LogWarning($"worldPosition1={worldPosition}, volume={volume}, minPitch={minPitch}, maxPitch={maxPitch}");
        if (clip == null || pool.Count == 0) return;

        // 1. Лимитер частоты для защиты FMOD от перегрузки
        if (_lastPlayTimes.TryGetValue(clip, out float lastTime))
        {
            if (Time.time - lastTime < MinSoundInterval) return;
            _lastPlayTimes[clip] = Time.time;
        }
        else
        {
            _lastPlayTimes.Add(clip, Time.time);
        }

        // 2. Валидация числовых значений float
        if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0f) volume = 0f;
        else if (volume > 1f) volume = 1f;

        if (float.IsNaN(minPitch) || float.IsInfinity(minPitch)) minPitch = 1f;
        if (float.IsNaN(maxPitch) || float.IsInfinity(maxPitch)) maxPitch = 1f;

        // 3. ЖЕСТКАЯ ОЧИСТКА КООРДИНАТ ОТ SMALL FLOAT (Underflow Protection)
        // Если значение оси меньше 0.1f по модулю или является NaN/Infinity, принудительно ставим чистый 0f
        if (float.IsNaN(worldPosition.x) || float.IsInfinity(worldPosition.x) || (worldPosition.x < 0.1f && worldPosition.x > -0.1f))
            worldPosition.x = 0f;

        if (float.IsNaN(worldPosition.y) || float.IsInfinity(worldPosition.y) || (worldPosition.y < 0.1f && worldPosition.y > -0.1f))
            worldPosition.y = 0f;

        if (float.IsNaN(worldPosition.z) || float.IsInfinity(worldPosition.z) || (worldPosition.z < 0.1f && worldPosition.z > -0.1f))
            worldPosition.z = 0f;

        // 4. Получение объекта из пула
        AudioSource source = pool.Dequeue();
        Transform sourceTransform = source.transform;

        // Гасим компонент, БЕЗОПАСНО перемещаем в очищенные координаты и включаем обратно
        source.enabled = false;

        sourceTransform.position = worldPosition;
        sourceTransform.localScale = Vector3.one;

        source.clip = clip;
        source.volume = volume;

        float finalPitch = Random.Range(minPitch, maxPitch);
        source.pitch = finalPitch;

        source.enabled = true;
        source.Play();

        // 5. Математически точный расчет времени жизни под любой питч
        float finalDuration = clip.length;
        float absolutePitch = Mathf.Abs(finalPitch);

        if (absolutePitch > 0.01f)
        {
            finalDuration = clip.length / absolutePitch;
        }

        ReturnToPoolAfterDelayAsync(source, finalDuration).Forget();
    }

    private async UniTaskVoid ReturnToPoolAfterDelayAsync(AudioSource source, float delay)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: this.GetCancellationTokenOnDestroy());

        if (source != null)
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            source.enabled = false; // Мягко гасим компонент, не трогая GameObject

            pool.Enqueue(source);
        }
    }
}
