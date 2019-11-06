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

public class ColorWheelOld : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
  [SerializeField] RectTransform rectTransform;
  [SerializeField] RectTransform targetRect;
  [SerializeField] SliderWrapper valueSliderWrapper;
  [SerializeField] SliderWrapper alphaSliderWrapper;
  [SerializeField] UnityEngine.UI.Image hoverImage;
  [SerializeField] UnityEngine.UI.Image valueFeedbackImage;
  [SerializeField] UnityEngine.UI.Image alphaFeedbackImage;
  [SerializeField] TMPro.TMP_InputField hexInput;
  [SerializeField] UnityEngine.UI.Button colorReset;
  [SerializeField] Color defaultColor;

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

  void Awake()
  {
    redVector = new Vector2(0, 1);

    float radianOffset = Mathf.PI / 6f;
    greenVector = new Vector2(Mathf.Cos(radianOffset), -1 * Mathf.Sin(radianOffset));
    blueVector = new Vector2(-1 * Mathf.Cos(radianOffset), -1 * Mathf.Sin(radianOffset));

    valueSliderWrapper.onValueChanged += OnValueSliderChanged;
    if (alphaSliderWrapper != null) alphaSliderWrapper.onValueChanged += OnAlphaSliderChanged;

    hexInput.onEndEdit.AddListener(OnEndHexEdit);
    colorReset.onClick.AddListener(() =>
    {
      if (finalColor != defaultColor)
      {
        SetColor(defaultColor);
        OnColorChange?.Invoke(defaultColor);
      }
    });
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

  public void ForceRelease()
  {
    selectingColor = false;
    hoverImage.enabled = false;
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    selectingColor = true;
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    hoverImage.enabled = true;
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    hoverImage.enabled = false;
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

  void CalculateColor(Vector2 localVec)
  {
    localVec /= (rectTransform.rect.width * .5f);

    float hue = 1f - (Vector2.SignedAngle(-redVector, localVec) + 180f) / 360f;
    float sat = Vector2.Distance(localVec, Vector2.zero);
    hueSatColor = Color.HSVToRGB(hue, sat, 1);
  }

  public virtual void SetColor(Color newColor)
  {
    if (selectingColor) return;

    float newHue, newSat, newVal;
    Color.RGBToHSV(newColor, out newHue, out newSat, out newVal);

    hueSatColor = Color.HSVToRGB(newHue, newSat, 1);
    valueFeedbackImage.color = hueSatColor;
    if (alphaFeedbackImage != null) alphaFeedbackImage.color = new Color(newColor.r, newColor.g, newColor.b);

    finalColor = newColor;
    finalColorNew = newColor;

    colorValue = newVal;
    colorAlpha = newColor.a;
    //find position on colorwheel
    float angle = Mathf.PI * 2f * newHue;
    targetRect.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * newSat * (rectTransform.rect.width * .5f);
  }

  void OnValueSliderChanged(float newValue)
  {
    colorValue = newValue;
    UpdateColor();
    SetColor(finalColorNew);
    OnColorChange?.Invoke(finalColor);
  }

  void OnAlphaSliderChanged(float newAlpha)
  {
    colorAlpha = newAlpha;
    UpdateColor();
    SetColor(finalColorNew);
    OnColorChange?.Invoke(finalColor);
  }

  void UpdateColor()
  {
    valueFeedbackImage.color = hueSatColor;
    finalColorNew = new Color(colorValue, colorValue, colorValue, 1) * hueSatColor;
    if (alphaFeedbackImage != null) alphaFeedbackImage.color = finalColorNew;
    finalColorNew.a = colorAlpha;
  }

  void Update()
  {
    valueSliderWrapper.SetValue(colorValue);
    if (alphaSliderWrapper != null) alphaSliderWrapper.SetValue(colorAlpha);

    if (selectingColor)
    {
      Vector2 mousepos = Input.mousePosition;
      Vector2 rectpoint;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousepos, null, out rectpoint);
      targetRect.anchoredPosition = rectpoint;
      CalculateColor(rectpoint);
      UpdateColor();
    }

    if (!hexInput.isFocused)
    {
      hexInput.text = $"#{ColorUtility.ToHtmlStringRGB(finalColorNew)}";
    }
  }
}
