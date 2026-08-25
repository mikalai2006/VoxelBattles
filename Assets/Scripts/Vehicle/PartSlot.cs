using UnityEngine;

public class PartSlot : MonoBehaviour
{
    public enum SlotType { Chassis, MainWeapon }
    public SlotType slotType;
    public GameObject CurrentInstalledPart { get; set; }
}
