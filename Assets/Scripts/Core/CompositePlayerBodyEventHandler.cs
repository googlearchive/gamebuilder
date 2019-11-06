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

// Convenience implementation that can be used to have multiple handlers for a single body's events.
// You can add entire other handlers to the "handlers" list or add individual delegates to the events.
public class CompositePlayerBodyEventHandler : PlayerBody.EventHandler
{
  public event System.Action OnDamagedEvent;
  public event System.Action OnDiedEvent;
  public event System.Action OnJumpDeniedEvent;
  public event System.Action OnJumpedEvent;
  public event System.Action OnLandedEvent;
  public event System.Action OnRespawnedEvent;
  public event System.Action<Color> OnTintChangedEvent;

  public List<PlayerBody.EventHandler> handlers = new List<PlayerBody.EventHandler>();

  public void OnDamaged()
  {
    OnDamagedEvent?.Invoke();
    foreach (var handler in handlers)
    {
      handler.OnDamaged();
    }
  }

  public void OnDied()
  {
    OnDiedEvent?.Invoke();
    foreach (var handler in handlers)
    {
      handler.OnDied();
    }
  }

  public void OnJumpDenied()
  {
    OnJumpDeniedEvent?.Invoke();
    foreach (var handler in handlers)
    {
      handler.OnJumpDenied();
    }
  }

  public void OnJumped()
  {
    OnJumpedEvent?.Invoke();
    foreach (var handler in handlers)
    {
      handler.OnJumped();
    }
  }

  public void OnLanded()
  {
    OnLandedEvent?.Invoke();
    foreach (var handler in handlers)
    {
      handler.OnLanded();
    }
  }

  public void OnRespawned()
  {
    OnRespawnedEvent?.Invoke();
    foreach (var handler in handlers)
    {
      handler.OnRespawned();
    }
  }

  public void OnTintChanged(Color tint)
  {
    OnTintChangedEvent?.Invoke(tint);
    foreach (var handler in handlers)
    {
      handler.OnTintChanged(tint);
    }
  }
}
