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
using static TerrainManager;

public class TerrainTool : Tool
{
  [SerializeField] Transform previewTransform;
  [SerializeField] EditTerrainTool editTerrainTool;
  [SerializeField] CreateTerrainPreview createTerrainPreview;
  [SerializeField] DeleteTerrainPreview deleteTerrainPreview;
  [SerializeField] PaintTerrainPreview paintTerrainPreview;

  [SerializeField] CreateToolRay createToolRay;
  [SerializeField] GameObject rayContainerObject;//so i can disable the effect in first and 3rd person

  TerrainToolSettings terrainToolSettings;
  TerrainManager terrain;
  TerrainRendering terrainRendering;
  UndoStack undoStack;

  BlockDirection blockDirection;
  bool validCellFound = false;

  internal bool operating = false;
  internal HashSet<Util.Tuple<Cell, CellValue>> operationCandidates = new HashSet<Util.Tuple<Cell, CellValue>>();

  // public struct Util.Tuple<Cell,CellValue>
  // {
  //   public Cell cell;
  //   public CellValue value;
  // }

  public enum Mode
  {

    Create,
    Dig,
    Paint,
    Edit,
    None
  }

  Mode mode = Mode.None;

  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);
    Util.FindIfNotSet(this, ref terrain);
    Util.FindIfNotSet(this, ref terrainRendering);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref createTerrainPreview);

    editTerrainTool.Setup();

    terrainToolSettings = editMain.GetTerrainSidebar();
    terrainToolSettings.onModeChange = SetMode;
    terrainToolSettings.onShapeChange = SetShape;
    terrainToolSettings.onDirectionChange = SetDirection;

    terrainToolSettings.onEditCopy = editTerrainTool.CopySelection;
    terrainToolSettings.onEditPaint = editTerrainTool.PaintSelection;
    terrainToolSettings.onEditDelete = editTerrainTool.DeleteSelection;

    terrainToolSettings.RequestOpen();
    SetMode(terrainToolSettings.GetMode());
    SetShape(terrainToolSettings.GetBlockShape());
    SetDirection(terrainToolSettings.GetBlockDirection());

    createTerrainPreview.SetTint(editMain.GetAvatarTint());
    createToolRay.SetLocalRayOriginTransform(emissionAnchor);
    createToolRay.SetTint(editMain.GetAvatarTint());
    editMain.SetCameraFollowingActor(false);
    editMain.TryEscapeOutOfCameraView();

    RefreshPreviewVisibility();
  }

  private void RefreshPreviewVisibility()
  {
    createTerrainPreview.SetVisibility(mode == Mode.Create);
    deleteTerrainPreview.SetVisibility(mode == Mode.Dig);
    paintTerrainPreview.SetVisibility(mode == Mode.Paint);
    editTerrainTool.SetPreviewVisibility(mode == Mode.Edit);

  }

  public override void Close()
  {
    createTerrainPreview.SetVisibility(false);
    terrainToolSettings.RequestClose();
    if (this.mode == Mode.Edit) editTerrainTool.Close();

    base.Close();

  }

  public override bool ShowHoverTargetFeedback()
  {
    return false;
  }

  public override bool ShowSelectedTargetFeedback()
  {
    return false;
  }

  public override bool CanEditTargetActors()
  {
    return false;
  }

  private void SetMode(Mode mode)
  {
    if (this.mode == mode) return;

    if (operating)
    {
      if (this.mode == Mode.Create) EndCreate();
      if (this.mode == Mode.Paint) EndPaint();
      if (this.mode == Mode.Dig) EndDig();
    }

    if (this.mode == Mode.Edit) editTerrainTool.Close();

    this.mode = mode;

    RefreshPreviewVisibility();
  }

  internal void RefreshCandidates()
  {
    HashSet<Util.Tuple<Cell, CellValue>> refreshed = new HashSet<Util.Tuple<Cell, CellValue>>();
    foreach (Util.Tuple<Cell, CellValue> cv in operationCandidates)
    {
      refreshed.Add(new Util.Tuple<Cell, CellValue>(cv.first, terrain.GetCellValue(cv.first)));
    }

    operationCandidates = refreshed;
  }

  private void SetShape(BlockShape blockShape)
  {
    createTerrainPreview.UpdatePreviewBlock(blockShape, terrainToolSettings.GetBlockDirection());
  }

  private void SetDirection(BlockDirection blockDirection)
  {
    createTerrainPreview.UpdatePreviewDirection(blockDirection);
  }


  public override bool Trigger(bool on)
  {
    base.Trigger(on);

    if (mode == Mode.Edit)
    {
      editTerrainTool.Trigger(on);
    }

    if (on == operating) return true;

    if (mode == Mode.Create)
    {
      if (on) StartCreate();
      else EndCreate();
    }
    else if (mode == Mode.Paint)
    {
      if (on) StartPaint();
      else EndPaint();
    }
    else if (mode == Mode.Dig)
    {
      if (on) StartDig();
      else EndDig();
    }
    else
    {
      if (on)
      {
        if (GetCurrentTargetCell(out Cell cell))
        {

          editTerrainTool.StartEditSelect(cell);
        }
      }
      if (!on) editTerrainTool.EndEditSelect();
    }

    return true;
  }



  private void StartPaint()
  {
    operating = true;
  }

  private void EndPaint()
  {
    operating = false;

    HashSet<Util.Tuple<Cell, CellValue>> copyOfCandidates = new HashSet<Util.Tuple<Cell, CellValue>>(operationCandidates);
    BlockStyle blockStyle = terrainToolSettings.GetBlockStyle();

    undoStack.Push(new UndoStack.Item
    {
      actionLabel = $"Creating blocks",
      getUnableToDoReason = () => null,
      getUnableToUndoReason = () => null,
      doIt = () => UpdateCellsStyle(copyOfCandidates, blockStyle),
      undo = () => CreateCells(copyOfCandidates),
    }, false);

    operationCandidates.Clear();
  }

  void StartDig()
  {
    operating = true;
  }


  void EndDig()
  {
    operating = false;
    DeleteOperationCandidates();
    deleteTerrainPreview.ClearPreview();
  }

  internal void DeleteOperationCandidates()
  {
    HashSet<Util.Tuple<Cell, CellValue>> copyOfCandidates = new HashSet<Util.Tuple<Cell, CellValue>>(operationCandidates);

    undoStack.Push(new UndoStack.Item
    {
      actionLabel = $"Creating blocks",
      getUnableToDoReason = () => null,
      getUnableToUndoReason = () => null,
      doIt = () => CreateCellsWithOneValue(copyOfCandidates, new CellValue()),
      undo = () => CreateCells(copyOfCandidates)
    });

    operationCandidates.Clear();
  }

  void StartCreate()
  {
    operating = true;
  }

  void EndCreate()
  {
    operating = false;

    HashSet<Util.Tuple<Cell, CellValue>> copyOfCandidates = new HashSet<Util.Tuple<Cell, CellValue>>(operationCandidates);

    TerrainManager.CellValue cellValue = new TerrainManager.CellValue(
       terrainToolSettings.GetBlockShape(),
       terrainToolSettings.GetBlockDirection(),
       terrainToolSettings.GetBlockStyle());


    undoStack.Push(new UndoStack.Item
    {
      actionLabel = $"Creating blocks",
      getUnableToDoReason = () => null,
      getUnableToUndoReason = () => null,
      doIt = () => CreateCellsWithOneValue(copyOfCandidates, cellValue),
      undo = () => CreateCells(copyOfCandidates)
    });

    createTerrainPreview.ClearCreationPreview();
    operationCandidates.Clear();
  }

  void CreateCellsWithOneValue(IEnumerable<Util.Tuple<Cell, CellValue>> cells, TerrainManager.CellValue cellValue)
  {
    foreach (Util.Tuple<Cell, CellValue> cv in cells)
    {
      terrain.SetCellValue(cv.first, cellValue);
    }
  }

  void UpdateCellsStyle(IEnumerable<Util.Tuple<Cell, CellValue>> cells, BlockStyle blockStyle)
  {
    HashSet<Util.Tuple<Cell, CellValue>> updatedCells = new HashSet<Util.Tuple<Cell, CellValue>>();
    foreach (Util.Tuple<Cell, CellValue> cv in cells)
    {
      CellValue value = terrain.GetCellValue(cv.first);
      value.style = blockStyle;
      terrain.SetCellValue(cv.first, value);
      updatedCells.Add(new Util.Tuple<Cell, CellValue>(cv.first, value));
    }

    operationCandidates = updatedCells;
  }

  void CreateCells(IEnumerable<Util.Tuple<Cell, CellValue>> cells)
  {
    foreach (Util.Tuple<Cell, CellValue> cv in cells)
    {
      terrain.SetCellValue(cv.first, cv.second);
    }
  }

  bool GetCurrentTargetCell(out Cell cell)
  {
    if (IsRayHittingGround())
    {
      cell = GetCellForRayHit(targetPosition, editMain.groundHitNormal);
      return true;
    }
    cell = new Cell(0, 0, 0);
    return false;
  }

  public override void UpdatePosition(Vector3 newpos)
  {
    base.UpdatePosition(newpos);

    if (mode != Mode.Create)
    {
      rayContainerObject.SetActive(false);
    }

    if (mode == Mode.Create)
    {
      CreateCellUpdate(newpos);
    }
    else if (mode == Mode.Paint)
    {
      PaintCellUpdate(newpos);
    }

    else if (mode == Mode.Dig)
    {
      DigCellUpdate(newpos);
    }
    else
    {
      editTerrainTool.UpdatePosition(newpos);
      if (IsRayHittingGround())
      {
        Cell cell = GetCellForRayHit(newpos, editMain.groundHitNormal);
        previewTransform.gameObject.SetActive(true);
        previewTransform.position = GetCellCenter(cell);

        if (operating)
        {
          editTerrainTool.EditSelectUpdate(cell);
        }
      }
      else
      {
        previewTransform.gameObject.SetActive(false);

      }
    }
  }

  private bool IsRayHittingGround()
  {
    return editMain.groundHitTransform != null && !IsTransformGroundPlane(editMain.groundHitTransform);
  }



  private void CreateCellUpdate(Vector3 newpos)
  {
    Cell cell;
    validCellFound = terrain.SnapToEmptyCell(newpos, editMain.groundHitNormal, out cell, out targetPosition);

    if (validCellFound != previewTransform.gameObject.activeSelf)
    {
      previewTransform.gameObject.SetActive(validCellFound);
    }

    rayContainerObject.SetActive(!editMain.Using3DCamera() && validCellFound);
    if (validCellFound)
    {
      createToolRay.UpdateRayWithObject(previewTransform.gameObject);
      previewTransform.position = targetPosition;
      createTerrainPreview.UpdatePreviewCell(cell);
    }

    if (!operating) return;

    if (operationCandidates.Add(new Util.Tuple<Cell, CellValue>(cell, terrain.GetCellValue(cell))))
    {
      createTerrainPreview.AddCellToCreationPreview(cell);
    }
  }

  private void DigCellUpdate(Vector3 newpos)
  {
    if (editMain.groundHitTransform == null || IsTransformGroundPlane(editMain.groundHitTransform))
    {
      previewTransform.gameObject.SetActive(false);
      return;
    }


    previewTransform.gameObject.SetActive(!operating);
    Cell cell = GetCellForRayHit(newpos, editMain.groundHitNormal);
    previewTransform.position = GetCellCenter(cell);

    if (!operating) return;

    // Cell cell = GetCellForRayHit(newpos, editMain.groundHitNormal);
    if (operationCandidates.Add(new Util.Tuple<Cell, CellValue>(cell, terrain.GetCellValue(cell))))
    {
      deleteTerrainPreview.AddCellToPreview(cell);
    }
  }


  private void PaintCellUpdate(Vector3 newpos)
  {
    if (editMain.groundHitTransform == null || IsTransformGroundPlane(editMain.groundHitTransform))
    {
      previewTransform.gameObject.SetActive(false);
      return;
    }

    previewTransform.gameObject.SetActive(true);
    // previewTransform.position = editMain.groundHitTransform.position;

    Vector3 hackOffset = Camera.main.transform.forward * 0.01f;
    Cell cell = GetContainingCell(newpos + hackOffset);

    previewTransform.position = GetCellCenter(cell);

    if (!operating) return;
    CellValue cellValue = terrain.GetCellValue(cell);

    if (cellValue.style != terrainToolSettings.GetBlockStyle())
    {
      if (operationCandidates.Add(new Util.Tuple<Cell, CellValue>(cell, cellValue)))
      {
        cellValue = terrain.GetCellValue(cell);
        cellValue.style = terrainToolSettings.GetBlockStyle();
        terrain.SetCellValue(cell, cellValue);
      }
    }
  }

  void Update()
  {
    if (!editMain.KeyLock() && mode == Mode.Edit)
    {
      editTerrainTool.ToolUpdate();
    }
    // if (!editMain.KeyLock() && mode == Mode.CopyPaste && copyPasteMode == CopyPasteMode.Copy && inputControl.GetButtonDown("Submit"))
    // {
    //   terrainToolSettings.SetCopyPasteMode(CopyPasteMode.Paste);
    // }
    UpdateStatusText();
  }

  private void UpdateStatusText()
  {
    Cell cell;
    string status;
    if (GetCurrentTargetCell(out cell))
    {
      status = $"Block ({cell.x}, {cell.y}, {cell.z})";
    }
    else
    {
      status = "Ready.";
    }
    terrainToolSettings.SetStatusText(status);
  }

  internal void PaintOperationCandidates()
  {
    HashSet<Util.Tuple<Cell, CellValue>> copyOfCandidates = new HashSet<Util.Tuple<Cell, CellValue>>(operationCandidates);
    BlockStyle blockStyle = terrainToolSettings.GetBlockStyle();

    undoStack.Push(new UndoStack.Item
    {
      actionLabel = $"Painting blocks",
      getUnableToDoReason = () => null,
      getUnableToUndoReason = () => null,
      doIt = () => UpdateCellsStyle(copyOfCandidates, blockStyle),
      undo = () => CreateCells(copyOfCandidates),
    });

  }

  internal void PasteOperationCandidates(Vector3 offset)
  {

    HashSet<Util.Tuple<Cell, CellValue>> candidatesOld = new HashSet<Util.Tuple<Cell, CellValue>>();
    HashSet<Util.Tuple<Cell, CellValue>> candidatesNew = new HashSet<Util.Tuple<Cell, CellValue>>();

    foreach (Util.Tuple<Cell, CellValue> cv in operationCandidates)
    {
      Vector3 newPosition = GetCellCenter(cv.first) + offset;
      Cell newCell = GetContainingCell(newPosition);
      candidatesNew.Add(new Util.Tuple<Cell, CellValue>(newCell, cv.second));
      candidatesOld.Add(new Util.Tuple<Cell, CellValue>(newCell, terrain.GetCellValue(newCell)));
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

  public override bool OnEscape()
  {
    if (mode == Mode.Edit)
    {
      return editTerrainTool.OnEscape();
    }
    return false;
  }

  public static bool IsTransformGroundPlane(Transform _transform)
  {
    return _transform.GetComponent<GrassSpawn>() != null;
  }

  public override bool TargetsActors()
  {
    return true;
  }

  public override bool TargetsGround()
  {
    return true;
  }

  public override bool TargetsSpace()
  {
    return true;
  }

  public override string GetName()
  {
    return "Terrain";
  }

  public override bool IsSelectionLocked()
  {
    return false;
  }

  public override bool GetToolEffectActive()
  {
    return mode == Mode.Create;
  }

  public override string GetToolEffectName()
  {
    return "CreateToolRayEffect";
  }
}
