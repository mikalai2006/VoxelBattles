using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;
using UIToolkitLibrary;

public class UIDialog : UILocaleBase
{
  [SerializeField] private UIDocument _uiDoc;
  [SerializeField] private VisualTreeAsset _entityItem;
  private VisualElement _wrap;
  private VisualElement _sprite;
  private VisualElement _listEntity;
  private Label _headerText;
  private Label _messageText;
  private Button _buttonOk;
  private Button _buttonCancel;
  private TaskCompletionSource<DataDialogResult> _processCompletionSource;
  public UnityEvent processAction;
  private DataDialog _dataDialog;
  private DataDialogResult _dataResultDialog;
  private VisualElement _root;
  protected VisualElement _wrapper;

  private void Awake()
  {
    GameManager.OnChangeTheme += ChangeTheme;
    UIEvents.NeedCloseDialogs += OnClickCancel;
  }

  private void OnDestroy()
  {
    GameManager.OnChangeTheme -= ChangeTheme;
    UIEvents.NeedCloseDialogs -= OnClickCancel;
  }

  private void Start()
  {
    _root = _uiDoc.rootVisualElement;

    _wrapper = _root.Q<VisualElement>("DialogWrapper");

    // _wrapper.style.scale = new Vector3(0.1f, 0.1f, 0.1f);
    _wrapper.style.translate = new StyleTranslate(new Translate(-400, 0));
    _wrapper.experimental.animation.Position(new Vector3(0,0,0), 200).Ease(UnityEngine.UIElements.Experimental.Easing.OutSine); //.Scale(1f, 200);

    _headerText = _root.Q<Label>("HeaderText");
    _messageText = _root.Q<Label>("MessageText");
    _sprite = _root.Q<VisualElement>("Sprite");
    _listEntity = _root.Q<VisualElement>("ListEntity");
    _listEntity.Clear();


    _buttonOk = _root.Q<Button>("Ok");
    _buttonOk.clickable.clicked += OnClickOk;

    _buttonCancel = _root.Q<Button>("Cancel");
    _buttonCancel.style.display = DisplayStyle.None;
    _buttonCancel.clickable.clicked += OnClickCancel;

    ChangeTheme();

    base.Initialize(_root);
  }

  private void ChangeTheme()
  {
    _root.Q<VisualElement>("DialogWrapper").style.backgroundColor = new StyleColor(_gameManager.Theme.colorBgDialog);
    _sprite.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorBgDialog);
  }

  public async Task<DataDialogResult> ProcessAction(DataDialog dataDialog)
  {
    _dataResultDialog = new DataDialogResult();
    _dataDialog = dataDialog;
    
    _wrapper.style.alignSelf = _dataDialog.align;

    if (_dataDialog.width > 0)
    {
      _wrapper.style.width = _dataDialog.width;
    }

    if (!string.IsNullOrEmpty(_dataDialog.title)) _headerText.text = _dataDialog.title;
    if (!string.IsNullOrEmpty(_dataDialog.message)) _messageText.text = _dataDialog.message;

    if (_dataDialog.showCancelButton)
    {
      _buttonCancel.style.display = DisplayStyle.Flex;
    }

    if (_dataDialog.sprite != null)
    {
      _sprite.style.display = DisplayStyle.Flex;
      _sprite.style.backgroundImage = new StyleBackground(_dataDialog.sprite);
    }
    else
    {
      _sprite.style.display = DisplayStyle.None;
    }

    _listEntity.Clear();

    if (_dataDialog.innerElement != null)
    {
      _listEntity.Add(_dataDialog.innerElement);
    }

    // if (_dataDialog.entities != null && _dataDialog.entities.Count > 0)
    // {
    //   foreach (var entityItem in _dataDialog.entities)
    //   {

    //     var entityEl = _entityItem.Instantiate();

    //     entityEl.Q<VisualElement>("Img").style.backgroundImage = new StyleBackground(entityItem.entity.sprite);
    //     entityEl.Q<VisualElement>("Img").style.unityBackgroundImageTintColor = _gameManager.Theme.entityColor;

    //     entityEl.Q<Label>("Count").text = entityItem.count.ToString();
    //     entityEl.Q<Label>("Count").style.color = _gameManager.Theme.entityColor;
    //     // entityEl.Q<Label>("Name").text = await Helpers.GetLocaledString(entityItem.configEntity.text.title);
    //     entityEl.Q<Button>().clickable.clicked += async () =>
    //     {

    //       AudioManager.Instance.Click();
    //       _gameManager.InputManager.Disable();

    //       var title = await Helpers.GetLocaledString(entityItem.entity.text.title);
    //       var message = await Helpers.GetLocaledString(entityItem.entity.text.description);
    //       var dialog = new DialogProvider(new DataDialog()
    //       {
    //         title = title,
    //         sprite = entityItem.entity.sprite,
    //         message = message,
    //         showCancelButton = false
    //       });

    //       await dialog.ShowAndHide();
    //       _gameManager.InputManager.Enable();
    //     };

    //     _listEntity.Add(entityEl);
    //   }
    // }

    // if (_dataDialog.bonuses != null && _dataDialog.bonuses.Count > 0)
    // {
    //   foreach (var bonusItem in _dataDialog.bonuses)
    //   {

    //     var entityEl = _entityItem.Instantiate();

    //     entityEl.Q<VisualElement>("Img").style.backgroundImage = new StyleBackground(bonusItem.entity.sprite);
    //     entityEl.Q<VisualElement>("Img").style.unityBackgroundImageTintColor = _gameManager.Theme.entityColor;

    //     entityEl.Q<Label>("Count").text = bonusItem.count.ToString();
    //     entityEl.Q<Label>("Count").style.color = _gameManager.Theme.entityColor;
    //     // entityEl.Q<Label>("Name").text = await Helpers.GetLocaledString(entityItem.configEntity.text.title);
    //     entityEl.Q<Button>().clickable.clicked += async () =>
    //     {

    //       AudioManager.Instance.Click();
    //       _gameManager.InputManager.Disable();

    //       var title = await Helpers.GetLocaledString(bonusItem.entity.text.title);
    //       var message = await Helpers.GetLocaledString(bonusItem.entity.text.description);
    //       var dialog = new DialogProvider(new DataDialog()
    //       {
    //         title = title,
    //         sprite = bonusItem.entity.sprite,
    //         message = message,
    //         showCancelButton = false
    //       });

    //       await dialog.ShowAndHide();
    //       _gameManager.InputManager.Enable();
    //     };

    //     _listEntity.Add(entityEl);
    //   }
    // }


    _processCompletionSource = new TaskCompletionSource<DataDialogResult>();

    return await _processCompletionSource.Task;
  }

  private void OnClickOk()
  {
    AudioManager.Instance.Click();
    _dataResultDialog.isOk = true;
    _processCompletionSource.SetResult(_dataResultDialog);

    processAction?.Invoke();
  }

  private void OnClickCancel()
  {
    AudioManager.Instance.Click();
    _dataResultDialog.isOk = false;
    _processCompletionSource.SetResult(_dataResultDialog);

    processAction?.Invoke();
  }
}

