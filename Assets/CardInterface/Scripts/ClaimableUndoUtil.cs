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

public static class ClaimableUndoUtil
{

  public struct ClaimableUndoItem
  {
    public string resourceId;
    public string resourceName;
    public string label;
    public GetInvalidResourceReason cannotDoReason;
    public GetInvalidResourceReason cannotUndoReason;
    public System.Action doIt;
    public System.Action undo;
  }

  public delegate string GetInvalidResourceReason();

  public static bool TryClaimAndDo(ClaimKeeper keeper, string resourceId, System.Action doAction)
  {
    // Since this is a synchronous action and we already checked the claim status,
    // do the operation without requesting another claim token.
    doAction();
    return true;
  }

  public static string GetUnableToEditResourceReason(ClaimKeeper keeper, string resourceId, string resourceName, GetInvalidResourceReason reason)
  {
    string ownerNickname = keeper.GetEffectiveOwnerNickname(resourceId);
    if (ownerNickname != null && !keeper.IsMine(resourceId))
    {
      return $"{ownerNickname} is editing '{resourceName}'.";
    }
    if (reason != null) return reason();
    return null;
  }

  public static void PushUndoForResource(this UndoStack stack, ClaimKeeper keeper, ClaimableUndoItem undoItem)
  {
    stack.Push(new UndoStack.Item
    {
      actionLabel = undoItem.label,
      getUnableToDoReason = () => GetUnableToEditResourceReason(keeper, undoItem.resourceId, undoItem.resourceName, undoItem.cannotDoReason),
      getUnableToUndoReason = () => GetUnableToEditResourceReason(keeper, undoItem.resourceId, undoItem.resourceName, undoItem.cannotUndoReason),
      doIt = () => TryClaimAndDo(keeper, undoItem.resourceId, undoItem.doIt),
      undo = () => TryClaimAndDo(keeper, undoItem.resourceId, undoItem.undo)
    });
  }
}