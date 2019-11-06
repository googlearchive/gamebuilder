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

using UnityEngine;
using UnityEngine.UI;

public class IconPickerIconRow : MonoBehaviour
{
  [SerializeField] Image[] cells;

  private int count;

  void Awake()
  {
    foreach (Image img in cells)
    {
      img.gameObject.SetActive(false);
    }
  }

  public bool IsFull()
  {
    return count >= cells.Length;
  }

  public void AddCell(out Image image, out Button button)
  {
    Debug.Assert(count < cells.Length, "IconPickerIconRow is full. Can't add image.");
    image = cells[count++];
    image.gameObject.SetActive(true);
    button = image.GetComponent<Button>();
    Debug.Assert(button != null, "IconPickerIconRow: all images need to be Buttons!");
  }
}
