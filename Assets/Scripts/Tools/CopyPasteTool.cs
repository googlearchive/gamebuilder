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

public class CopyPasteTool : Tool
{
  [SerializeField] Transform containerNode;
  [SerializeField] Material copyMaterial;

  VoosEngine voosEngine;
  UndoStack undoStack;
  //HashSet<VoosActor> actorsToCopy = new HashSet<VoosActor>();

  enum Mode
  {
    Copy,
    Paste
  };

  Mode mode = Mode.Copy;
  public override bool Trigger(bool on)
  {
    base.Trigger(on);

    if (on)
    {
      return OnTrigger();
    }

    return true;
  }

  bool OnTrigger()
  {
    if (mode == Mode.Paste)
    {
      Paste();
      //Cleanup();
      return true;
    }


    if (hoverActor != null && !hoverActor.IsLockedByAnother())
    {
      bool addedOrPresent = editMain.AddSetOrRemoveTargetActor(hoverActor);
      return true;
    }
    else
    {
      if (!Util.HoldingModiferKeys())
      {
        editMain.ClearTargetActors();
      }
      return false;
    }
  }

  private void Paste()
  {
    List<VoosEngine.CopyPasteActorRequest> copyPasteRequests = new List<VoosEngine.CopyPasteActorRequest>();

    foreach (KeyValuePair<VoosActor, GameObject> entry in pastePreview)
    {
      if (entry.Key == null) continue;

      VoosActor baseActor = entry.Key.GetCloneParentActor() ?? entry.Key;
      if (baseActor.IsLockedByAnother() || baseActor == null) continue;

      int count = voosEngine.CountCopiesOf(baseActor);
      string baseName = baseActor.GetDisplayName();
      string copyName = baseName + "-" + (count + 1);
      Vector3 position = entry.Value.transform.position;
      Quaternion rotation = entry.Key.GetRotation();

      copyPasteRequests.Add(new VoosEngine.CopyPasteActorRequest
      {
        source = entry.Key,
        pastedPosition = position,
        pastedRotation = rotation,
        pastedDisplayName = copyName
      });
    }

    List<VoosActor> pastedActors = voosEngine.CopyPasteActors(copyPasteRequests);

    undoStack.PushUndoForCreatingActors(pastedActors, $"Paste {pastedActors.Count} actors");
  }

  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref undoStack);

    SetToPaste();
  }

  public override void Close()
  {
    ClearPreview();
  }

  public override bool TargetsSpace()
  {
    return mode == Mode.Paste;
  }

  public override bool TargetsGround()
  {
    return mode == Mode.Paste;
  }

  public override bool OnEscape()
  {
    Cleanup();
    return true;
  }

  void Cleanup()
  {
    ClearPreview();
    editMain.RemoveToolFromList(this);
  }


  void Update()
  {
    if (mode == Mode.Copy) return;

    foreach (KeyValuePair<VoosActor, GameObject> entry in pastePreview)
    {
      if (entry.Key == null)
      {
        Cleanup();
        return;
      }
      VoosActor baseActor = entry.Key.GetCloneParentActor() ?? entry.Key;
      if (baseActor.IsLockedByAnother() || baseActor == null)
      {
        Cleanup();
        return;
      }
    }

    // UpdatePastePosition();
  }

  void ClearPreview()
  {
    foreach (KeyValuePair<VoosActor, GameObject> entry in pastePreview)
    {
      Destroy(entry.Value);
    }
    pastePreview.Clear();
  }



  Dictionary<VoosActor, GameObject> pastePreview = new Dictionary<VoosActor, GameObject>();
  public void SetToPaste()
  {
    VoosActor focusActor = editMain.GetFocusedTargetActor();
    if (focusActor == null)
    {
      return;
    }

    containerNode.SetPositionAndRotation(focusActor.GetPosition(), focusActor.GetRotation());

    foreach (VoosActor actor in voosEngine.GetActorsAndDescendants(editMain.GetTargetActors()))
    {
      pastePreview[actor] = GetPreviewOfActor(actor);
    }

    mode = Mode.Paste;
  }

  private GameObject GetPreviewOfActor(VoosActor actor)
  {
    GameObject actorRep = new GameObject("rep");
    actorRep.transform.parent = containerNode;

    actorRep.transform.position = actor.GetPosition();
    actorRep.transform.rotation = actor.GetRotation();

    Bounds _newbounds = new Bounds(actorRep.transform.position, Vector3.zero);

    foreach (MeshFilter mf in actor.gameObject.GetComponentsInChildren<MeshFilter>())
    {
      // TextMeshPro becomes unhappy when its meshes are assigned to another MeshFilter
      // (it stops updating the text), so skip TextMeshPro meshes...
      if (mf.GetComponent<TMPro.TextMeshPro>()) continue;

      GameObject _newGameobject = new GameObject("copymesh");

      // Note: using mf.mesh instead of mf.sharedMesh, for reasons I don't understand,
      // causes a native crash in the EXE but not in the Unity Editor when copying
      // actors that have TextMeshPro components on them, even though we have the
      // guard above to prevent us from copying TextMeshPro objects. Using
      // mf.sharedMesh solved the problem.
      _newGameobject.AddComponent<MeshFilter>().mesh = mf.sharedMesh;
      _newGameobject.AddComponent<MeshRenderer>().material = copyMaterial;

      _newGameobject.transform.SetParent(actorRep.transform);
      _newGameobject.transform.position = mf.transform.position;
      _newGameobject.transform.rotation = mf.transform.rotation;
      _newGameobject.transform.localScale = mf.transform.lossyScale;

      _newbounds.Encapsulate(_newGameobject.GetComponent<MeshRenderer>().bounds);
    }

    return actorRep;
  }

  public override string GetName()
  {
    return "Copy";
  }

  public override void UpdatePosition(Vector3 newpos)
  {
    containerNode.position = Util.IsControlOrCommandHeld() ? TerrainManager.SnapPosition(newpos) : newpos;
  }
}
