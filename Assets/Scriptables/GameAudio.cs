using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GameAudio : ScriptableObject
{
  [Range(0, 1)] public float volumeEffect;
  [Range(0, 1)] public float volumeMusic;
  public AudioClip clickButton;
  public AudioClip go;
  public AudioClip selectpuzzle;
  public AudioClip timerWarning;
  public AudioClip noMoreMoves;

  [Header("Lock")]
  public AudioClip lockAdd;
  public AudioClip lockIs;
  public AudioClip lockBreak;


  [Header("Pop")]
  public AudioClip popBig;
  public AudioClip[] pop;
  public AudioClip multipop;
  public AudioClip cascadePop;

  [Header("Bang")]
  public AudioClip bigBang;
  public AudioClip chuzzleBump;
  public AudioClip flash;
  public AudioClip manyChuzzles;

  [Header("Bottle")]
  public AudioClip bottled;
  public List<AudioClip> bubbles;

  [Header("Eye")]
  public AudioClip EyeChirp;
  public AudioClip EyesEscape;

  [Header("Level")]
  public AudioClip levelUp;
  public AudioClip bonusTing;
  public AudioClip bonusLife;
  public AudioClip ready;

}
