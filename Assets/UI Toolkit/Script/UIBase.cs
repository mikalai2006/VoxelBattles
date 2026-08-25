using UnityEngine;
using UnityEngine.UIElements;

public class UIBase : UILocaleBase
{
  [SerializeField] private UIDocument _uiDoc;
  [SerializeField] private VisualTreeAsset _templateInsertBlok;
  protected Label Title;
  protected VisualElement Wrapper;
  protected VisualElement Panel;
  protected VisualElement root;
  protected Button CloseButton;

  public virtual void Awake()
  {
    GameManager.OnChangeTheme += ChangeTheme;
  }

  public virtual void OnDestroy()
  {
    GameManager.OnChangeTheme -= ChangeTheme;

  }

  public virtual void Start()
  {
    root = _uiDoc.rootVisualElement;

    Title = root.Q<Label>("Title");

    Wrapper = root.Q<VisualElement>("Wrapper");

    Panel = root.Q<VisualElement>("Panel");

    CloseButton = root.Q<Button>("CloseBtn");

    VisualElement docDialogBlok = _templateInsertBlok.Instantiate();
    docDialogBlok.style.flexGrow = 1;
    Wrapper.Clear();
    Wrapper.Add(docDialogBlok);

    ChangeTheme();

    base.Initialize(root);
  }

  private void ChangeTheme()
  {
    VisualElement imgCloseButton = CloseButton.Q<VisualElement>("Img");
    imgCloseButton.style.backgroundImage = new StyleBackground(_gameSetting.spriteClose);
    imgCloseButton.style.unityBackgroundImageTintColor = _gameManager.Theme.colorSecondary;

    Panel.style.backgroundColor = _gameManager.Theme.colorBgDialog;
  }
}
