using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using User;
using UnityEngine.Events;

public class InitUserWindow : UILocaleBase
{
  [SerializeField] private UIDocument _uiDoc;
  public UIDocument MenuApp => _uiDoc;

  private readonly string _nameFieldLogin = "Login";
  private readonly string _nameButtonLogin = "ButtonLogin";

  private const int MIN_LENGTH_NAME = 3;
  private TextField _fieldName;
  private Button _buttonLogin;
  private VisualElement _form;
  private AppInfoContainer _result = new();

  private TaskCompletionSource<AppInfoContainer> _loginCompletionSource;


  public UnityEvent loginAction;

  private void Start()
  {
    _form = MenuApp.rootVisualElement.Q<VisualElement>("Form");

    _fieldName = MenuApp.rootVisualElement.Q<TextField>(_nameFieldLogin);
    _fieldName.RegisterCallback<InputEvent>(e =>
    {
      OnValidFormField();
    });

    _buttonLogin = MenuApp.rootVisualElement.Q<Button>(_nameButtonLogin);
    _buttonLogin.clickable.clicked += () =>
    {
      _buttonLogin.SetEnabled(false);
      OnSimpleLoginClicked();
      _buttonLogin.SetEnabled(true);
    };

    OnValidFormField();

    _form.style.backgroundColor = _gameManager.Theme.bgColor;

    base.Initialize(_uiDoc.rootVisualElement);

  }

  public async Task<AppInfoContainer> ProcessLogin()
  {
    _loginCompletionSource = new TaskCompletionSource<AppInfoContainer>();

    return await _loginCompletionSource.Task;
  }

  private void OnValidFormField()
  {
    if (_fieldName.text.Length < MIN_LENGTH_NAME)
    {
      _buttonLogin.SetEnabled(false);
    }
    else
    {
      _buttonLogin.SetEnabled(true);
    }
  }

  private void OnSimpleLoginClicked()
  {

    if (_fieldName.text.Length < MIN_LENGTH_NAME)
    {
      return;
    }

    _result.UserInfo.name = _fieldName.text;

    _loginCompletionSource.SetResult(_result);

    loginAction?.Invoke();
  }
}

