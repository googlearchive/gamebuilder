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

namespace GameBuilder
{
  public class CircularBuffer<T> : IEnumerable<T>
  {
    private T[] buffer;
    private int nextFree = 0;
    private bool hasFilled = false;

    public CircularBuffer(int capacity)
    {
      buffer = new T[capacity];
    }

    public int Capacity => buffer.Length;

    public int Count => hasFilled ? this.Capacity : nextFree;

    public void Add(T item)
    {
      buffer[nextFree] = item;
      if (nextFree == buffer.Length - 1)
      {
        hasFilled = true;
      }
      nextFree = (nextFree + 1) % Capacity;
    }

    public void Clear()
    {
      nextFree = 0;
      hasFilled = false;
    }

    public bool Contains(T item)
    {
      int index = System.Array.IndexOf(buffer, item);

      if (index == -1)
      {
        return false;
      }

      if (!hasFilled && index >= nextFree)
      {
        return false;
      }

      return true;
    }

    // To support foreach
    public struct Enumerator : IEnumerator<T>
    {
      private CircularBuffer<T> buffer;

      private int current;

      public Enumerator(CircularBuffer<T> buffer)
      {
        this.buffer = buffer;
        // MoveNext is called once at start..
        this.current = -1;
      }

      public T Current
      {
        get
        {
          return buffer.buffer[current];
        }
      }

      object IEnumerator.Current => this.Current;

      public void Dispose()
      {
      }

      public bool MoveNext()
      {
        current++;
        return current < buffer.Count;
      }

      public void Reset()
      {
        current = -1;
      }
    }

    public Enumerator GetEnumerator()
    {
      return new Enumerator(this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
      return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return new Enumerator(this);
    }
  }
}