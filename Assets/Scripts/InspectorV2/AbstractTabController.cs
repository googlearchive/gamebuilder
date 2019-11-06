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
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class AbstractTabController : MonoBehaviour
{
  public abstract void Setup();

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public virtual void RequestDestroy()
  {
    Destroy(gameObject);
  }

  public virtual bool KeyLock()
  {
    GameObject selected = EventSystem.current?.currentSelectedGameObject;
    // The less hacky way would be for each tab to implement this function individually. But YAGNI.
    // Equals comparison is needed for when object is destroyed.
    return selected != null && !selected.Equals(null) && selected.GetComponent<TMPro.TMP_InputField>() != null;
  }

  public virtual void Open(VoosActor actor, Dictionary<string, object> props = null)
  {
    gameObject.SetActive(true);
  }

  public virtual bool OnMenuRequest()
  {
    return false;
  }

  public virtual void Close()
  {
    gameObject.SetActive(false);
  }

  public virtual Dictionary<string, object> GetState()
  {
    return null;
  }

  protected virtual void Update()
  {
    if (Input.GetKeyDown(KeyCode.Tab))
    {
      if (Input.GetKey(KeyCode.LeftShift))
      {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
          Selectable selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp();
          if (selectable != null)
            selectable.Select();
        }
      }
      else
      {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
          Selectable selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
          if (selectable != null)
            selectable.Select();
        }
      }
    }
  }

}
