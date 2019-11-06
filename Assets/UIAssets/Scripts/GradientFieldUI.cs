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

public abstract class GradientFieldUI<T> : MonoBehaviour
{
  private class StopGroup
  {
    public GradientStopUI stop;
    public T value;

    public StopGroup(GradientStopUI stop, T value)
    {
      this.stop = stop;
      this.value = value;
    }
  }

  [SerializeField] protected RectTransform gradientTransform;
  [SerializeField] protected RectTransform mouseoverArea;
  [SerializeField] EventTrigger mouseoverTrigger;
  [SerializeField] Transform gradientStopsContainer;
  [SerializeField] GradientStopUI gradientStopTemplate;
  [SerializeField] RectTransform stopHint;
  private List<StopGroup> stops = new List<StopGroup>();
  private string selectedStopId = null;
  private EditForm editForm = null;
  private bool changingStops = false;
  private IModel model;

  public event System.Action<float, T> addStopRequested;
  public event System.Action<string, float> changeStopPositionRequested;
  public event System.Action<string, T> changeStopValueRequested;
  public event System.Action<string> removeStopRequested;

  public void Setup()
  {
    editForm = GetEditForm();
    editForm.requestClose += () =>
    {
      SelectStop(null);
    };
    EventTrigger.Entry clickEntry = new EventTrigger.Entry();
    clickEntry.eventID = EventTriggerType.PointerClick;
    clickEntry.callback.AddListener((data) => { OnPointerClick((PointerEventData)data); });
    mouseoverTrigger.triggers.Add(clickEntry);
  }

  public void SetData(IModel model)
  {
    this.model = model;
    BeginChangingStops();
    ClearStops();
    for (int i = 0; i < model.GetCount(); i++)
    {
      AddStop(model.GetId(i), model.GetPosition(i), model.GetValue(i));
    }
    EndChangingStops();
  }

  protected IModel GetModel()
  {
    return model;
  }

  private void BeginChangingStops()
  {
    changingStops = true;
  }

  private void EndChangingStops()
  {
    changingStops = false;
    stops.Sort((StopGroup v1, StopGroup v2) =>
    {
      return v1.stop.GetPosition().CompareTo(v2.stop.GetPosition());
    });
    UpdateEditForm();
    UpdateStops();
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    if (!CanAddStop()) return;
    float stopPosition = GetStopPositionFromMouse();
    addStopRequested?.Invoke(stopPosition, FindAdjacentValue(stopPosition));
  }

  public void Update()
  {
    float stopPosition = GetStopPositionFromMouse();
    float offsetX = stopPosition * gradientTransform.rect.width;
    Vector2 rectPosition = stopHint.anchoredPosition;
    rectPosition.x = offsetX;
    stopHint.anchoredPosition = rectPosition;
    Vector2 mousePosition = RectTransformUtility.WorldToScreenPoint(null, Input.mousePosition);
    stopHint.gameObject.SetActive(
      CanAddStop() && RectTransformUtility.RectangleContainsScreenPoint(mouseoverArea, mousePosition));
  }

  private float GetStopPositionFromMouse()
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
      gradientTransform, Input.mousePosition, null, out localPos);
    float offsetX = localPos.x + gradientTransform.rect.width / 2.0f;
    return Mathf.Max(Mathf.Min(offsetX / gradientTransform.rect.width, 1), 0);
  }

  // TODO: Model should be calculating this instead
  private T FindAdjacentValue(float position)
  {
    int i = 0;
    while (i < model.GetCount() - 1 && model.GetPosition(i) < position)
    {
      i++;
    }
    return model.GetValue(i);
  }

  private void AddStop(string id, float position, T value)
  {
    GradientStopUI stop = Instantiate(gradientStopTemplate, gradientStopsContainer);
    stop.Setup();
    stop.SetId(id);
    stop.SetPosition(position);
    stop.onClick += () =>
    {
      SelectStop(stop.GetId());
    };
    stop.onDragged += (np) =>
    {
      changeStopPositionRequested?.Invoke(stop.GetId(), np);
    };
    stop.onRemoveGesture += () =>
    {
      removeStopRequested?.Invoke(stop.GetId());
    };
    stop.gameObject.SetActive(true);
    stops.Add(new StopGroup(stop, value));
    if (!changingStops)
    {
      EndChangingStops();
    }
  }

  private void ClearStops()
  {
    foreach (StopGroup group in stops)
    {
      group.stop.Destroy();
    }
    stops.Clear();
    if (!changingStops)
    {
      EndChangingStops();
    }
  }

  private void SelectStop(string id)
  {
    if (selectedStopId != null)
    {
      StopGroup group = GetStopGroupById(selectedStopId);
      group.stop.SetSelected(false);
    }
    selectedStopId = id;
    UpdateEditForm();
  }

  private void UpdateEditForm()
  {
    if (selectedStopId == null)
    {
      editForm.gameObject.SetActive(false);
      return;
    }

    StopGroup group = GetStopGroupById(selectedStopId);
    if (group == null)
    {
      selectedStopId = null;
      editForm.gameObject.SetActive(false);
      return;
    }

    group.stop.SetSelected(true);
    editForm.Populate(group.value, (T value) =>
    {
      changeStopValueRequested?.Invoke(selectedStopId, value);
    });
    editForm.gameObject.SetActive(true);
  }

  private StopGroup GetStopGroupById(string id)
  {
    return stops.Find((StopGroup group) =>
    {
      return group.stop.GetId() == id;
    });
  }

  protected virtual void UpdateStops() { }

  protected virtual bool CanAddStop()
  {
    return true;
  }

  protected int GetNumStops()
  {
    return stops.Count;
  }

  protected abstract EditForm GetEditForm();

  public abstract class EditForm : MonoBehaviour
  {

    [SerializeField] RectTransform rectTransform;
    public abstract void Populate(T value, System.Action<T> callback);
    public event System.Action requestClose;

    void Update()
    {
      if (Input.GetMouseButtonDown(0) && !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
      {
        RequestClose();
      }
    }

    protected void RequestClose()
    {
      requestClose?.Invoke();
    }

  }

  public interface IModel
  {
    int GetCount();
    string GetId(int index);
    float GetPosition(int index);
    T GetValue(int index);
  }

}
