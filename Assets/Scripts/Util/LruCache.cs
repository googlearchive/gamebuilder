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

using System.Collections.Generic;

// Simple LRU cache with O(1) get and O(1) put.
public class LruCache<T>
{
  private int capacity;
  // Items ordered by recency -- most recently accessed one is LAST on this list.
  // So the best candidate to discard is at the START of the list.
  private LinkedList<KeyValuePair<string, T>> items = new LinkedList<KeyValuePair<string, T>>();
  // Map from keys to nodes in the linked list above.
  private Dictionary<string, LinkedListNode<KeyValuePair<string, T>>> nodeMap =
      new Dictionary<string, LinkedListNode<KeyValuePair<string, T>>>();
  // Eviction delegate (we call this every time we evict an entry).
  public delegate void OnEvictEntry(string key, T value);
  private OnEvictEntry evictDelegate;

  public LruCache(int capacity, OnEvictEntry evictDelegate = null)
  {
    this.capacity = capacity;
    this.evictDelegate = evictDelegate;
  }

  public T Get(string key)
  {
    LinkedListNode<KeyValuePair<string, T>> node;
    if (nodeMap.TryGetValue(key, out node))
    {
      RefreshRecency(node);
      return node.Value.Value;
    }
    return default(T);
  }

  public void Put(string key, T value)
  {
    LinkedListNode<KeyValuePair<string, T>> node;
    if (nodeMap.TryGetValue(key, out node))
    {
      RefreshRecency(node);
      node.Value = new KeyValuePair<string, T>(key, value);
      return;
    }
    node = new LinkedListNode<KeyValuePair<string, T>>(new KeyValuePair<string, T>(key, value));
    items.AddLast(node);
    nodeMap[key] = node;
    // Note: according to docs LinkedList.Count is O(1).
    if (items.Count > capacity)
    {
      EvictOldest();
    }
  }

  public void Evict(string key)
  {
    LinkedListNode<KeyValuePair<string, T>> node;
    if (nodeMap.TryGetValue(key, out node))
    {
      items.Remove(node);
      nodeMap.Remove(key);
      if (evictDelegate != null)
      {
        evictDelegate(key, node.Value.Value);
      }
    }
  }

  private void RefreshRecency(LinkedListNode<KeyValuePair<string, T>> node)
  {
    items.Remove(node); // This is O(1).
    // LAST on the list means "most recently used".
    items.AddLast(node);
  }

  private void EvictOldest()
  {
    // The oldest (least recently used) item is the first one on the list.
    Evict(items.First.Value.Key);
  }
}