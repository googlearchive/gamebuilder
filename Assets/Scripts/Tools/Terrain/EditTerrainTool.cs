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
using System.Linq;
using UnityEngine;
using static TerrainManager;

public class EditTerrainTool : MonoBehaviour
{
  [SerializeField] EditTerrainPreview editTerrainPreview;
  [SerializeField] CopyPasteTerrainPreview copyPasteTerrainPreview;
  [SerializeField] TerrainTool terrainTool;
  EditMain editMain;
  InputControl inputControl;
  TerrainManager terrain;

  Cell startCell;
  Cell endCell;

  enum State
  {
    Copy,
    Cut,
    None
  };
  Vector3 copyOffset;

  State state = State.None;

  internal void Setup()
  {
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref inputControl);
    Util.FindIfNotSet(this, ref terrain);
    copyPasteTerrainPreview.Setup();
  }

  internal void Close()
  {
    EndCopy();
    TryClearSelection();
    editTerrainPreview.DestroyPreviewObjects();
    terrainTool.operating = false;

    //     if (this.mode == Mode.CopyPaste)
    // {
    //   ClearCopy();
    // }
  }

  internal void SetPreviewVisibility(bool on)
  {
    editTerrainPreview.SetVisibility(on && state == State.None);
  }

  internal void Trigger(bool on)
  {
    if (on)
    {
      if (state == State.Copy)
      {
        PasteCells();
      }
    }
  }

  internal void EndEditSelect()
  {
    terrainTool.operating = false;
  }

  internal void StartEditSelect(Cell cell)
  {
    if (state != State.Copy)
    {
      editTerrainPreview.ClearPreview();
      terrainTool.operationCandidates.Clear();
      terrainTool.operationCandidates.Add(new Util.Tuple<Cell, CellValue>(cell, terrain.GetCellValue(cell)));
      startCell = cell;
      endCell = cell;
      UpdateOperationCandidatesAndPreview();
      terrainTool.operating = true;
    }
  }

  private void UpdateOperationCandidatesAndPreview()
  {
    terrainTool.operationCandidates = new HashSet<Util.Tuple<Cell, CellValue>>(terrain.GetFilledCells(startCell, endCell));
    editTerrainPreview.UpdateCells(terrainTool.operationCandidates);
  }

  internal void EditSelectUpdate(Cell cell)
  {
    if (state != State.None) return;

    if (!endCell.Equals(cell))
    {
      endCell = cell;
      UpdateOperationCandidatesAndPreview();
    }
  }

  internal void DeleteSelection()
  {
    terrainTool.DeleteOperationCandidates();
    editTerrainPreview.ClearPreview();
  }

  internal void PaintSelection()
  {
    terrainTool.PaintOperationCandidates();
  }

  internal void ToolUpdate()
  {
    if (inputControl.GetButtonDown("Delete"))
    {
      DeleteSelection();
    }

    if (inputControl.GetButtonDown("Copy"))
    {
      CopySelection();
    }

    copyPasteTerrainPreview.SetVisibility(state == State.Copy);
  }

  internal void CopySelection()
  {
    if (terrainTool.operationCandidates.Count == 0) return;

    state = State.Copy;
    copyPasteTerrainPreview.SelectCellGroup(terrainTool.operationCandidates);
  }

  internal void UpdatePosition(Vector3 newpos)
  {
    if (state == State.Copy) CopyUpdate(newpos);
    //throw new NotImplementedException();
  }

  private void CopyUpdate(Vector3 newpos)
  {
    if (terrainTool.operationCandidates.Count == 0) return;

    Vector3 pos;
    Cell cell;


    bool validCellFound = terrain.SnapToEmptyCell(newpos, editMain.groundHitNormal, out cell, out pos);

    if (!validCellFound)
    {
      terrain.SnapToCell(newpos, out cell, out pos);
    }


    // terrain.SnapToCell(newpos, out cell, out pos);
    copyOffset = pos - GetCellCenter(terrainTool.operationCandidates.Last().first);
    copyPasteTerrainPreview.UpdateOffset(copyOffset);
  }

  internal void PasteCells()
  {
    terrainTool.RefreshCandidates();
    terrainTool.PasteOperationCandidates(copyOffset);
  }

  public bool OnEscape()
  {
    if (state == State.Copy)
    {
      EndCopy();
      return true;
    }

    return TryClearSelection();

  }

  private bool TryClearSelection()
  {
    if (terrainTool.operationCandidates.Count > 0)
    {
      terrainTool.operationCandidates.Clear();
      editTerrainPreview.ClearPreview();
      return true;
    }

    return false;
  }

  private void EndCopy()
  {
    state = State.None;
    copyPasteTerrainPreview.Clear();
  }
}

/* else if (mode == Mode.CopyPaste)
    {
      if (copyPasteMode == CopyPasteMode.Copy)
      {
        if (on) StartCopy();
        else EndCopy();
      }
      else
      {
        if (on) PasteCells();
      }
    }
    
    
    
    
    
  void StartCopy()
  {
    operating = true;
    if (!Util.IsShiftHeld()) ClearCopy();
  }

  void ClearCopy()
  {
    copyPasteTerrainPreview.Clear();
    operationCandidates.Clear();
  }

  void EndCopy()
  {
    operating = false;
  }

  void PasteCells()
  {

    HashSet<CellWithValue> candidatesOld = new HashSet<CellWithValue>();
    HashSet<CellWithValue> candidatesNew = new HashSet<CellWithValue>();

    foreach (CellWithValue cv in operationCandidates)
    {
      Vector3 newPosition = GetCellCenter(cv.cell) + pasteOffset;
      Cell newCell = GetContainingCell(newPosition);
      candidatesNew.Add(new CellWithValue { cell = newCell, value = cv.value });
      candidatesOld.Add(new CellWithValue { cell = newCell, value = terrain.GetCellValue(newCell) });
    }

    undoStack.Push(new UndoStack.Item
    {
      actionLabel = $"Pasting blocks",
      getUnableToDoReason = () => null,
      getUnableToUndoReason = () => null,
      doIt = () => CreateCells(candidatesNew),
      undo = () => CreateCells(candidatesOld)
    });
  }
    void SetCopyPasteMode(CopyPasteMode newMode)
  {
    if (newMode == copyPasteMode) return;

    if (copyPasteMode == CopyPasteMode.Copy && operating)
    {
      EndCopy();
    }

    copyPasteMode = newMode;





        else if (mode == Mode.CopyPaste)
    {
      if (copyPasteMode == CopyPasteMode.Copy)
      {
        CopyCellUpdate(newpos);
      }
      else
      {
        PasteCellUpdate(newpos);
      }
    }
  }
    


    
  Cell lastCopiedCell;
  private void CopyCellUpdate(Vector3 newpos)
  {


    if (editMain.groundHitTransform == null || IsTransformGroundPlane(editMain.groundHitTransform))
    {
      previewTransform.gameObject.SetActive(false);

      return;
    }
    Vector3 hackOffset = Camera.main.transform.forward * 0.01f;
    Cell cell = GetContainingCell(newpos + hackOffset);

    previewTransform.gameObject.SetActive(true);
    previewTransform.position = GetCellCenter(cell);


    if (!operating) return;

    if (terrain.GetCellValue(cell).blockType == BlockShape.Empty)
      return;
    CellWithValue cv = new CellWithValue { cell = cell, value = terrain.GetCellValue(cell) };
    if (operationCandidates.Add(cv))
    {
      lastCopiedCell = cell;
      copyPasteTerrainPreview.SelectCell(cv);
    }
  }

  private void PasteCellUpdate(Vector3 newpos)
  {
    if (operationCandidates.Count == 0) return;

    Vector3 pos;
    Cell cell;


    validCellFound = terrain.SnapToEmptyCell(newpos, editMain.groundHitNormal, out cell, out pos);

    if (!validCellFound)
    {
      terrain.SnapToCell(newpos, out cell, out pos);
    }


    // terrain.SnapToCell(newpos, out cell, out pos);
    pasteOffset = pos - GetCellCenter(lastCopiedCell);
    copyPasteTerrainPreview.UpdateOffset(pasteOffset);
  }

  
  public override bool OnEscape()
  {
    if (mode == Mode.CopyPaste)
    {
      if (copyPasteMode == CopyPasteMode.Paste)
      {
        terrainToolSettings.SetCopyPasteMode(CopyPasteMode.Copy);
      }
      if (operationCandidates.Count > 0)
      {
        ClearCopy();
        return true;
      }
      else
      {
        return false;
      }
    }

    return false;
  }

     */
