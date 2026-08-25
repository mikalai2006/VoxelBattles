// using Assets;

// using Cysharp.Threading.Tasks;

// public class UIThropsOperation : LocalAssetLoader
// {
//   // public UISettingsOperation()
//   // {
//   // }

//   public async UniTask<DataDialogResult> ShowAndHide()
//   {
//     var window = await Load();
//     var result = await window.ProcessAction();
//     Unload();
//     return result;
//   }

//   public UniTask<UIThrops> Load()
//   {
//     return LoadInternal<UIThrops>(ConstantsApp.UILabels.UI_THROPHS);
//   }

//   public void Unload()
//   {
//     UnloadInternal();
//   }
// }