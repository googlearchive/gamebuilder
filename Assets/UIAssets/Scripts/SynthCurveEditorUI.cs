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
public class SynthCurveEditorUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
  private const int SAMPLE_WIDGET_HEIGHT = 6;
  // Number of samples in the array.
  public int numSamples = 32;
  // Range of values for samples in the array (range is 0 .. numLevels - 1).
  public int numLevels = 64;
  // Prefab for the little dot that represents one sample in this widget.
  // This prefab must be a child of this widget.
  public RectTransform sampleWidgetPrefab;
  // (Calculated) width of a sample and height of a single value notch.
  private float sampleWidth, sampleValueHeight;
  // The panel (our own RectTransform).
  private RectTransform panel;
  // The widgets that represent each sample we are editing. These are all our
  // children.
  private RectTransform[] sampleWidgets;
  // Image component of each sample widget.
  private Image[] sampleWidgetImages;
  // Current values of the samples.
  // This array always has numSamples elements, each of which in [0..numLevels - 1]
  private int[] sampleValues;
  // If true, the mouse pointer is inside this widget.
  private bool pointerIsInside;
  // If true, we are currently in the middle of a drag.
  // While dragging, we continue to process mouse events even if the mouse moves outside
  // of our bounds.
  private bool dragging;
  // Last mouse pos
  private Vector2 lastMousePos;
  // If Shift is held while dragging, this is the locked sample value to use.
  private int lockedSampleValue = -1;

  public event System.Action onFinishDrag;

  public void Setup()
  {
    panel = GetComponent<RectTransform>();
    sampleWidgetPrefab.gameObject.SetActive(false);
    sampleWidgets = new RectTransform[numSamples];
    sampleWidgetImages = new Image[numSamples];
    sampleValues = new int[numSamples];

    for (int i = 0; i < numSamples; i++)
    {
      sampleValues[i] = numLevels / 2;
      sampleWidgets[i] = GameObject.
        Instantiate(sampleWidgetPrefab.gameObject).GetComponent<RectTransform>();
      sampleWidgets[i].transform.SetParent(panel, false);
      sampleWidgets[i].gameObject.SetActive(true);
      sampleWidgetImages[i] = sampleWidgets[i].GetComponent<Image>();
      Debug.Assert(sampleWidgetImages[i] != null, "Sample widget does not have Image component?");
    }
  }

  public void OnPointerEnter(PointerEventData data)
  {
    pointerIsInside = true;
  }

  public void OnPointerExit(PointerEventData data)
  {
    pointerIsInside = false;
  }

  void Update()
  {
    float newSampleWidth = panel.sizeDelta.x / numSamples;
    float newSampleValueHeight = panel.sizeDelta.y / numLevels;
    if (newSampleWidth != sampleWidth || newSampleValueHeight != sampleValueHeight)
    {
      sampleWidth = newSampleWidth;
      sampleValueHeight = newSampleValueHeight;
      for (int i = 0; i < numSamples; i++)
      {
        sampleWidgets[i].anchoredPosition = new Vector2(i * sampleWidth - SAMPLE_WIDGET_HEIGHT / 2, 0);
        sampleWidgets[i].sizeDelta = new Vector2(sampleWidth, SAMPLE_WIDGET_HEIGHT);
      }
      UpdateView();
    }

    if (dragging)
    {
      HandleMouseDrag(lastMousePos, Input.mousePosition);
      lastMousePos = Input.mousePosition;
      if (!Input.GetMouseButton(0))
      {
        // Stopped dragging.
        dragging = false;
        onFinishDrag?.Invoke();
      }
    }
    else
    {
      if (pointerIsInside && Input.GetMouseButton(0))
      {
        // Start dragging
        dragging = true;
        lockedSampleValue = -1;
        lastMousePos = Input.mousePosition;
        HandleMouseDrag(Input.mousePosition, Input.mousePosition);
      }
    }
  }

  void HandleMouseDrag(Vector2 fromScreenPos, Vector2 toScreenPos)
  {
    Vector2 startPoint, endPoint;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, fromScreenPos, null, out startPoint);
    RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, toScreenPos, null, out endPoint);
    startPoint += new Vector2(panel.rect.width / 2, panel.rect.height / 2);
    endPoint += new Vector2(panel.rect.width / 2, panel.rect.height / 2);

    int startSampleIndex = (int)(startPoint.x / sampleWidth);
    int endSampleIndex = (int)(endPoint.x / sampleWidth);

    if (endSampleIndex < startSampleIndex)
    {
      int temp = endSampleIndex;
      endSampleIndex = startSampleIndex;
      startSampleIndex = temp;
    }

    int newValue = (int)(endPoint.y / sampleValueHeight);

    if (lockedSampleValue < 0 && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
    {
      lockedSampleValue = newValue;
    }
    else if (lockedSampleValue >= 0 && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
    {
      newValue = lockedSampleValue;
    }

    for (int i = startSampleIndex; i <= endSampleIndex; i++)
    {
      if (i >= 0 && i < sampleValues.Length)
      {
        sampleValues[i] = Mathf.Clamp(newValue, 0, numLevels - 1);
      }
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
      sampleWidgetImages[i].color = CalculateSampleColor(sampleValues[i]);
    }
  }

  private Color CalculateSampleColor(float sampleValue)
  {
    float hue = Mathf.Clamp(0.5f - (sampleValue / numLevels) * 0.5f, 0, 0.5f);
    return Color.HSVToRGB(hue, 1, 1);
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
