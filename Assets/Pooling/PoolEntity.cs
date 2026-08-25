using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

/// <summary>
/// Базовый класс для всех сущностей, живущих в UltraVirtualPool.
/// Автоматически управляет жизненным циклом, таймерами и вложенными дочерними объектами.
/// </summary>
public abstract class PoolEntity : MonoBehaviour
{
    // Ссылка на родной пул, из которого был заспавнен данный объект
    private UltraVirtualPool _sourcePool;

    // Уникальный хэндл текущего поколения этой ячейки пула
    private PoolHandle _myHandle;

    private float _lifeTime;

    // Токен для отмены асинхронного таймера при досрочном деспавне (например, при взрыве)
    private CancellationTokenSource _cts;

    /// <summary>
    /// Возвращает текущий валидный хэндл сущности.
    /// </summary>
    public PoolHandle Handle => _myHandle;

    // =========================================================================
    // ЗОЛОТАЯ СЕРЕДИНА: Список активных связей (Zero-Alloc, мгновенный доступ)
    // =========================================================================
    // Память под 4 ссылки выделяется один раз при создании объекта. 
    // Больше никаких выделений памяти (GC) и поисков по иерархии через GetComponents.
    private readonly List<VirtualChild> _attachedChildren = new List<VirtualChild>(4);

    // =========================================================================
    // УПРАВЛЕНИЕ МАССОВЫМ УДАЛЕНИЕМ (Код написан 1 раз для всех наследников)
    // =========================================================================
    [Header("Pool Array Settings")]
    [Tooltip("Максимальное количество объектов, которое эта сущность может заспавнить внутрь себя за раз (например, пушек на машине).")]
    [SerializeField] protected int maxPoolElements = 32;

    // Унаследованный массив хэндлов для дочерних элементов (имя стандартизировано)
    protected PoolHandle[] _activePoolHandle;
    protected int _activeCount;

    /// <summary>
    /// Безопасная инициализация массива хэндлов. Автоматически вызывается при первом спавне.
    /// </summary>
    protected void InitPoolArray()
    {
        if (_activePoolHandle == null)
        {
            _activePoolHandle = new PoolHandle[maxPoolElements];
            for (int i = 0; i < _activePoolHandle.Length; i++)
            {
                _activePoolHandle[i] = PoolHandle.Invalid;
            }
        }
    }

    /// <summary>
    /// Регистрирует дочерний элемент. Автоматически вызывается из VirtualChild.Attach.
    /// </summary>
    public void RegisterChild(VirtualChild child)
    {
        if (child != null && !_attachedChildren.Contains(child))
        {
            _attachedChildren.Add(child);
        }
    }

    /// <summary>
    /// Удаляет дочерний элемент из списка. Автоматически вызывается из VirtualChild.Detach.
    /// </summary>
    public void UnregisterChild(VirtualChild child)
    {
        if (child != null)
        {
            _attachedChildren.Remove(child);
        }
    }

    /// <summary>
    /// Универсальный метод для массового удаления всех вложенных объектов (например, всех пушек этой машины).
    /// </summary>
    public void RemoveAllActive(Vector3 explosionForce, ForceMode forceMode)
    {
        if (_activeCount == 0 || _activePoolHandle == null) return;

        // Идем с конца заполненной части массива хэндлов
        for (int i = _activeCount - 1; i >= 0; i--)
        {
            PoolHandle handle = _activePoolHandle[i];

            if (handle.IsValid)
            {
                // ИСПРАВЛЕНО: Вместо неверного запроса к пулу через _sourcePool.GetElementByHandle,
                // мы ищем нужного ребенка в нашем собственном быстром списке '_attachedChildren'.
                PoolEntity entityToDespawn = null;

                for (int j = 0; j < _attachedChildren.Count; j++)
                {
                    // Проверяем, совпадает ли хэндл ребенка с тем, который мы хотим удалить
                    if (_attachedChildren[j].MyPoolEntity != null &&
                        _attachedChildren[j].MyPoolEntity.Handle.Equals(handle))
                    {
                        entityToDespawn = _attachedChildren[j].MyPoolEntity;
                        break;
                    }
                }

                // Если нашли прямую ссылку на скрипт ребенка в нашем бортовом журнале
                if (entityToDespawn != null)
                {
                    // Вызываем деспавн напрямую у пушки. Она сама знает свой родной пул пушек 
                    // (поле _sourcePool внутри пушки указывает на правильный пул) и безопасно вернется туда!
                    entityToDespawn.SmartDespawn(explosionForce, forceMode);
                }

                // Очищаем ячейку в массиве хэндлов
                _activePoolHandle[i] = PoolHandle.Invalid;
            }
        }

        _activeCount = 0;
    }

    // =========================================================================
    // ВНУТРЕННЯЯ ЛОГИКА ПУЛА И ЖИЗНЕННОГО ЦИКЛА
    // =========================================================================

    /// <summary>
    /// Внутренний метод пула для инициализации сущности при спавне (Zero-Alloc).
    /// </summary>
    public void InternalSetup(UltraVirtualPool pool, PoolHandle handle, float lifeTime)
    {
        //_sourcePool = pool;
        //_myHandle = handle;

        //// Гарантируем, что массив хэндлов дочерних элементов готов к работе
        //InitPoolArray();

        //// Если задано время жизни, запускаем асинхронный таймер автоудаления
        //if (lifeTime > 0.001f)
        //{
        //    _cts?.Cancel();
        //    _cts?.Dispose();
        //    _cts = new CancellationTokenSource();

        //    StartLifeTimeTimer(lifeTime, _cts.Token).Forget();
        //}



        //// Вызов кастомной логики в классе-наследнике (например, в Vehicle или Gun)
        //OnSpawn();

        Prepare(pool, handle, lifeTime);

        Activate();
    }

    /// <summary>
    /// Фаза 1: Связывание памяти. Объект извлечен, но его игровая логика еще спит.
    /// </summary>
    public PoolEntity Prepare(UltraVirtualPool pool, PoolHandle handle, float lifeTime)
    {
        _sourcePool = pool;
        _myHandle = handle;
        _lifeTime = lifeTime;

        InitPoolArray();

        return this; // Возвращаем себя для цепочки вызовов
    }

    /// <summary>
    /// Фаза 2: Точка входа в игру. Включает логику после полной конфигурации.
    /// </summary>
    public void Activate()
    {
        // Если задано время жизни, запускаем асинхронный таймер автоудаления
        if (_lifeTime > 0.001f)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            StartLifeTimeTimer(_lifeTime, _cts.Token).Forget();
        }

        // Вызов кастомной логики в классе-наследнике (например, в Vehicle или Gun)
        OnSpawn();
    }



    private async UniTaskVoid StartLifeTimeTimer(float delay, CancellationToken token)
    {
        // Ожидание без аллокаций памяти
        bool isCanceled = await UniTask.Delay(
            System.TimeSpan.FromSeconds(delay),
            delayTiming: PlayerLoopTiming.Update,
            cancellationToken: token
        ).SuppressCancellationThrow();

        // Если таймер не отменили досрочно, объект сам возвращает себя в пул
        if (!isCanceled)
        {
            SmartDespawn(Vector3.zero, ForceMode.Force);
        }
    }

    /// <summary>
    /// Безопасный возврат текущего объекта и ВСЕХ его дочерних элементов в пулы одной командой.
    /// </summary>
    public void SmartDespawn(Vector3 explosionForce, ForceMode forceMode)
    {
        if (_myHandle.IsValid && _sourcePool != null)
        {
            // Отменяем активный таймер жизни, чтобы он не сработал повторно в пуле
            _cts?.Cancel();

            // АВТОМАТИЗАЦИЯ: Объект сам принудительно очищает массив хэндлов, 
            // которые он заспавнил в себя (например, пушки внутри машины)
            RemoveAllActive(explosionForce, forceMode);

            // ШАГ 1: Сначала мгновенно и без аллокаций деспавним всех зарегистрированных "пассажиров"
            CleanUpInternalPoolObjects();

            // ШАГ 2: Вызываем пользовательскую логику очистки в наследнике
            OnDespawn();

            // ШАГ 3: Возвращаем саму сущность в её родной пул
            _sourcePool.DespawnSafe(_myHandle, explosionForce, forceMode);

            // Обнуляем хэндл, помечая объект как неактивный
            _myHandle = PoolHandle.Invalid;
        }
    }

    /// <summary>
    /// Мгновенная очистка привязанных элементов по прямым ссылкам из памяти (O(1) по скорости).
    /// </summary>
    private void CleanUpInternalPoolObjects()
    {
        // Бежим с конца нашего контролируемого списка
        for (int i = _attachedChildren.Count - 1; i >= 0; i--)
        {
            VirtualChild child = _attachedChildren[i];

            if (child != null && child.IsAttached)
            {
                PoolEntity childEntity = child.MyPoolEntity;

                // Отвязываем виртуальное родительство (сбрасываем физику PhysX).
                // Используем ForceMode.Force с нулевым вектором вместо несуществующего Discrete.
                child.Detach(Vector3.zero, ForceMode.Force);

                if (childEntity != null && childEntity.Handle.IsValid)
                {
                    // Отправляем дочерний элемент в его родной пул
                    childEntity.SmartDespawn(Vector3.zero, ForceMode.Force);
                }
            }
        }

        // На всякий случай гарантируем полную очистку списка связей
        _attachedChildren.Clear();
    }

    // Эти методы обязаны переопределить конкретные сущности (машины, пушки, ракеты)
    protected abstract void OnSpawn();
    protected abstract void OnDespawn();

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
