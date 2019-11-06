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

public class UnreliableMessageSystem : MonoBehaviour
{
  public delegate void OnUnreliableMessageReceived<T>(T content);

  private Dictionary<byte, System.Action<object, int>> messageHandlers = new Dictionary<byte, System.Action<object, int>>();

  private RaiseEventOptions raiseEventOptions = new RaiseEventOptions
  {
    Receivers = ReceiverGroup.Others,
    CachingOption = EventCaching.DoNotCache
  };

  public void OnEnable()
  {
    PhotonNetwork.OnEventCall += OnEvent;
  }

  public void OnDisable()
  {
    PhotonNetwork.OnEventCall -= OnEvent;
  }

  public void AddHandler<T>(byte eventCode, OnUnreliableMessageReceived<T> handler)
  {
    if (messageHandlers.ContainsKey(eventCode))
    {
      throw new System.Exception("Already had an unreliable message handler for event code " + eventCode);
    }
    messageHandlers[eventCode] = (content, senderId) => handler(JsonUtility.FromJson<T>((string)content));
  }

  public void AddRawHandler(byte eventCode, System.Action<object, int> handler)
  {
    if (messageHandlers.ContainsKey(eventCode))
    {
      throw new System.Exception("Already had an unreliable message handler for event code " + eventCode);
    }
    messageHandlers[eventCode] = handler;
  }

  public void RemoveHandler(byte eventCode)
  {
    if (messageHandlers[eventCode] != null)
    {
      messageHandlers.Remove(eventCode);
    }
  }

  void OnEvent(byte eventCode, object content, int senderId)
  {
    System.Action<object, int> handler;
    if (messageHandlers.TryGetValue(eventCode, out handler))
    {
      handler.Invoke(content, senderId);
    }
  }

  public void Send<T>(byte eventCode, T content)
  {
    SendRaw(eventCode, JsonUtility.ToJson(content));
  }

  public void SendRaw(byte eventCode, object content)
  {
    PhotonNetwork.RaiseEvent(eventCode, content, false, raiseEventOptions);
  }

}
