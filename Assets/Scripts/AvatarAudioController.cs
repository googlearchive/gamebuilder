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

public class AvatarAudioController : MonoBehaviour
{

  [SerializeField] float MIN_PITCH_PRIMARY_EDITOR = .4f;
  [SerializeField] float MAX_PITCH_PRIMARY_EDITOR = 1f;
  [SerializeField] float MIN_VOLUME_PRIMARY_EDITOR = .3f;
  [SerializeField] float MAX_VOLUME_PRIMARY_EDITOR = 1f;

  [SerializeField] float MIN_PITCH_SECONDARY_EDITOR = .4f;
  [SerializeField] float MAX_PITCH_SECONDARY_EDITOR = 1f;
  [SerializeField] float MIN_VOLUME_SECONDARY_EDITOR = .3f;
  [SerializeField] float MAX_VOLUME_SECONDARY_EDITOR = 1f;

  [SerializeField] float MIN_PITCH_PRIMARY_EXPLORER = .4f;
  [SerializeField] float MAX_PITCH_PRIMARY_EXPLORER = 1f;
  [SerializeField] float MIN_VOLUME_PRIMARY_EXPLORER = .3f;
  [SerializeField] float MAX_VOLUME_PRIMARY_EXPLORER = 1f;

  [SerializeField] float MIN_PITCH_SECONDARY_EXPLORER = .4f;
  [SerializeField] float MAX_PITCH_SECONDARY_EXPLORER = 1f;
  [SerializeField] float MIN_VOLUME_SECONDARY_EXPLORER = .3f;
  [SerializeField] float MAX_VOLUME_SECONDARY_EXPLORER = 1f;

  [SerializeField] float CHANGE_LERP_VAL = .1f;
  [SerializeField] float MAX_AVATAR_SPEED = 2f;

  float lerpValue;

  [SerializeField] AudioSource basicAudioSource;
  [SerializeField] AudioSource primaryLoopAudioSource;
  [SerializeField] AudioSource secondaryLoopAudioSource;
  [SerializeField] AudioClip transformToEditorClip;
  [SerializeField] AudioClip transformToExplorerClip;
  [SerializeField] AudioClip jumpClip;
  [SerializeField] AudioClip landClip;
  [SerializeField] AudioClip damageClip;
  [SerializeField] AudioClip deathClip;
  [SerializeField] AudioClip respawnClip;
  [SerializeField] AudioClip primaryEditorLoopClip;
  [SerializeField] AudioClip secondaryEditorLoopClip;
  [SerializeField] AudioClip primaryExplorerLoopClip;
  [SerializeField] AudioClip secondaryExplorerLoopClip;

  bool isPlayingAsRobot = false;
  bool spatialAudio = false;

  enum AvatarState
  {
    Editor,
    Explorer
  }

  AvatarState avatarState;

  public void OnTransformToEditor()
  {
    avatarState = AvatarState.Editor;
    basicAudioSource.PlayOneShot(transformToEditorClip);

    primaryLoopAudioSource.clip = primaryEditorLoopClip;
    secondaryLoopAudioSource.clip = secondaryEditorLoopClip;
    primaryLoopAudioSource.Stop();
    secondaryLoopAudioSource.Stop();
  }

  public void OnTransformToExplorer()
  {
    avatarState = AvatarState.Explorer;
    if (!isPlayingAsRobot) return;
    basicAudioSource.PlayOneShot(transformToExplorerClip);

    primaryLoopAudioSource.clip = primaryExplorerLoopClip;
    secondaryLoopAudioSource.clip = secondaryExplorerLoopClip;
    primaryLoopAudioSource.Play();
    secondaryLoopAudioSource.Play();
  }

  public void LaunchAsEditor()
  {
    avatarState = AvatarState.Editor;

    primaryLoopAudioSource.clip = primaryEditorLoopClip;
    secondaryLoopAudioSource.clip = secondaryEditorLoopClip;
    primaryLoopAudioSource.Stop();
    secondaryLoopAudioSource.Stop();
  }

  public void LaunchAsExplorer()
  {
    avatarState = AvatarState.Explorer;

    primaryLoopAudioSource.clip = primaryExplorerLoopClip;
    secondaryLoopAudioSource.clip = secondaryExplorerLoopClip;
    primaryLoopAudioSource.Play();
    secondaryLoopAudioSource.Play();
  }

  bool isGrounded = true;
  public void UpdateGrounded(bool on)
  {
    // Debug.Log("GROUND: " + on);
    isGrounded = on;
    if (on)
    {
      OnLand();
    }
  }

  public void SetSpatial(bool on)
  {
    spatialAudio = on;
    float blend = on ? 1 : 0;
    basicAudioSource.spatialBlend = blend;
    primaryLoopAudioSource.spatialBlend = blend;
    secondaryLoopAudioSource.spatialBlend = blend;
  }

  public void OnJump()
  {
    if (!isPlayingAsRobot) return;
    basicAudioSource.PlayOneShot(jumpClip);
  }

  public void OnLand()
  {
    if (!isPlayingAsRobot) return;
    basicAudioSource.PlayOneShot(landClip);
  }

  public void OnDamage()
  {
    if (!isPlayingAsRobot) return;
    basicAudioSource.PlayOneShot(damageClip);
  }

  public void OnDeath()
  {
    if (!isPlayingAsRobot) return;
    basicAudioSource.PlayOneShot(deathClip);
  }

  public void OnRespawn()
  {
    if (!isPlayingAsRobot) return;
    basicAudioSource.PlayOneShot(respawnClip);
  }

  public void UpdateVelocity(float x, float y)
  {
    float speed = (new Vector2(x, y)).magnitude;
    if (avatarState == AvatarState.Explorer && !isGrounded)
    {
      speed *= .5f;
    }

    lerpValue = Mathf.Lerp(lerpValue, speed / MAX_AVATAR_SPEED, CHANGE_LERP_VAL); //low pass filter to smooth changes

    if (avatarState == AvatarState.Editor) UpdateVelocityEditor();
    else UpdateVelocityExplorer();
  }

  public void UpdateVelocityEditor()
  {
    primaryLoopAudioSource.pitch = Mathf.Lerp(MIN_PITCH_PRIMARY_EDITOR, MAX_PITCH_PRIMARY_EDITOR, lerpValue);
    primaryLoopAudioSource.volume = Mathf.Lerp(MIN_VOLUME_PRIMARY_EDITOR, MAX_VOLUME_PRIMARY_EDITOR, lerpValue);
    secondaryLoopAudioSource.pitch = Mathf.Lerp(MIN_PITCH_SECONDARY_EDITOR, MAX_PITCH_SECONDARY_EDITOR, lerpValue);
    secondaryLoopAudioSource.volume = Mathf.Lerp(MIN_VOLUME_SECONDARY_EDITOR, MAX_VOLUME_SECONDARY_EDITOR, lerpValue);
  }

  public void UpdateVelocityExplorer()
  {
    if (!isPlayingAsRobot)
    {
      primaryLoopAudioSource.volume = 0f;
      secondaryLoopAudioSource.volume = 0f;
      return;
    }
    primaryLoopAudioSource.pitch = Mathf.Lerp(MIN_PITCH_PRIMARY_EXPLORER, MAX_PITCH_PRIMARY_EXPLORER, lerpValue);
    primaryLoopAudioSource.volume = Mathf.Lerp(MIN_VOLUME_PRIMARY_EXPLORER, MAX_VOLUME_PRIMARY_EXPLORER, lerpValue);
    secondaryLoopAudioSource.pitch = Mathf.Lerp(MIN_PITCH_SECONDARY_EXPLORER, MAX_PITCH_SECONDARY_EXPLORER, lerpValue);
    secondaryLoopAudioSource.volume = Mathf.Lerp(MIN_VOLUME_SECONDARY_EXPLORER, MAX_VOLUME_SECONDARY_EXPLORER, lerpValue);
  }

  public void SetIsPlayingAsRobot(bool val)
  {
    this.isPlayingAsRobot = val;
  }

}
