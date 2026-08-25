using UnityEngine;

public class Vehicle : MonoBehaviour
{
    // Кэшируем интерфейсы, а не конкретные классы
    //public IMovable Movement { get; private set; }
    public IShootable Weapon { get; private set; }

    private void Awake()
    {
        // Автоматически ищем компоненты на этом же GameObject
        //Movement = GetComponent<IMovable>();
        Weapon = GetComponent<IShootable>();
    }

    // Вспомогательные свойства для быстрой проверки
    //public bool CanMove => Movement != null;
    public bool HasWeapon => Weapon != null;
}
