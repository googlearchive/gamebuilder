/*
 * Copyright 2019 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicPopup : MonoBehaviour
{
  [SerializeField] PopupMessageUI popupMessageUI;
  [SerializeField] GameObject containerObject;

  public class Popup
  {
    public System.Func<string> getMessage;

    // If not null, this will be polled every frame to decide if the popup should keep showing or not.
    public System.Func<bool> keepShowing;

    public bool isCancellable = false;

    public System.Action onCancel = null;

    public List<PopupButton.Params> buttons;
    public string textFieldText = null;  // null means no text field; otherwise, it's the text in it.

    public bool fullWidthButtons = false;

    public float textWrapWidth = 0f;

    // Please only use in special cases!! Like "Save before quit?" dialog.
    public bool forceImmediateTakeover = false;
  }

  List<PopupButton> buttonInstances = new List<PopupButton>();
  TMPro.TMP_InputField textFieldInstance;

  Queue<Popup> popupQueue = new Queue<Popup>();
  Popup currentPopup = null;

  float timeOfLastOpen = -1;

  void Awake()
  {
    containerObject.SetActive(false);
  }

  public bool IsOpen()
  {
    return containerObject.activeSelf;
  }

  public bool IsCancellable()
  {
    return currentPopup.isCancellable;
  }

  float GetTimeSinceLastOpen()
  {
    return Time.realtimeSinceStartup - timeOfLastOpen;
  }

  void Update()
  {
    if (IsOpen() && currentPopup != null)
    {
      popupMessageUI.textField.text = currentPopup.getMessage();

      if (currentPopup.keepShowing != null && currentPopup.keepShowing() == false)
      {
        Close();
      }
    }
  }

  void Close()
  {
    if (currentPopup == null) return;
    containerObject.SetActive(false);
    currentPopup = null;
    foreach (var inst in buttonInstances)
    {
      GameObject.Destroy(inst.gameObject);
    }
    buttonInstances.Clear();
    if (textFieldInstance != null)
    {
      GameObject.Destroy(textFieldInstance.gameObject);
      textFieldInstance = null;
    }
    PumpQueue();
  }

  public void Cancel()
  {
    if (GetTimeSinceLastOpen() < 0.5f) return;
    if (currentPopup == null) return;
    currentPopup.onCancel?.Invoke();
    Close();
  }

  void PumpQueue()
  {
    if (this == null) return;

    if (currentPopup != null)
    {
      // Still showing another one - do nothing;
      return;
    }

    if (popupQueue.Count > 0)
    {
      currentPopup = popupQueue.Dequeue();

      popupMessageUI.buttonParent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = currentPopup.fullWidthButtons;

      var layout = popupMessageUI.textField.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
      if (currentPopup.textWrapWidth > 0f)
      {
        if (layout == null)
        {
          layout = popupMessageUI.textField.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        }
        layout.preferredWidth = currentPopup.textWrapWidth;
      }
      else
      {
        if (layout != null)
        {
          MonoBehaviour.Destroy(layout);
        }
      }


      if (currentPopup.textFieldText != null)
      {
        var obj = GameObject.Instantiate(popupMessageUI.textFieldPrefab, popupMessageUI.buttonParent);
        textFieldInstance = obj.GetComponent<TMPro.TMP_InputField>();
        textFieldInstance.text = currentPopup.textFieldText;
      }

      if (currentPopup.buttons != null)
      {
        foreach (var buttonParams in currentPopup.buttons)
        {
          var instance = GameObject.Instantiate(popupMessageUI.buttonPrefab, popupMessageUI.buttonParent);
          var buttonInstance = instance.GetComponent<PopupButton>();
          var wrappedParams = buttonParams;
          wrappedParams.onClick = () =>
          {
            buttonParams.onClick?.Invoke();
            Close();
          };
          buttonInstance.Show(wrappedParams);
          buttonInstances.Add(buttonInstance);

          // HACK - input field submit is the same as clicking the first button
          if (buttonInstances.Count == 1 && textFieldInstance != null)
          {
            textFieldInstance.onSubmit.AddListener(unused => wrappedParams.onClick.Invoke());
          }
        }
      }

      // Immediately set text to avoid flicker.
      popupMessageUI.textField.text = currentPopup.getMessage();

      transform.SetAsLastSibling();
      containerObject.SetActive(true);

      // Focus text field, if there is one
      if (textFieldInstance != null)
      {
        textFieldInstance.Select();
      }

      timeOfLastOpen = Time.realtimeSinceStartup;
    }
  }

  public void Show(string message, string buttonLabel, System.Action onClosed = null, float textWrapWidth = 0f)
  {
    var buttons = new List<PopupButton.Params>();
    buttons.Add(new PopupButton.Params { getLabel = () => buttonLabel, onClick = onClosed });
    Show(new Popup { getMessage = () => message, buttons = buttons, textWrapWidth = textWrapWidth });
  }

  public void ShowWithCancel(string message, string buttonLabel, System.Action onButtonClicked, float textWrapWidth = 0f)
  {
    var buttons = new List<PopupButton.Params>();
    buttons.Add(new PopupButton.Params { getLabel = () => buttonLabel, onClick = onButtonClicked });
    buttons.Add(new PopupButton.Params { getLabel = () => "Cancel", onClick = () => { } });
    Show(new Popup { getMessage = () => message, buttons = buttons, textWrapWidth = textWrapWidth, isCancellable = true });
  }

  public void ShowTextInput(string message, string inputText, System.Action<string> onOkAction, System.Action onCancelAction = null)
  {
    onCancelAction = onCancelAction ?? (() => { });
    var buttons = new List<PopupButton.Params>();
    buttons.Add(new PopupButton.Params { getLabel = () => "OK", onClick = () => onOkAction.Invoke(GetTextFieldText()) });
    buttons.Add(new PopupButton.Params { getLabel = () => "Cancel", onClick = () => onCancelAction.Invoke() });
    Show(new Popup { getMessage = () => message, buttons = buttons, textFieldText = inputText, isCancellable = true });
  }

  private string GetTextFieldText()
  {
    if (textFieldInstance != null)
    {
      var field = GameObject.FindObjectOfType<TMPro.TMP_InputField>();
      return field.text;
    }
    return null;
  }

  public void Show(Popup popup)
  {
    if (popup.forceImmediateTakeover)
    {
      // The nuclear option..
      if (currentPopup != null)
      {
        Close();
      }
      popupQueue.Clear();
    }
    popupQueue.Enqueue(popup);
    PumpQueue();
  }

  public void ShowTwoButtons(string message,
    string button1, System.Action action1,
    string button2, System.Action action2,
    float textWrapWidth = 0f)
  {
    var buttons = new List<PopupButton.Params>();
    buttons.Add(new PopupButton.Params { getLabel = () => button1, onClick = action1 });
    buttons.Add(new PopupButton.Params { getLabel = () => button2, onClick = action2 });
    Show(new Popup { getMessage = () => message, buttons = buttons, textWrapWidth = textWrapWidth, isCancellable = false });
  }

  public void ShowThreeButtons(string message,
  string button1, System.Action action1,
  string button2, System.Action action2,
  string button3, System.Action action3,
  float textWrapWidth = 0f)
  {
    var buttons = new List<PopupButton.Params>();
    buttons.Add(new PopupButton.Params { getLabel = () => button1, onClick = action1 });
    buttons.Add(new PopupButton.Params { getLabel = () => button2, onClick = action2 });
    buttons.Add(new PopupButton.Params { getLabel = () => button3, onClick = action3 });
    Show(new Popup { getMessage = () => message, buttons = buttons, textWrapWidth = textWrapWidth, isCancellable = false });
  }


  // Convenience
  public void AskHowToPlay(System.Action<GameBuilderApplication.PlayOptions> onResponse)
  {
#if USE_PUN
    Show(new DynamicPopup.Popup
    {
      fullWidthButtons = true,
      getMessage = () => "<size=80%>Play as single player or multiplayer?",
      isCancellable = true,
      buttons = new List<PopupButton.Params>() {
        new PopupButton.Params
        {
          getLabel = () => "Single Player",
          onClick = () => onResponse(new GameBuilderApplication.PlayOptions{isMultiplayer = false})
        },
        new PopupButton.Params
        {
          getLabel = () => "Private Multiplayer",
          onClick = () => onResponse(new GameBuilderApplication.PlayOptions{isMultiplayer = true, startAsPublic = false})
        },
        new PopupButton.Params
        {
          getLabel = () => "Public Multiplayer",
          onClick = () => {
            MultiplayerWarning warning = null;
            Util.FindIfNotSet(this, ref warning);
            warning.Open(() => {
              onResponse(new GameBuilderApplication.PlayOptions{isMultiplayer = true, startAsPublic = true});
            });
          }
        },
        new PopupButton.Params
        {
          getLabel = () => "Cancel",
          onClick = () => {}
        },
      }
    });
#else
    StartCoroutine(CallNextFrame(() => onResponse(new GameBuilderApplication.PlayOptions { isMultiplayer = false })));
#endif
  }

  IEnumerator CallNextFrame(System.Action func)
  {
    yield return null;
    func();
  }

  // Convenience
  public void AskHowToPlayMultiplayer(System.Action<GameBuilderApplication.PlayOptions> onResponse)
  {
    Show(new DynamicPopup.Popup
    {
      fullWidthButtons = true,
      getMessage = () => "Start a new multiplayer game?",
      isCancellable = true,
      buttons = new List<PopupButton.Params>() {
        new PopupButton.Params
        {
          getLabel = () => "Private Multiplayer",
          onClick = () => onResponse(new GameBuilderApplication.PlayOptions{isMultiplayer = true, startAsPublic = false})
        },
        new PopupButton.Params
        {
          getLabel = () => "Public Multiplayer",
          onClick = () =>  onResponse(new GameBuilderApplication.PlayOptions{isMultiplayer = true, startAsPublic = true})
        },
      new PopupButton.Params
      {
        getLabel = () => "Cancel",
        onClick = () => { }
      },
    }
    });
  }


}
