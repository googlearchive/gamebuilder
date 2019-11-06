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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectorController : MonoBehaviour
{
  [SerializeField] RectTransform window;
  [SerializeField] ActorContentChecker contentChecker;
  [SerializeField] InspectorContent content;

  [SerializeField] GameObject[] tabObjects;
  [SerializeField] CanvasGroup canvasGroup;
  [SerializeField] MouseoverTooltip tooltip;
  [SerializeField] EditWindowFrameUI editWindowFrameUI;

  public System.Action<VoosActor> onOpenActor;

  private VoosEngine voosEngine;
  private VoosActor currActor;

  private EditMain editMain;
  private InputControl inputControl;

  string applyChangesButtonString;

  const float SHOW_ANIMATION_DURATION = .3f;
  const float HIDE_ANIMATION_DURATION = 1f;

  private float lastOwnershipRequest = Mathf.NegativeInfinity;
  const float OWNERSHIP_CHECK_DELAY_S = 0.5f;
  private bool isShowing = false;

  const int TOTAL_TABS = 3;
  enum TabType
  {
    Info = 0,
    Visual = 1,
    Physics = 2
  }

  public bool GetIsShowing()
  {
    return isShowing;
  }

  public void Setup()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref inputControl);

    // Setup tabs.
    contentChecker.Setup();
    content.InitializeInspector(this);

    //disable the canvas group initially (since it should be hidden)
    DisableCanvasGroup();
    window.localScale = Vector3.one * .01f;

    SetupSubmenu();
    SetupTooltips();
  }

  private void SetupSubmenu()
  {
    editWindowFrameUI.infoToggle.onValueChanged.AddListener((on) => { if (on) SwitchTab((int)TabType.Info); });
    editWindowFrameUI.visualsToggle.onValueChanged.AddListener((on) => { if (on) SwitchTab((int)TabType.Visual); });
    editWindowFrameUI.physicsToggle.onValueChanged.AddListener((on) => { if (on) SwitchTab((int)TabType.Physics); });

    editWindowFrameUI.upButton.onClick.AddListener(PreviousTab);
    editWindowFrameUI.downButton.onClick.AddListener(NextTab);
  }

  void DisableCanvasGroup()
  {
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;
    canvasGroup.alpha = 0;
  }

  void EnableCanvasGroup()
  {
    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;
    canvasGroup.alpha = 1;
  }

  public void SetActor(VoosActor actor)
  {
    this.currActor = actor;
    editWindowFrameUI.headerText.text = GetHeaderDescription();
    if (isShowing && currActor != null)
    {
      contentChecker.Open(actor, OnOpenContent, OnCloseContent);
    }
    else
    {
      contentChecker.Close();
    }
  }

  private string GetHeaderDescription()
  {
    if (currActor == null) return "";
    string displayName = currActor.GetDisplayName();
    if (displayName.Length > 10)
    {
      displayName = displayName.Substring(0, 10) + "...";
    }
    return displayName + ": " + content.GetCurrentTabName();
  }

  public void SetTooltip(MouseoverTooltip tooltip)
  {
    this.tooltip = tooltip;
  }

  public VoosActor GetActor()
  {
    return currActor;
  }

  public void Hide()
  {
    isShowing = false;
    contentChecker.Close();

    DisableCanvasGroup();
    gameObject.SetActive(false);

    // StopAllCoroutines();
    // StartCoroutine(HideRoutine());
  }

  public void Show()
  {
    isShowing = true;
    gameObject.SetActive(true);
    contentChecker.Open(currActor, OnOpenContent, OnCloseContent);

    // EnableCanvasGroup();
    // gameObject.SetActive(true);
    StopAllCoroutines();
    StartCoroutine(ShowRoutine());
  }

  IEnumerator ShowRoutine()
  {
    float timer = 0;
    while (timer < SHOW_ANIMATION_DURATION)
    {
      timer += Time.unscaledDeltaTime;
      float percent = Mathf.Clamp01(timer / SHOW_ANIMATION_DURATION);
      canvasGroup.alpha = percent;
      window.localScale = Vector3.one * percent;
      yield return null;
    }
    EnableCanvasGroup();
  }


  //TODO: THIS DOES NOT WORK
  IEnumerator HideRoutine()
  {
    float timer = 0;
    while (timer < HIDE_ANIMATION_DURATION)
    {
      timer += Time.unscaledDeltaTime;
      float percent = Mathf.Clamp01(timer / HIDE_ANIMATION_DURATION);
      canvasGroup.alpha = 1 - percent;
      yield return null;
    }

    StopAllCoroutines();
    DisableCanvasGroup();
    gameObject.SetActive(false);
  }

  public bool KeyLock()
  {
    return content.KeyLock();
  }

  public void SwitchTab(int index)
  {
    content.SwitchTab(index);
    editWindowFrameUI.headerText.text = GetHeaderDescription();
  }

  // public void SwitchTab(string tabName, Dictionary<string, object> props = null)
  // {
  //   content.SwitchTab(tabName);
  //   editWindowFrameUI.headerText.text = GetHeaderDescription();
  // }

  void OnOpenContent(VoosActor showActor)
  {
    // Kind of not ideal ...
    this.currActor = showActor;
    if (currActor != null)
    {
      editWindowFrameUI.headerText.text = GetHeaderDescription();
      content.Open(currActor);
      onOpenActor?.Invoke(currActor);
    }
    else
    {
      contentChecker.Close();
    }
  }

  void OnCloseContent(bool noActor)
  {
    content.Close();
    if (noActor)
    {
      this.currActor = null;
    }
  }

  public bool OnMenuRequest()
  {
    if (!content.OnMenuRequest())
    {
      return false;
    }
    return true;
  }

  void Update()
  {
    if (!editMain.UserMainKeyLock())
    {
      if (inputControl.GetButtonDown("PrevToolOption"))
      {
        PreviousTab();
      }

      if (inputControl.GetButtonDown("NextToolOption"))
      {
        NextTab();
      }
    }

    int index = content.GetCurrentTabIndex();
    editWindowFrameUI.infoToggle.isOn = index == (int)TabType.Info;
    editWindowFrameUI.visualsToggle.isOn = index == (int)TabType.Visual;
    editWindowFrameUI.physicsToggle.isOn = index == (int)TabType.Physics;

    // editWindowFrameUI.submenuObject.SetActive(currActor != null);
    editWindowFrameUI.submenuObject.SetActive(AnyTabOpen());
  }

  bool AnyTabOpen()
  {
    foreach (GameObject tab in tabObjects)
    {
      if (tab.activeSelf) return true;
    }

    return false;
  }

  void PreviousTab()
  {
    int newindex = content.GetCurrentTabIndex() - 1;
    if (newindex < 0) newindex = (TOTAL_TABS - 1);
    SwitchTab(newindex);
  }

  void NextTab()
  {
    int newindex = (content.GetCurrentTabIndex() + 1) % TOTAL_TABS;
    SwitchTab(newindex);
  }

  private void SetupTooltips()
  {

    editWindowFrameUI.infoToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Info");
    editWindowFrameUI.transformToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Transform");
    editWindowFrameUI.visualsToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Visuals");
    editWindowFrameUI.physicsToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Physics");

    editWindowFrameUI.upButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription($"Previous ({inputControl.GetKeysForAction("PrevToolOption")})");
    editWindowFrameUI.downButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription($"Next ({inputControl.GetKeysForAction("NextToolOption")})");
  }
}
