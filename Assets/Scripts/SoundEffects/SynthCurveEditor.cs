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
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Widget that lets the user edit a sound curve (pitch or volume, for instance).
// This class doesn't really know that it's editing sound, it just knows
// that it's editing an array of numSamples samples, each of which in the range
// [0..numLevels - 1]. Might as well be called an "array editor" but that would
// sound so much less exciting.
[RequireComponent(typeof(RectTransform))]
public class SynthCurveEditor : MonoBehaviour
{
  // Number of samples in the array.
  public int numSamples = 32;
  // Range of values for samples in the array (range is 0 .. numLevels - 1).
  public int numLevels = 64;
  // Prefab for the little dot that represents one sample in this widget.
  // This prefab must be a child of this widget.
  public RectTransform sampleWidgetPrefab;
  // (Calculated) width of a sample and height of a single value notch.
  private int sampleWidth, sampleValueHeight;
  // The panel (our own RectTransform).
  private RectTransform panel;
  // The widgets that represent each sample we are editing. These are all our
  // children.
  private RectTransform[] sampleWidgets;
  // Current values of the samples.
  // This array always has numSamples elements, each of which in [0..numLevels - 1]
  private int[] sampleValues;
  // If true, the mouse pointer is inside this widget.
  private bool pointerIsInside;
  // If true, we are currently in the middle of a drag.
  // While dragging, we continue to process mouse events even if the mouse moves outside
  // of our bounds.
  private bool dragging;

  // Explicit init. Must be called before anything else.
  public void Setup()
  {
    panel = GetComponent<RectTransform>();
    sampleWidgetPrefab.gameObject.SetActive(false);
    sampleWidgets = new RectTransform[numSamples];
    sampleValues = new int[numSamples];
    sampleWidth = (int)(panel.sizeDelta.x / numSamples);
    sampleValueHeight = (int)(panel.sizeDelta.y / numLevels);

    for (int i = 0; i < numSamples; i++)
    {
      sampleValues[i] = numLevels / 2;
      sampleWidgets[i] = GameObject.
        Instantiate(sampleWidgetPrefab.gameObject).GetComponent<RectTransform>();
      sampleWidgets[i].transform.SetParent(panel, false);
      sampleWidgets[i].anchoredPosition = new Vector2(i * sampleWidth, 0);
      sampleWidgets[i].sizeDelta = new Vector2(sampleWidth, 20);
      sampleWidgets[i].gameObject.SetActive(true);
    }
    UpdateView();
  }

  public void OnPointerEntered()
  {
    pointerIsInside = true;
  }

  public void OnPointerExit()
  {
    pointerIsInside = false;
  }

  void Update()
  {
    if (dragging || (pointerIsInside && Input.GetMouseButton(0)))
    {
      dragging = true;
      HandleMouse(Input.mousePosition);
    }
    if (!Input.GetMouseButton(0))
    {
      dragging = false;
    }
  }

  // Handles the fact that the mouse interacted with that screen position (either a click, or
  // just dragging over).
  void HandleMouse(Vector2 screenPos)
  {
    Vector2 localPoint;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screenPos, null, out localPoint);
    int sampleIndex = (int)(localPoint.x / sampleWidth);
    int newValue = (int)(localPoint.y / sampleValueHeight);
    if (sampleIndex >= 0 && sampleIndex < sampleValues.Length)
    {
      sampleValues[sampleIndex] = Mathf.Clamp(newValue, 0, numLevels - 1);
    }
    UpdateView();
  }

  // Updates view to match model.
  void UpdateView()
  {
    for (int i = 0; i < numSamples; i++)
    {
      sampleWidgets[i].anchoredPosition = new Vector2(sampleWidgets[i].anchoredPosition.x,
        sampleValues[i] * sampleValueHeight);
    }
  }

  // Sets the sample values. This will automatically update the view.
  public void SetSampleValues(int[] values)
  {
    for (int i = 0; i < sampleValues.Length; i++)
    {
      sampleValues[i] = i < values.Length ? values[i] : 0;
    }
    UpdateView();
  }

  // Returns the current sample values.
  public int[] GetSampleValues()
  {
    int[] clone = new int[sampleValues.Length];
    for (int i = 0; i < sampleValues.Length; i++)
    {
      clone[i] = sampleValues[i];
    }
    return clone;
  }
}
