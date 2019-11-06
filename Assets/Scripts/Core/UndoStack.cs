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
using CT = CommandTerminal;

public class UndoStack : MonoBehaviour
{
  static UndoStack lastCreated;

  const int MaxUndoItems = 50;

  public class Item
  {
    // This should describe what was done, not what undo will do. So like,
    // "Deleted actor." Not "Un-delete actor".
    public string actionLabel;

    // Return null if we're OK to do.
    public System.Func<string> getUnableToDoReason;
    public System.Action doIt;

    // Return null if we're OK to undo.
    public System.Func<string> getUnableToUndoReason;
    public System.Action undo;
  }

  public event System.Action onPushed;
  public event System.Action<Item> onUndone;
  public event System.Action<Item> onRedone;

  // Use a doubly linked list so we can limit the size. Ie. after max items,
  // start removing stuff "from the bottom" (ie. front) of the queue.
  LinkedList<Item> stack = new LinkedList<Item>();
  LinkedList<Item> redoStack = new LinkedList<Item>();

  DynamicPopup popups;

  bool waitingOnPopup = false;

  void Awake()
  {
    Util.FindIfNotSet(this, ref popups);
    lastCreated = this;
  }

  public void Push(Item item, bool immediatelyCallDo = true)
  {
    // Do the action.
    if (immediatelyCallDo)
    {
      Debug.AssertFormat(item.getUnableToDoReason() == null, $"Undo item '{item.actionLabel}' was pushed, but it returned non-null 'getUnableToDoReason': {item.getUnableToDoReason()}");
      item.doIt();
    }

    // The user is proceeding normally, so clear redo.
    redoStack.Clear();

    // Add to undo stack.
    stack.AddLast(item);
    while (stack.Count > MaxUndoItems)
    {
      stack.RemoveFirst();
    }
    onPushed?.Invoke();
  }

  public bool IsEmpty()
  {
    return stack.Count == 0;
  }

  public bool IsRedoEmpty()
  {
    return redoStack.Count == 0;
  }

  public Item GetTopItem()
  {
    return stack.Last.Value;
  }

  public void TriggerUndo()
  {
    if (IsEmpty()) return;
    if (waitingOnPopup) return;

    var node = stack.Last;
    var item = node.Value;

    string unableReason = item.getUnableToUndoReason();
    if (unableReason != null)
    {
      waitingOnPopup = true;
      popups.ShowTwoButtons(
        $"Woops, we cannot undo '{item.actionLabel}'. {unableReason}",
        "Try again later", () =>
        {
          waitingOnPopup = false;
        },
        "Delete this undo step", () =>
        {
          // Remove this from undo history
          stack.Remove(node);
          waitingOnPopup = false;
        },
        600f);
    }
    else
    {
      // Undo it, and move it onto redo stack.
      item.undo();
      stack.Remove(node);
      redoStack.AddLast(item);

      onUndone?.Invoke(item);
    }
  }

  public void TriggerRedo()
  {
    if (IsRedoEmpty()) return;
    if (waitingOnPopup) return;
    var node = redoStack.Last;
    var item = node.Value;

    string unableReason = item.getUnableToDoReason();
    if (unableReason != null)
    {
      waitingOnPopup = true;
      popups.ShowTwoButtons(
        $"Woops, we cannot re-do '{item.actionLabel}'. {unableReason}",
        "Try again later", () =>
        {
          // Leave everything as is
          waitingOnPopup = false;
        },
        "Delete this undo step", () =>
        {
          // Remove this from undo history
          redoStack.Remove(node);
          waitingOnPopup = false;
        },
        600f);
    }
    else
    {
      // Redo it, push it back onto undo stack.
      item.doIt();
      redoStack.Remove(node);
      stack.AddLast(item);

      onRedone?.Invoke(item);
    }
  }

  [CT.RegisterCommand(Help = "Show undo/redo stack")]
  static void CommandUndos(CT.CommandArg[] args)
  {
    CommandTerminal.HeadlessTerminal.Log("== UNDOS:");
    foreach (var item in lastCreated.stack)
    {
      CommandTerminal.HeadlessTerminal.Log(item.actionLabel);
    }

    CommandTerminal.HeadlessTerminal.Log("== REDOS:");
    foreach (var item in lastCreated.redoStack)
    {
      CommandTerminal.HeadlessTerminal.Log(item.actionLabel);
    }
  }
}
