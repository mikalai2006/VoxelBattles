using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;


public abstract class UILocaleBase : MonoBehaviour
{
  [SerializeField] public LocalizedStringTable _localization;
  private VisualElement _box;

  private List<LocalizeObj> _elementList;
  protected GameManager _gameManager => GameManager.Instance;
  protected GameSetting _gameSetting => GameManager.Instance.Settings;
  protected AudioManager _audioManager => GameManager.Instance.audioManager;

  public virtual async void Initialize(VisualElement root)
  {
    _box = root;
    await Localize(root);
    Theming(_box);
  }

  public void Theming(VisualElement _root)
  {
    _box = _root;

    // // UQueryBuilder<VisualElement> builder = new UQueryBuilder<VisualElement>(_box);
    // // List<VisualElement> list = builder.Class("text-primary").ToList();
    // // foreach (var item in list)
    // // {
    // //   item.style.color = _gameManager.Theme.colorPrimary;
    // // }

    // UQueryBuilder<VisualElement> builderSecondary = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listSecondary = builderSecondary.Where(t => t.ClassListContains("text-secondary")).ToList();
    // foreach (var item in listSecondary)
    // {
    //   item.style.color = _gameManager.Theme.colorSecondary;
    // }

    // UQueryBuilder<VisualElement> builderDrag = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listDragEl = builderDrag.Class("unity-base-slider__dragger").ToList();
    // foreach (var item in listDragEl)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorSecondary;
    // }

    // // bg_secondary
    // UQueryBuilder<VisualElement> qBgSecondary = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBgSecondary = qBgSecondary.Class("bg_secondary").ToList();
    // foreach (var item in listBgSecondary)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorBgSecondary;
    // }
    
    // // bg_accent
    // UQueryBuilder<VisualElement> qBgAccent = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBgAccent = qBgAccent.Class("bg-accent").ToList();
    // foreach (var item in listBgAccent)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorBgAccent;
    // }

    // // bg_primary
    // UQueryBuilder<VisualElement> qBgPrimary = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBgPrimary = qBgPrimary.Class("bg-primary").ToList();
    // foreach (var item in listBgPrimary)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorBgPrimary;
    // }

    // // bg_success
    // UQueryBuilder<VisualElement> qBgSuccess = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBgSuccess = qBgSuccess.Class("bg-success").ToList();
    // foreach (var item in listBgSuccess)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorBgSuccess;
    // }

    // UQueryBuilder<VisualElement> builderInput = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listInputEl = builderInput.Class("unity-base-text-field__input").ToList();
    // foreach (var item in listInputEl)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //   item.style.color = _gameManager.Theme.colorTextInput;
    // }

    // UQueryBuilder<VisualElement> builderPopup = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listPopupEl = builderPopup.Class("unity-base-popup-field__input").ToList();
    // foreach (var item in listPopupEl)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //   item.style.color = _gameManager.Theme.colorTextInput;
    // }
    // UQueryBuilder<VisualElement> builderArrow = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listArrowEl = builderArrow.Class("unity-base-popup-field__arrow").ToList();
    // foreach (var item in listArrowEl)
    // {
    //   item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    // }

    // UQueryBuilder<VisualElement> builderCheck = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listCheckEl = builderCheck.Class("unity-toggle__checkmark").ToList();
    // foreach (var item in listCheckEl)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //   item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //   item.style.color = _gameManager.Theme.colorTextInput;
    // }

    // UQueryBuilder<Button> builderBtn = new UQueryBuilder<Button>(_box);
    // List<Button> listBtnEl = builderBtn.Class("button").ToList();
    // foreach (var item in listBtnEl)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorBgButton;
    // }

    // // button_secondary
    // UQueryBuilder<Button> qButtonSecondary = new UQueryBuilder<Button>(_box);
    // List<Button> listButtonSecondary = qButtonSecondary.Class("button-secondary").ToList();
    // foreach (var item in listButtonSecondary)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorButtonSecondary;
    // }
    
    // // button_accent
    // UQueryBuilder<Button> qButtonAccent = new UQueryBuilder<Button>(_box);
    // List<Button> listButtonAccent = qButtonAccent.Class("button-accent").ToList();
    // foreach (var item in listButtonAccent)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorButtonAccent;
    // }

    // // button_primary
    // UQueryBuilder<Button> qButtonPrimary = new UQueryBuilder<Button>(_box);
    // List<Button> listButtonPrimary = qButtonPrimary.Class("button-primary").ToList();
    // foreach (var item in listButtonPrimary)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorButtonPrimary;
    // }

    // // button_success
    // UQueryBuilder<Button> qButtonSuccess = new UQueryBuilder<Button>(_box);
    // List<Button> listButtonSuccess = qButtonSuccess.Class("button-success").ToList();
    // foreach (var item in listButtonSuccess)
    // {
    //   item.style.backgroundColor = _gameManager.Theme.colorButtonSuccess;
    // }

    // // UQueryBuilder<VisualElement> builderBgAccent = new UQueryBuilder<VisualElement>(_box);
    // // List<VisualElement> listBgAccentEl = builderBgAccent.Class("bg_accent").ToList();
    // // foreach (var item in listBgAccentEl)
    // // {
    // //     item.style.backgroundColor = _gameManager.Theme.colorAccent;
    // // }
    // // UQueryBuilder<VisualElement> builderBgSecondary = new UQueryBuilder<VisualElement>(_box);
    // // List<VisualElement> listBgSceondaryEl = builderBgSecondary.Class("bg_secondary").ToList();
    // // foreach (var item in listBgSceondaryEl)
    // // {
    // //     item.style.backgroundColor = _gameManager.Theme.colorSecondary;
    // // }

    // // UQueryBuilder<VisualElement> builderBtnAccent = new UQueryBuilder<VisualElement>(_box);
    // // List<VisualElement> listBtnAccentEl = builderBtnAccent.Class("button_accent").ToList();
    // // foreach (var item in listBtnAccentEl)
    // // {
    // //     item.style.backgroundColor = _gameManager.Theme.colorAccent;
    // // }
    // UQueryBuilder<VisualElement> builderLowBtns = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listLowBtns = builderLowBtns.Class("unity-scroller__low-button").ToList();
    // foreach (var item in listLowBtns)
    // {
    //   item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    // }

    // UQueryBuilder<VisualElement> builderHighBtns = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listHighBtns = builderHighBtns.Class("unity-scroller__high-button").ToList();
    // foreach (var item in listHighBtns)
    // {
    //   item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    // }

    // // tints

    // // bg-tint-secondary
    // UQueryBuilder<VisualElement> qBgTintSecondary = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBgTintSecondary = qBgTintSecondary.Class("bg-tint-secondary").ToList();
    // foreach (var item in listBgTintSecondary)
    // {
    //   item.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorBgSecondary);
    // }
    
    // // bg-tint-accent
    // UQueryBuilder<VisualElement> qBgTintAccent = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBgTintAccent = qBgTintAccent.Class("bg-tint-accent").ToList();
    // foreach (var item in listBgTintAccent)
    // {
    //   item.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorBgAccent);
    // }
    
    // // bg-tint-primary
    // UQueryBuilder<VisualElement> qBgTintPrimary = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBgTintPrimary = qBgTintPrimary.Class("bg-tint-primary").ToList();
    // foreach (var item in listBgTintPrimary)
    // {
    //   item.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorBgPrimary);
    // }
    
    // // bg-tint-white
    // UQueryBuilder<VisualElement> qBgTintWhite = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBgTintWhite = qBgTintWhite.Class("bg-tint-white").ToList();
    // foreach (var item in listBgTintWhite)
    // {
    //   item.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorWhite);
    // }

    // // outlines
    // // outline-black
    // UQueryBuilder<Label> qOutlineBlack = new UQueryBuilder<Label>(_box);
    // List<Label> listOutlineBlack = qOutlineBlack.Class("outline-black").ToList();
    // foreach (var item in listOutlineBlack)
    // {
    //   item.style.unityTextOutlineColor = new StyleColor(_gameManager.Theme.colorOutlineBlack);
    //   item.style.unityTextOutlineWidth = _gameManager.Theme.widthOutline;
    // }
    // // outline-white
    // UQueryBuilder<Label> qOutlineWhite = new UQueryBuilder<Label>(_box);
    // List<Label> listOutlineWhite = qOutlineWhite.Class("outline-white").ToList();
    // foreach (var item in listOutlineWhite)
    // {
    //   item.style.unityTextOutlineColor = new StyleColor(_gameManager.Theme.colorOutlineWhite);
    //   item.style.unityTextOutlineWidth = _gameManager.Theme.widthOutline;
    // }


    // // borders

    // // border-accent
    // UQueryBuilder<VisualElement> qBorderAccent = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBorderAccent = qBorderAccent.Class("border-accent").ToList();
    // foreach (var item in listBorderAccent)
    // {
    //   item.style.borderTopColor = _gameManager.Theme.colorBgAccent;
    //   item.style.borderBottomColor = _gameManager.Theme.colorBgAccent;
    //   item.style.borderLeftColor = _gameManager.Theme.colorBgAccent;
    //   item.style.borderRightColor = _gameManager.Theme.colorBgAccent;
    // }
    // // border-secondary
    // UQueryBuilder<VisualElement> qBorderSecondary = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBorderSecondary = qBorderSecondary.Class("border-secondary").ToList();
    // foreach (var item in listBorderSecondary)
    // {
    //   item.style.borderTopColor = _gameManager.Theme.colorBgSecondary;
    //   item.style.borderBottomColor = _gameManager.Theme.colorBgSecondary;
    //   item.style.borderLeftColor = _gameManager.Theme.colorBgSecondary;
    //   item.style.borderRightColor = _gameManager.Theme.colorBgSecondary;
    // }
    // // border-primary
    // UQueryBuilder<VisualElement> qBorderPrimary = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBorderPrimary = qBorderPrimary.Class("border-primary").ToList();
    // foreach (var item in listBorderPrimary)
    // {
    //   item.style.borderTopColor = _gameManager.Theme.colorBgPrimary;
    //   item.style.borderBottomColor = _gameManager.Theme.colorBgPrimary;
    //   item.style.borderLeftColor = _gameManager.Theme.colorBgPrimary;
    //   item.style.borderRightColor = _gameManager.Theme.colorBgPrimary;
    // }
    // // border-success
    // UQueryBuilder<VisualElement> qBorderSuccess = new UQueryBuilder<VisualElement>(_box);
    // List<VisualElement> listBorderSuccess = qBorderSuccess.Class("border-success").ToList();
    // foreach (var item in listBorderSuccess)
    // {
    //   item.style.borderTopColor = _gameManager.Theme.colorBgSuccess;
    //   item.style.borderBottomColor = _gameManager.Theme.colorBgSuccess;
    //   item.style.borderLeftColor = _gameManager.Theme.colorBgSuccess;
    //   item.style.borderRightColor = _gameManager.Theme.colorBgSuccess;
    // }
  }

  public async UniTask Localize(VisualElement root)
  {
    await LocalizationSettings.InitializationOperation.Task;

    var op = _localization.GetTableAsync();
    await op.Task;
    _elementList = HelperUI.FindAllTextElements(root);
    OnTableLoaded(op);
  }

  private void OnTableLoaded(AsyncOperationHandle<StringTable> op)
  {
    StringTable table = op.Result;

    foreach (var item in _elementList)
    {
      var entry = op.Result[item.Key];
      if (entry != null)
        item.Element.text = entry.LocalizedValue;
      else
        Debug.LogWarning($"No {op.Result.LocaleIdentifier.Code} translation for key: '{item.Key}'");
    }
  }
}

