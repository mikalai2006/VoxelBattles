using UnityEngine;

[CreateAssetMenu]
public class GameRank : ScriptableObject
{
  [Space(5)]
  [Header("Основная информация")]
  public TextLocalize text;
  [Tooltip("Спрайт ранга")]
  public Sprite sprite;
}
