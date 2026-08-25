using UnityEngine.UIElements;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Localization.Tables;
using UnityEngine;
using UnityEngine.Localization;

namespace UIToolkitLibrary
{
    public class UIView : IDisposable
    {
        protected GameManager _gameManager => GameManager.Instance;
        LocalizedStringTable _localization;
        private List<LocalizeObj> _elementList;
        protected bool m_HideOnAwake = true;

        // UI reveals other underlaying UIs, partially see-through
        protected bool m_IsOverlay;

        protected VisualElement m_TopElement;
        VisualElement m_Hint;
        static class StyleClasses
        {
            public static string Hint = "Hint";
        }

        // Properties
        public VisualElement Root => m_TopElement;
        public bool IsTransparent => m_IsOverlay;
        public bool IsHidden => m_TopElement.style.display == DisplayStyle.None;

        // Constructor
        /// <summary>
        /// Initializes a new instance of the UIView class.
        /// </summary>
        /// <param name="topElement">The topmost VisualElement in the UXML hierarchy.</param>
        public UIView(VisualElement topElement, LocalizedStringTable localization)
        {
            m_TopElement = topElement ?? throw new ArgumentNullException(nameof(topElement));

            _localization = localization;

            Initialize();

            Theming(m_TopElement);

            Localize(m_TopElement).Forget();
        }

        public virtual void Initialize()
        {
            m_Hint = m_TopElement.Q<VisualElement>(StyleClasses.Hint);

            SetVisualElements();
            RegisterButtonCallbacks();
            
            if (m_HideOnAwake)
            {
                Hide();
            }

        }

        // Sets up the VisualElements for the UI. Override to customize.
        protected virtual void SetVisualElements()
        {

        }

        // Registers callbacks for buttons in the UI. Override to customize.
        protected virtual void RegisterButtonCallbacks()
        {

        }

        // Displays the UI.
        public virtual void Show()
        {
            m_TopElement.style.display = DisplayStyle.Flex;
        }

        // Hides the UI.
        public virtual void Hide()
        {
            m_TopElement.style.display = DisplayStyle.None;
        }

        // Unregisters any callbacks or event handlers. Override to customize.
        public virtual void Dispose()
        {

        }
        
        protected void ShowHint(VisualElement hint)
        {
            m_Hint.Clear();
            m_Hint.style.display = DisplayStyle.Flex;
            
            m_Hint.AddToClassList("hint_wrapper");
            m_Hint.style.backgroundColor = new StyleColor(_gameManager.Theme.colorBgHint);

            m_Hint.Add(hint);
        }

        protected void HideHint()
        {
            if (m_Hint != null)
            {
                m_Hint.style.display = DisplayStyle.None;
                m_Hint.Clear();
            }
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

        public void Theming(VisualElement _root)
        {
            var _box = _root;

            // System.IO.File.WriteAllText("Assets/UI/Styles/Theme.uss", ".my-class { color: red; }");

    //         UQueryBuilder<VisualElement> builder = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> list = builder.Class("text-primary").ToList();
    //         foreach (var item in list)
    //         {
    //         item.style.color = _gameManager.Theme.colorPrimary;
    //         }

    //         UQueryBuilder<VisualElement> builderSecondary = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listSecondary = builderSecondary.Where(t => t.ClassListContains("text-secondary")).ToList();
    //         foreach (var item in listSecondary)
    //         {
    //         item.style.color = _gameManager.Theme.colorSecondary;
    //         }

    //         UQueryBuilder<VisualElement> builderDrag = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listDragEl = builderDrag.Class("unity-base-slider__dragger").ToList();
    //         foreach (var item in listDragEl)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorSecondary;
    //         }

    //         // bg_secondary
    //         UQueryBuilder<VisualElement> qBgSecondary = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBgSecondary = qBgSecondary.Class("bg_secondary").ToList();
    //         foreach (var item in listBgSecondary)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorBgSecondary;
    //         }
            
    //         // bg_accent
    //         UQueryBuilder<VisualElement> qBgAccent = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBgAccent = qBgAccent.Class("bg-accent").ToList();
    //         foreach (var item in listBgAccent)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorBgAccent;
    //         }

    //         // bg_primary
    //         UQueryBuilder<VisualElement> qBgPrimary = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBgPrimary = qBgPrimary.Class("bg-primary").ToList();
    //         foreach (var item in listBgPrimary)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorBgPrimary;
    //         }

    //         // bg_success
    //         UQueryBuilder<VisualElement> qBgSuccess = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBgSuccess = qBgSuccess.Class("bg-success").ToList();
    //         foreach (var item in listBgSuccess)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorBgSuccess;
    //         }

    //         // borders
    //         // border-accent
    //         UQueryBuilder<VisualElement> qBorderAccent = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBorderAccent = qBorderAccent.Class("border-accent").ToList();
    //         foreach (var item in listBorderAccent)
    //         {
    //             item.style.borderTopColor = _gameManager.Theme.colorBgAccent;
    //             item.style.borderBottomColor = _gameManager.Theme.colorBgAccent;
    //             item.style.borderLeftColor = _gameManager.Theme.colorBgAccent;
    //             item.style.borderRightColor = _gameManager.Theme.colorBgAccent;
    //         }

    //         UQueryBuilder<VisualElement> builderInput = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listInputEl = builderInput.Class("unity-base-text-field__input").ToList();
    //         foreach (var item in listInputEl)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //         item.style.color = _gameManager.Theme.colorTextInput;
    //         }

    //         UQueryBuilder<VisualElement> builderPopup = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listPopupEl = builderPopup.Class("unity-base-popup-field__input").ToList();
    //         foreach (var item in listPopupEl)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //         item.style.color = _gameManager.Theme.colorTextInput;
    //         }
    //         UQueryBuilder<VisualElement> builderArrow = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listArrowEl = builderArrow.Class("unity-base-popup-field__arrow").ToList();
    //         foreach (var item in listArrowEl)
    //         {
    //         item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //         }

    //         UQueryBuilder<VisualElement> builderCheck = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listCheckEl = builderCheck.Class("unity-toggle__checkmark").ToList();
    //         foreach (var item in listCheckEl)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //         item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //         item.style.color = _gameManager.Theme.colorTextInput;
    //         }

    //         UQueryBuilder<Button> builderBtn = new UQueryBuilder<Button>(_box);
    //         List<Button> listBtnEl = builderBtn.Class("button").ToList();
    //         foreach (var item in listBtnEl)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorBgButton;
    //         }

    //         // button_secondary
    //         UQueryBuilder<Button> qButtonSecondary = new UQueryBuilder<Button>(_box);
    //         List<Button> listButtonSecondary = qButtonSecondary.Class("button-secondary").ToList();
    //         foreach (var item in listButtonSecondary)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorButtonSecondary;
    //         }
            
    //         // button_accent
    //         UQueryBuilder<Button> qButtonAccent = new UQueryBuilder<Button>(_box);
    //         List<Button> listButtonAccent = qButtonAccent.Class("button-accent").ToList();
    //         foreach (var item in listButtonAccent)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorButtonAccent;
    //         }

    //         // button_primary
    //         UQueryBuilder<Button> qButtonPrimary = new UQueryBuilder<Button>(_box);
    //         List<Button> listButtonPrimary = qButtonPrimary.Class("button-primary").ToList();
    //         foreach (var item in listButtonPrimary)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorButtonPrimary;
    //         }

    //         // button_success
    //         UQueryBuilder<Button> qButtonSuccess = new UQueryBuilder<Button>(_box);
    //         List<Button> listButtonSuccess = qButtonSuccess.Class("button-success").ToList();
    //         foreach (var item in listButtonSuccess)
    //         {
    //         item.style.backgroundColor = _gameManager.Theme.colorButtonSuccess;
    //         }

    //         // UQueryBuilder<VisualElement> builderBgAccent = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listBgAccentEl = builderBgAccent.Class("bg_accent").ToList();
    //         // foreach (var item in listBgAccentEl)
    //         // {
    //         //     item.style.backgroundColor = _gameManager.Theme.colorAccent;
    //         // }
    //         // UQueryBuilder<VisualElement> builderBgSecondary = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listBgSceondaryEl = builderBgSecondary.Class("bg_secondary").ToList();
    //         // foreach (var item in listBgSceondaryEl)
    //         // {
    //         //     item.style.backgroundColor = _gameManager.Theme.colorSecondary;
    //         // }

    //         // UQueryBuilder<VisualElement> builderBtnAccent = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listBtnAccentEl = builderBtnAccent.Class("button_accent").ToList();
    //         // foreach (var item in listBtnAccentEl)
    //         // {
    //         //     item.style.backgroundColor = _gameManager.Theme.colorAccent;
    //         // }

    //         // bg_tint_secondary
    //         UQueryBuilder<VisualElement> qBgTintSecondary = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBgTintSecondary = qBgTintSecondary.Class("bg-tint-secondary").ToList();
    //         foreach (var item in listBgTintSecondary)
    //         {
    //         item.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorBgSecondary);
    //         }

    //         // bg_tint_accent
    //         UQueryBuilder<VisualElement> qBgTintAccent = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBgTintAccent = qBgTintAccent.Class("bg-tint-accent").ToList();
    //         foreach (var item in listBgTintAccent)
    //         {
    //             item.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorBgAccent);
    //         }
            
    //         // bg-tint-primary
    //         UQueryBuilder<VisualElement> qBgTintPrimary = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBgTintPrimary = qBgTintPrimary.Class("bg-tint-primary").ToList();
    //         foreach (var item in listBgTintPrimary)
    //         {
    //         item.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorBgPrimary);
    //         }
    
    //         // bg-tint-white
    //         UQueryBuilder<VisualElement> qBgTintWhite = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listBgTintWhite = qBgTintWhite.Class("bg-tint-white").ToList();
    //         foreach (var item in listBgTintWhite)
    //         {
    //         item.style.unityBackgroundImageTintColor = new StyleColor(_gameManager.Theme.colorWhite);
    //         }

    //         UQueryBuilder<VisualElement> builderLowBtns = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listLowBtns = builderLowBtns.Class("unity-scroller__low-button").ToList();
    //         foreach (var item in listLowBtns)
    //         {
    //         item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //         }

    //         UQueryBuilder<VisualElement> builderHighBtns = new UQueryBuilder<VisualElement>(_box);
    //         List<VisualElement> listHighBtns = builderHighBtns.Class("unity-scroller__high-button").ToList();
    //         foreach (var item in listHighBtns)
    //         {
    //         item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //         }
            
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

    //         // UQueryBuilder<VisualElement> builder = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> list = builder.Class("text-primary").ToList();
    //         // foreach (var item in list)
    //         // {
    //         // item.style.color = _gameManager.Theme.colorPrimary;
    //         // }

    //         // UQueryBuilder<VisualElement> builderSecondary = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listSecondary = builderSecondary.Where(t => t.ClassListContains("text-secondary")).ToList();
    //         // foreach (var item in listSecondary)
    //         // {
    //         // item.style.color = _gameManager.Theme.colorSecondary;
    //         // }

    //         // UQueryBuilder<VisualElement> builderDrag = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listDragEl = builderDrag.Class("unity-base-slider__dragger").ToList();
    //         // foreach (var item in listDragEl)
    //         // {
    //         // item.style.backgroundColor = _gameManager.Theme.colorSecondary;
    //         // }

    //         // UQueryBuilder<VisualElement> builderInput = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listInputEl = builderInput.Class("unity-base-text-field__input").ToList();
    //         // foreach (var item in listInputEl)
    //         // {
    //         // item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //         // item.style.color = _gameManager.Theme.colorTextInput;
    //         // }

    //         // UQueryBuilder<VisualElement> builderPopup = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listPopupEl = builderPopup.Class("unity-base-popup-field__input").ToList();
    //         // foreach (var item in listPopupEl)
    //         // {
    //         // item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //         // item.style.color = _gameManager.Theme.colorTextInput;
    //         // }
    //         // UQueryBuilder<VisualElement> builderArrow = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listArrowEl = builderArrow.Class("unity-base-popup-field__arrow").ToList();
    //         // foreach (var item in listArrowEl)
    //         // {
    //         // item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //         // }

    //         // UQueryBuilder<VisualElement> builderCheck = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listCheckEl = builderCheck.Class("unity-toggle__checkmark").ToList();
    //         // foreach (var item in listCheckEl)
    //         // {
    //         // item.style.backgroundColor = _gameManager.Theme.colorBgInput;
    //         // item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //         // item.style.color = _gameManager.Theme.colorTextInput;
    //         // }

    //         // UQueryBuilder<VisualElement> builderBtn = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listBtnEl = builderBtn.Class("button").ToList();
    //         // foreach (var item in listBtnEl)
    //         // {
    //         // item.style.backgroundColor = _gameManager.Theme.colorBgButton;
    //         // }

            
    //         // UQueryBuilder<VisualElement> builderBtnSuccess = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listBtnSuccessEl = builderBtnSuccess.Class("button_success").ToList();
    //         // foreach (var item in listBtnSuccessEl)
    //         // {
    //         //     item.style.backgroundColor = _gameManager.Theme.colorCompleted;
    //         // }

    //         // UQueryBuilder<VisualElement> builderBtnAccent = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listBtnAccentEl = builderBtnAccent.Class("button_accent").ToList();
    //         // foreach (var item in listBtnAccentEl)
    //         // {
    //         //     item.style.backgroundColor = _gameManager.Theme.colorAccent;
    //         // }

    //         // UQueryBuilder<VisualElement> builderLowBtns = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listLowBtns = builderLowBtns.Class("unity-scroller__low-button").ToList();
    //         // foreach (var item in listLowBtns)
    //         // {
    //         // item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //         // }

    //         // UQueryBuilder<VisualElement> builderHighBtns = new UQueryBuilder<VisualElement>(_box);
    //         // List<VisualElement> listHighBtns = builderHighBtns.Class("unity-scroller__high-button").ToList();
    //         // foreach (var item in listHighBtns)
    //         // {
    //         // item.style.unityBackgroundImageTintColor = _gameManager.Theme.colorTextInput;
    //         // }
        }
    }
}

