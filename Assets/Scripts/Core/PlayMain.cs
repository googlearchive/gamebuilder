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
using System.Linq;

public class PlayMain : AvatarMain, PlayerBody.Controller
{
  /* [SerializeField] GameObject buttonPromptRoot;
  [SerializeField] TMPro.TextMeshProUGUI buttonPromptField; */

  [SerializeField] ActionPrompt actionPrompt;
  [SerializeField] Transform childTransform;

  [SerializeField] DamageScreenEffect damageScreenEffect;
  [SerializeField] DamageScreenEffect deathScreenEffect;
  [SerializeField] VoosEngine voosEngine;
  PlayerBody playerBody;
  Vector3 lastAvatarPos;

  CompositePlayerBodyEventHandler playerBodyEventHandler = new CompositePlayerBodyEventHandler();

  public override void Setup(UserMain _usermain)
  {
    base.Setup(_usermain);
    Util.FindIfNotSet(this, ref voosEngine);

    playerBodyEventHandler.handlers.Clear();
    playerBodyEventHandler.handlers.Add(navigationControls.userBody);
    playerBodyEventHandler.OnDiedEvent += OnDied;
    playerBodyEventHandler.OnDamagedEvent += OnDamage;

    // The player's tint won't necessarily change, so make sure we use the
    // latest. Not thrilled about this explicit call..feels hacky.
    //navigationControls.userBody.SetTint(playerBody.GetVoosActor().GetTint());
  }

  void AbandonPlayerBody()
  {
    if (playerBody != null)
    {
      playerBody.StopControlling(this);
      playerBody = null;
    }
  }

  public void SetPlayerBody(PlayerBody newPlayerBody)
  {
    AbandonPlayerBody();
    playerBody = newPlayerBody;
    if (playerBody != null)
    {
      playerBody.StartControlling(this);
      bodyParent = playerBody.GetAvatarTransform();
      navigationControls.UpdateRotationValues(playerBody.GetHeadTransform().rotation);
      childTransform.SetParent(playerBody.GetHeadTransform());
      SetAvatarTransform(playerBody.transform);
    }
    else
    {
      bodyParent = null;
      navigationControls.UpdateRotationValues(Quaternion.identity);
      childTransform.SetParent(null);
      SetAvatarTransform(null);
    }

    childTransform.localPosition = Vector3.zero;
    childTransform.localRotation = Quaternion.identity;
  }

  public void ResetPlayerBody() // TODO: What does this mean? Do we still need this?
  {
    AbandonPlayerBody();
  }

  public VoosActor GetPlayerActor()
  {
    return playerBody != null ? playerBody.GetVoosActor() : null;
  }

  void OnDamage()
  {
    damageScreenEffect.TriggerEffect();
  }

  void OnDied()
  {
    deathScreenEffect.TriggerEffect();
  }

  public Vector3 GetPlayerScale()
  {
    if (playerBody == null) return Vector3.one;
    return playerBody.GetVoosActor().GetLocalScale();
  }

  private void Update()
  {
    lastAvatarPos = playerBody != null ? playerBody.transform.position : lastAvatarPos;

    if (!userMain.CursorOverUI())
    {
      if (inputControl.GetButtonDown("Action1"))
      {
        navigationControls.TryCaptureCursor();
      }
    }

    // Temporary band-aid guard, since this assumption runs deep.
    if (playerBody != null && navigationControls.userBody != null)
    {
      navigationControls.SetUserBodyVelocity(playerBody.GetVelocity());
      navigationControls.SetGrounded(playerBody.GetIsTouchingGround());
    }
    UpdatePlayUI();

    navigationControls.userBody.SetPlayerVisible(false);
  }

  public override void Teleport(Vector3 newPos, Quaternion newRot)
  {
    if (playerBody != null)
    {
      playerBody.Teleport(newPos, newRot);
      navigationControls.UpdateRotationValues(newRot);
    }
    lastAvatarPos = newPos;
  }

  string loseMessage = "YOU LOSE";
  string winMessage = "YOU WIN";

  void UpdatePlayUI()
  {
    if (playerBody == null)
    {
      return;
    }

    UpdatePrompts();
  }


  public override Quaternion GetAim()
  {
    return playerBody != null ? playerBody.GetHeadTransform().rotation : Quaternion.identity;
  }

  public override Vector3 GetAvatarPosition()
  {
    return lastAvatarPos;
  }

  string GetPlayerActorName()
  {
    if (playerBody == null)
    {
      return null;
    }
    return playerBody.GetComponent<VoosActor>().GetName();
  }

  private readonly System.Text.StringBuilder PromptBuilder = new System.Text.StringBuilder();
  void UpdatePrompts()
  {
    actionPrompt.UpdatePrompts(voosEngine.GetToolTipsForPlayer(GetPlayerActorName()).ToArray());
  }

  PlayerBody.ControllerInput PlayerBody.Controller.GetInput()
  {
    // If we're not in play mode, never send any input.
    if (this.IsActive())
    {
      return navigationControls;
    }
    else
    {
      return null;
    }
  }

  PlayerBody.EventHandler PlayerBody.Controller.GetEventHandler()
  {
    // If we're not in play mode, never send any input.
    if (this.IsActive())
    {
      return playerBodyEventHandler;
    }
    else
    {
      return null;
    }
  }

  string PlayerBody.Controller.GetName()
  {
    return $"PlayMain ({this.name})";

  }
}
