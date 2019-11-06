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
using UnityEngine;

public class InputControl : MonoBehaviour
{
  [SerializeField] UserMain userMain;

  struct ActionMap
  {
    public KeyCode keyCode;
    public KeyCode modKeyCode;
    public KeyCode keyCodeAlt;
    public KeyCode modKeyCodeAlt;
  }

  // Movement and mouse X/Y/wheel are handled with Unity's built in Input manager
  // All other input actions are here
  Dictionary<string, ActionMap> actions = new Dictionary<string, ActionMap>{
    {"Jump",new ActionMap(){
      keyCode=KeyCode.Space,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"View",new ActionMap(){
      keyCode=KeyCode.V,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Action1",new ActionMap(){
      keyCode=KeyCode.Mouse0,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Action2",new ActionMap(){
      keyCode=KeyCode.Mouse1,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"MoveCamera",new ActionMap(){
      keyCode=KeyCode.Mouse1,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"RotateCamera",new ActionMap(){
      keyCode=KeyCode.Mouse2,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"ToggleBuildPlay",new ActionMap(){
      keyCode=KeyCode.Tab,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Sprint",new ActionMap(){
      keyCode=KeyCode.LeftShift,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Pause",new ActionMap(){
      keyCode=KeyCode.P,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Reset",new ActionMap(){
      keyCode=KeyCode.R,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Undo",new ActionMap(){
      keyCode=KeyCode.Z,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.Z,
      modKeyCodeAlt=KeyCode.LeftCommand}},
    {"Redo",new ActionMap(){
      keyCode=KeyCode.Y,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.Y,
      modKeyCodeAlt=KeyCode.LeftCommand}},
    {"Copy",new ActionMap(){
      keyCode=KeyCode.C,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.C,
      modKeyCodeAlt=KeyCode.LeftCommand}},
    {"Delete",new ActionMap(){
      keyCode=KeyCode.Delete,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.Backspace,
      modKeyCodeAlt=KeyCode.None}},
    {"Focus",new ActionMap(){
      keyCode=KeyCode.F,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Snap",new ActionMap(){
      keyCode=KeyCode.LeftControl,
      modKeyCode=KeyCode.LeftControl, //hack
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"PrevToolOption",new ActionMap(){
      keyCode=KeyCode.Q,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"NextToolOption",new ActionMap(){
      keyCode=KeyCode.E,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"PrevToolSecondaryOption",new ActionMap(){
      keyCode=KeyCode.Z,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"NextToolSecondaryOption",new ActionMap(){
      keyCode=KeyCode.X,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"ToolOption1",new ActionMap(){
      keyCode=KeyCode.Alpha1,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"ToolOption2",new ActionMap(){
      keyCode=KeyCode.Alpha2,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"ToolOption3",new ActionMap(){
      keyCode=KeyCode.Alpha3,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"ToolOption4",new ActionMap(){
      keyCode=KeyCode.Alpha4,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Rotate",new ActionMap(){
      keyCode=KeyCode.R,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"ToggleCursor",new ActionMap(){
      keyCode=KeyCode.LeftAlt,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Save",new ActionMap(){
      keyCode=KeyCode.S,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.S,
      modKeyCodeAlt=KeyCode.LeftCommand}},
    {"ListActors",new ActionMap(){
      keyCode=KeyCode.L,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.L,
      modKeyCodeAlt=KeyCode.LeftCommand}},
    {"Console",new ActionMap(){
      keyCode=KeyCode.BackQuote,
      modKeyCode=KeyCode.None,
      keyCodeAlt=KeyCode.None,
      modKeyCodeAlt=KeyCode.None}},
    {"Parent",new ActionMap(){
      keyCode=KeyCode.H,
      modKeyCode=KeyCode.LeftControl,
      keyCodeAlt=KeyCode.H,
      modKeyCodeAlt=KeyCode.LeftCommand}}
  };

  HashSet<KeyCode> overlappingKeycodes = new HashSet<KeyCode>();
  public void Setup()
  {
    //see which keys have nones that matter
    foreach (KeyValuePair<string, ActionMap> action in actions)
    {
      PopulateKeyCodeOverlapSet(action.Value, action.Value.keyCode);
      PopulateKeyCodeOverlapSet(action.Value, action.Value.keyCodeAlt);
    }
  }

  bool DoesKeyCodeOverlap(KeyCode kc)
  {
    if (kc == KeyCode.None) return false;
    return overlappingKeycodes.Contains(kc);
  }

  void PopulateKeyCodeOverlapSet(ActionMap comparedAction, KeyCode kc)
  {
    foreach (KeyValuePair<string, ActionMap> action in actions)
    {
      if (comparedAction.Equals(action.Value)) continue;
      if (kc == action.Value.keyCode
      || kc == action.Value.keyCodeAlt)
      {
        overlappingKeycodes.Add(kc);
        break;
      }
    }
  }

  public bool GetButton(string action)
  {
    if (!actions.ContainsKey(action)) Debug.Log(action);

    return (Input.GetKey(actions[action].keyCode) && ModKeyHeld(actions[action].keyCode, actions[action].modKeyCode)) ||
    (Input.GetKey(actions[action].keyCodeAlt) && ModKeyHeld(actions[action].keyCodeAlt, actions[action].modKeyCodeAlt));
  }

  public bool GetButtonDown(string action)
  {
    if (!actions.ContainsKey(action)) Debug.Log(action);

    return (Input.GetKeyDown(actions[action].keyCode) && ModKeyHeld(actions[action].keyCode, actions[action].modKeyCode)) ||
    (Input.GetKeyDown(actions[action].keyCodeAlt) && ModKeyHeld(actions[action].keyCodeAlt, actions[action].modKeyCodeAlt));
  }

  public bool GetButtonUp(string action)
  {
    if (!actions.ContainsKey(action)) Debug.Log(action);

    return (Input.GetKeyUp(actions[action].keyCode) && ModKeyHeld(actions[action].keyCode, actions[action].modKeyCode)) ||
    (Input.GetKeyUp(actions[action].keyCodeAlt) && ModKeyHeld(actions[action].keyCodeAlt, actions[action].modKeyCodeAlt));
  }

  //ctrl, cmd or none?
  public bool ModKeyHeld(KeyCode primaryKey, KeyCode modKey)
  {
    if (modKey == KeyCode.LeftControl || modKey == KeyCode.RightControl)
    {
      return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }
    else if (modKey == KeyCode.LeftCommand || modKey == KeyCode.RightCommand)
    {
      return Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
    }
    else if (modKey == KeyCode.None)
    {
      if (DoesKeyCodeOverlap(primaryKey))
      {
        return !Input.GetKey(KeyCode.LeftControl)
        && !Input.GetKey(KeyCode.RightControl)
        && !Input.GetKey(KeyCode.LeftCommand)
        && !Input.GetKey(KeyCode.RightCommand);
      }
      else
      {
        return true;
      }

    }
    else //unforeseen
    {
      return Input.GetKey(modKey);
    }
  }

  public Vector3 GetMoveAxes()
  {
    return new Vector3(Input.GetAxis("Move X"), Input.GetAxis("Move Y"), Input.GetAxis("Move Z"));
  }

  public string GetKeysForAction(string action)
  {
    if (!actions.ContainsKey(action)) return "";

    ActionMap map = actions[action];
    string keys = map.modKeyCode == KeyCode.None ? "" : ScrubName(map.modKeyCode.ToString()) + "+";
    keys += ScrubName(map.keyCode.ToString());

    if (map.keyCodeAlt != KeyCode.None)
    {
      string altKey = map.modKeyCodeAlt == KeyCode.None ? "" : ScrubName(map.modKeyCodeAlt.ToString()) + "+";
      altKey += ScrubName(map.keyCodeAlt.ToString());
      keys += altKey;
    }

    return keys;
  }



  public Vector2 GetLookAxes()
  {
    float invertMod = 1;
    if (userMain.playerOptions.invertMouselook) invertMod = -1;
    return new Vector2(Input.GetAxis("Mouse X") * userMain.playerOptions.mouseLookSensitivity, invertMod * userMain.playerOptions.mouseLookSensitivity * Input.GetAxis("Mouse Y"));
  }

  public float GetZoom()
  {
    return Input.GetAxis("Mouse ScrollWheel") * userMain.playerOptions.mouseWheelSensitivity;
  }

  public Vector2 GetMouseAxes()
  {
    return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
  }

  public int GetNumberKeyDown()
  {
    for (int i = 0; i < 10; i++)
    {
      if (Input.GetKeyDown((KeyCode)(48))) return 9;
      if (Input.GetKeyDown((KeyCode)(48 + i))) return i - 1;
    }
    return -1;
  }

  public static string ConvertMouseButtonName(string name)
  {
    if (name.Contains("Left")) return "LMB";
    if (name.Contains("Right")) return "RMB";
    else if (name.Contains("Middle")) return "MMB";
    else if (name.Contains("Mouse Wheel")) return name.Substring(6, name.Length - 6);
    else return name;
  }

  public static string ScrubName(string name)
  {
    if (name.Contains("Keypad")) return name.Substring(7, name.Length - 7);
    if (name.Contains("Alpha"))
    {
      return name.Substring(5, name.Length - 5);
    }
    if (name.Contains("Control")) return "Ctrl";
    if (name.Contains("Command")) return "Cmd";
    return name;
  }



}




