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

using UnityEngine.EventSystems;
public class ColorFieldUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
  [SerializeField] RectTransform wheelRect;
  [SerializeField] RectTransform crosshairsRect;

  [SerializeField] SliderWrapperUI valueSliderWrapper;
  [SerializeField] SliderWrapperUI alphaSliderWrapper;
  [SerializeField] GameObject alphaSliderContainer;

  [SerializeField] UnityEngine.UI.Image valueSliderImage;
  [SerializeField] UnityEngine.UI.Image alphaSliderImage;

  [SerializeField] TMPro.TMP_InputField hexInput;
  [SerializeField] UnityEngine.UI.Button resetButton;

  Color defaultColor = Color.white;

  Vector2 redVector;
  Vector2 blueVector;
  Vector2 greenVector;

  public Color finalColor = Color.white;
  public Color finalColorNew = Color.white;
  public Color hueSatColor = Color.white;
  float colorValue = .5f;
  float colorAlpha = 1f;

  public System.Action<Color> OnColorChange;

  bool selectingColor = false;

  public bool IsBeingEdited()
  {
    return selectingColor || hexInput.isFocused;
  }

  public void ForceRelease()
  {
    selectingColor = false;
  }

  public void DisableAlpha()
  {
    alphaSliderContainer.SetActive(false);
  }

  public void Setup()
  {
    SetupColorVectors();

    valueSliderWrapper.onValueChanged += OnValueSliderChanged;
    alphaSliderWrapper.onValueChanged += OnAlphaSliderChanged;

    hexInput.onEndEdit.AddListener(OnEndHexEdit);
    resetButton.onClick.AddListener(() =>
    {
      if (finalColor != defaultColor)
      {
        SetColor(defaultColor);
        OnColorChange?.Invoke(defaultColor);
      }
    });
  }

  void Update()
  {
    valueSliderWrapper.SetValue(colorValue);
    if (alphaSliderWrapper != null) alphaSliderWrapper.SetValue(colorAlpha);

    if (selectingColor)
    {
      Vector2 mousepos = Input.mousePosition;
      Vector2 rectpoint;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(wheelRect, mousepos, null, out rectpoint);
      CalculateColor(rectpoint);

      float newHue, newSat, newVal;
      Color.RGBToHSV(hueSatColor, out newHue, out newSat, out newVal);
      float angle = Mathf.PI * 2f * newHue;
      crosshairsRect.anchoredPosition =
        new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * newSat * (wheelRect.rect.width * .5f);

      UpdateColor();
    }

    if (!hexInput.isFocused)
    {
      hexInput.text = $"#{ColorUtility.ToHtmlStringRGB(finalColorNew)}";
    }
  }

  private void SetupColorVectors()
  {
    float radianOffset = Mathf.PI / 6f;
    redVector = new Vector2(0, 1);
    greenVector = new Vector2(Mathf.Cos(radianOffset), -1 * Mathf.Sin(radianOffset));
    blueVector = new Vector2(-1 * Mathf.Cos(radianOffset), -1 * Mathf.Sin(radianOffset));
  }

  void OnValueSliderChanged(float newValue)
  {
    colorValue = newValue;
    UpdateAndSetColor();
  }

  void OnAlphaSliderChanged(float newAlpha)
  {
    colorAlpha = newAlpha;
    UpdateAndSetColor();
  }

  void UpdateAndSetColor()
  {
    UpdateColor();
    SetColor(finalColorNew);
    OnColorChange?.Invoke(finalColor);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    selectingColor = true;
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    selectingColor = false;
    if (finalColorNew != finalColor)
    {
      finalColor = finalColorNew;
      OnColorChange?.Invoke(finalColor);
    }
  }

  void UpdateColor()
  {
    valueSliderImage.color = hueSatColor;
    finalColorNew = new Color(colorValue, colorValue, colorValue, 1) * hueSatColor;
    alphaSliderImage.color = finalColorNew;
    finalColorNew.a = colorAlpha;
  }

  public virtual void SetColor(Color newColor)
  {
    if (selectingColor) return;

    float newHue, newSat, newVal;
    Color.RGBToHSV(newColor, out newHue, out newSat, out newVal);

    hueSatColor = Color.HSVToRGB(newHue, newSat, 1);
    valueSliderImage.color = hueSatColor;
    alphaSliderImage.color = new Color(newColor.r, newColor.g, newColor.b);

    finalColor = newColor;
    finalColorNew = newColor;

    colorValue = newVal;
    colorAlpha = newColor.a;
    //find position on colorwheel
    float angle = Mathf.PI * 2f * newHue;
    crosshairsRect.anchoredPosition =
      new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * newSat * (wheelRect.rect.width * .5f);
  }

  void OnEndHexEdit(string newstring)
  {
    if (newstring != "" && Input.GetButtonDown("Submit"))
    {
      Color newcolor;
      if (ColorUtility.TryParseHtmlString(newstring, out newcolor) && newcolor != finalColor)
      {
        SetColor(newcolor);
        OnColorChange?.Invoke(newcolor);
      }
    }
  }

  void CalculateColor(Vector2 localVec)
  {
    localVec /= (wheelRect.rect.width * .5f);

    float hue = Mathf.Clamp(1f - (Vector2.SignedAngle(-redVector, localVec) + 180f) / 360f, 0, 1);
    float sat = Mathf.Clamp(Vector2.Distance(localVec, Vector2.zero), 0, 1);
    hueSatColor = Color.HSVToRGB(hue, sat, 1);
  }

  public void OnDrag(PointerEventData eventData)
  {
    // throw new NotImplementedException();
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    // throw new NotImplementedException();
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    // throw new NotImplementedException();
  }
}
