using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu]
public class GameTheme : ScriptableObject
{

  [Space(5)]
  [Header("UI")]
  public Color colorBg;
  public Color colorListItemBg;
  public Color colorListItemBorder;
  public int padding;
  public int margin;



  // [Space(5)]
  // [Header("Game")]
  // public Sprite bgImage;
  // public Sprite timerProgressBg;
  // public Sprite timerProgress;
  // public Sprite timerBg;
  // public Sprite bgGrid;
  // public Sprite imageWarning;
  // public Color colorLockLine;

  // [Space(5)]
  // [Header("Hints")]
  // public Color colorHintCircleFrom;
  // public Color colorHintCircleTo;
  // public Color colorHintLineStart;
  // public Color colorHintLineEnd;
  // public Color colorHintStarBtn;
  // public Color colorHintStarBtnSecond;

  // [Space(5)]
  // [Header("Entity")]
  // public Color colorEntityBliss;
  // public Color colorEntitySymbol;
  // public Color entityColor;

  [Space(5)]
  [Header("Game")]
  public Color colorActive;
  public Color colorCompleted;
  public Color bgColor;
  public Color colorPrimary;
  public Color colorSecondary;
  public Color colorAccent;
  // public Color colorHead;
  
  [Header("GameScreen")]
  public Color colorWrapperGameScreen;

  [Header("InfoBox")]
  public Color colorBgInfoRow;
  public Color colorTextInfoRow;

  [Space(5)]
  [Header("UI")]
  [Range(0, 20)] public int widthOutline;
  public Color colorOutlineWhite;
  public Color colorOutlineBlack;
  public Color colorWhite;
  public Color colorBgHint;
  public Color colorBgInput;
  public Color colorBgSecondary;
  public Color colorBgPrimary;
  public Color colorBgAccent;
  public Color colorBgSuccess;
  // public Color colorTintAccent;
  public Color colorButtonAccent;
  public Color colorButtonSecondary;
  public Color colorButtonPrimary;
  public Color colorButtonSuccess;
  public Color colorTextInput;
  // public Color colorBgTopSide;
  public Color colorBgButton;
  public Color colorBgDialog;

  [Space(5)]
  [Header("Images")]
  public Sprite spriteAva;
  public Sprite spriteFill;
  public Sprite spriteSettings;
  public Sprite spriteCoin;
  public Sprite spriteCubes;
  public Sprite spritePlus;
  public Sprite spriteAngar;
  public Sprite spriteShop;
  public Sprite spriteBattle;
  public Sprite spriteArrow;
}
