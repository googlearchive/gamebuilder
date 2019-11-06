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
namespace BehaviorUX
{
  public abstract class ArrayPropertyField<T, F> : PropertyField, IEditableListAdapter<F> where F : TypeField<T>
  {
    [SerializeField] TMPro.TextMeshProUGUI label;
    [SerializeField] F fieldPrefab;
    [SerializeField] EditableListUI editableListUI;

    private EditableList<F, ArrayPropertyField<T, F>> list;

    public delegate void OnValueChanged(T[] value);
    public event OnValueChanged onValueChanged;

    protected abstract T GetDefaultValue();

    protected override void Initialize()
    {
      label.text = editor.labelForDisplay;

      list = new EditableList<F, ArrayPropertyField<T, F>>(this, editableListUI);
      list.onRequestAddItem += OnRequestAddItem;
      list.onRequestMoveItem += OnRequestMoveItem;
      list.onRequestDeleteItem += OnRequestDeleteItem;

      Update();
    }

    public virtual F Inflate(RectTransform container)
    {
      F field = Instantiate(fieldPrefab, container);
      return field;
    }

    public int GetCount()
    {
      return ((T[])editor.data).Length;
    }

    public void Populate(int index, F field)
    {
      T[] array = (T[])editor.data;
      Debug.Log("??? " + index + ", " + array[index]);
      field.SetLabel("");
      field.SetValue(array[index]);
      field.SetListener((value) =>
      {
        T[] newArray = (T[])array.Clone();
        newArray[index] = value;
        WriteData(newArray);
      });
    }

    void OnRequestAddItem(int index)
    {
      T[] array = (T[])editor.data;
      T value = index > 0 ? array[index - 1] : GetDefaultValue();
      T[] newArray = new T[array.Length + 1];
      Array.Copy(array, newArray, index);
      Array.ConstrainedCopy(array, index, newArray, index + 1, array.Length - index);
      newArray[index] = value;
      WriteData(newArray);
    }

    void OnRequestMoveItem(int fromIndex, int toIndex)
    {
      T[] array = (T[])editor.data;
      T[] newArray = new T[array.Length];
      Array.Copy(array, newArray, fromIndex);
      if (fromIndex < toIndex)
      {
        Array.ConstrainedCopy(array, fromIndex + 1, newArray, fromIndex, toIndex - fromIndex);
        newArray[toIndex] = array[fromIndex];
        Array.ConstrainedCopy(array, toIndex + 1, newArray, toIndex + 1, array.Length - (toIndex + 1));
      }
      else
      {
        newArray[toIndex] = array[fromIndex];
        Array.ConstrainedCopy(array, toIndex, newArray, toIndex + 1, fromIndex - toIndex);
        Array.ConstrainedCopy(array, fromIndex + 1, newArray, fromIndex + 1, array.Length - (fromIndex + 1));
      }
      WriteData(newArray);
    }

    void OnRequestDeleteItem(int index)
    {
      T[] array = (T[])editor.data;
      T[] newArray = new T[array.Length - 1];
      Array.Copy(array, newArray, index);
      Array.ConstrainedCopy(array, index + 1, newArray, index, array.Length - (index + 1));
      WriteData(newArray);
    }

    void WriteData(T[] array)
    {
      editor.SetData(array);
      onValueChanged?.Invoke(array);
      list.Refresh();
    }

    private void Update()
    {
      list.Update();
    }
  }
}