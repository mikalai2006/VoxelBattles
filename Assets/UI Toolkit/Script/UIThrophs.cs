// using System.Linq;
// using System.Threading.Tasks;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using TMPro;

// public class UIThrops : MonoBehaviour
// {
//   protected GameManager _gameManager => GameManager.Instance;
//   public UnityEngine.UI.Button _btnBack;
//   public GameObject ContentThrophs;
//   private TaskCompletionSource<DataDialogResult> _processCompletionSource;
//   private DataDialogResult _result;
  
//   public GameObject Info;
//   public TextMeshProUGUI title;
//   public TextMeshProUGUI description;
//   public UnityEngine.UI.Image image;


//   public void Start()
//   {
//     Info.SetActive(false);
//     // var gameState = JsonUtility.FromJson<StateGame>(PlayerPrefs.GetString(_gameManager.Settings.nameSaveData));
//     // bool isSavedGame = false;
//     // if (gameState != null && gameState.games.Count > 0)
//     // {
//     //   isSavedGame = gameState.games.Find(t => t.typeGame == _gameManager.GameType.typeGame) != null;
//     // }

//     _btnBack.onClick.AddListener(() =>
//     {
//       ClickBackToStartMenu();
//     });


//     CreateThrophs();
//   }

//     // void OnDestroy()
//     // {

//     //     Time.timeScale = 1;
//     // }


//     public async UniTask<DataDialogResult> ProcessAction()
//   {
//     _result = new DataDialogResult();

//     _processCompletionSource = new TaskCompletionSource<DataDialogResult>();

//     return await _processCompletionSource.Task;
//   }

  
//   public void CreateThrophs()
//   {
//     BaseThroph[] monoThrops = ContentThrophs.GetComponentsInChildren<BaseThroph>();
//     var allThrophsConfig = _gameManager.Settings.throphs;

//     for (int k = 0; k < monoThrops.Count(); k++)
//       {
//         var config = allThrophsConfig.ToList().Find(t => t.idEntity == monoThrops[k].name);
//         // Debug.Log($"throphs idEntity=" + config.idEntity + ", name = " + monoThrops[k].name);
//         monoThrops[k].SetConfig(config, this);
//       }

//     // for (int k = 0; k < allThrophsConfig.Count(); k++)
//     //   {
//     //       var gameObj = LeanPool.Spawn(
//     //         allThrophsConfig[k].prefab,
//     //         ContentThrophs.gameObject.transform.position,
//     //         Quaternion.identity,
//     //         ContentThrophs.gameObject.transform
//     //         );

//     //       gameObj.SetConfig(allThrophsConfig[k], this);
//     //   }
//   }

//   public void ShowText(GameBonus config, bool exist) {
//     Info.SetActive(true);

//     title.text = config.text.title.GetLocalizedString();
//     description.text = config.text.description.GetLocalizedString();
//     image.sprite = config.sprite;
//     image.SetNativeSize();

//     if (exist == false) {
//       Color col = Color.black;
//       col.a = 0.8f;
//       image.color = col;
//     } else {
//       Color col = Color.white;
//       col.a = 1f;
//       image.color = col;
//     }
//   }

//   public void HideText() {
//     Info.SetActive(false);
    
//     AudioManager.Instance.Click();
//   }

//   private void ClickBackToStartMenu()
//   {
//     AudioManager.Instance.Click();

//     _result.isOk = false;

//     _processCompletionSource.SetResult(_result);
//   }
// }
