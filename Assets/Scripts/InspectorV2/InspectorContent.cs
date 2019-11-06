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

public class InspectorContent : MonoBehaviour
{
  public TabRef[] tabRefs;
  private VoosActor currActor;
  private TabRef currTabRef;
  private bool isShowing = false;

  int currTabIndex = 0;

  public void InitializeInspector(InspectorController inspector)
  {
    foreach (TabRef tabRef in tabRefs)
    {
      tabRef.tabController.Setup();
    }
    currTabRef = tabRefs[currTabIndex];
  }

  public void Open(VoosActor actor)
  {
    currActor = actor;
    if (currActor == null)
    {
      throw new System.Exception("Actor not set");
    }
    isShowing = true;
    currTabRef.tabController.Open(currActor);
  }

  public void Close()
  {
    if (!isShowing) return;
    isShowing = false;
    currTabRef?.tabController.Close();
  }

  public string GetCurrentTabName()
  {
    return currTabRef?.name;
  }

  public void Destroy()
  {
    foreach (TabRef tabRef in tabRefs)
    {
      tabRef.tabController.RequestDestroy();
    }
  }

  public void SwitchTab(int index)
  {
    currTabIndex = Mathf.Min(index, tabRefs.Length - 1);
    SwitchTab(tabRefs[currTabIndex]);
  }

  public int GetCurrentTabIndex()
  {
    return currTabIndex;
  }

  public void SwitchTab(TabRef tabRef)
  {
    if (currTabRef == tabRef) return;
    currTabRef?.tabController.Close();
    currTabRef = tabRef;
    if (isShowing)
    {
      currTabRef.tabController.Open(currActor);
    }
  }

  public bool KeyLock()
  {
    return currTabRef != null && currTabRef.tabController.KeyLock();
  }

  public bool OnMenuRequest()
  {
    return currTabRef != null && currTabRef.tabController.OnMenuRequest();
  }

  [System.Serializable]
  public class TabRef
  {
    public string name;
    public AbstractTabController tabController;
  }
}