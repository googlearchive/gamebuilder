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

// By "Scoreboard" we just mean anything that relates to the game state and the game's rules.
[System.Serializable]
public struct GameRulesState
{
  [System.Serializable]
  public struct Player
  {
    public string name;
    public int points;
    public int health;
  }

  // [System.Serializable]
  // public struct Team
  // {
  //   public string name;
  //   public int points;
  //   public Player[] players;
  // }

  [System.Serializable]
  public struct RuleDescription
  {
    public string key;
    public string description;
  }

  // public Team[] teams;

  // This can be 'gameover', 'round', or 'start'. See the behaviors for 'Rules'.
  public string state;

  public string winningPlayer;

  public float secondsLeft;

  public RuleDescription[] ruleDescriptions;

  public Player[] players;
}
