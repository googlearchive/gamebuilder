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
using System.IO;
using UnityEngine;
using static TerrainManager;

public class TerrainToolSettings : Sidebar
{
  [SerializeField] TerrainToolSettingsUI settingsUI;
  [SerializeField] Sprite[] textureIcons;
  [SerializeField] ProgressBlockStyleUI progressItemPrefab;
  private UnityEngine.UI.Toggle[] textureToggles;

  public System.Action<TerrainTool.Mode> onModeChange;
  public System.Action<BlockShape> onShapeChange;
  public System.Action<BlockDirection> onDirectionChange;
  public System.Action onEditCopy;
  public System.Action onEditPaint;
  public System.Action onEditDelete;
  // public System.Action<TerrainTool.CopyPasteMode> onCopyPasteModeChange;

  Dictionary<string, Util.Tuple<ProgressBlockStyleUI, WorkshopAssetSource.GetUploadProgress>> importingListItems =
    new Dictionary<string, Util.Tuple<ProgressBlockStyleUI, WorkshopAssetSource.GetUploadProgress>>();

  private BlockTextureIcon[] icons = null;

  DynamicPopup popups;
  WorkshopAssetSource workshopAssetSource;
  BlockDirection blockDirection;
  TerrainRendering terrainRendering;
  TerrainManager terrainManager;
  EditMain editMain;
  UserMain userMain;
  InputControl inputControl;

  const int SHAPE_OFFSET = 1;

  public struct BlockTextureIcon
  {
    public Sprite sprite;
    public Color color;
    public TerrainManager.BlockStyle style;
  }

  public override void Setup(SidebarManager sidebarManager)
  {
    base.Setup(sidebarManager);

    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref terrainRendering);
    Util.FindIfNotSet(this, ref terrainManager);
    Util.FindIfNotSet(this, ref inputControl);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref workshopAssetSource);

    terrainManager.onCustomStyleTextureChange += RefreshTextureToggles;

    for (int i = 0; i < settingsUI.modeToggles.Length; i++)
    {
      int index = i;
      settingsUI.modeToggles[i].onValueChanged.AddListener((on) => { if (on) onModeChange?.Invoke((TerrainTool.Mode)index); });
    }

    for (int i = 0; i < settingsUI.shapeToggles.Length; i++)
    {
      int index = i + SHAPE_OFFSET; //+1 with constant because 0 is empty
      settingsUI.shapeToggles[i].onValueChanged.AddListener((on) => { if (on) onShapeChange?.Invoke((BlockShape)index); });
    }

    settingsUI.selectCopy.onClick.AddListener(() => onEditCopy?.Invoke());
    settingsUI.selectPaint.onClick.AddListener(() => onEditPaint?.Invoke());
    settingsUI.selectDelete.onClick.AddListener(() => onEditDelete?.Invoke());

    AddTooltipsToShapeToggles();
    UpdateBlockTextureIcons();
    LoadTextureToggles(icons);
    settingsUI.rotateButton.onClick.AddListener(NextBlockDirection);
    settingsUI.importStyleButton.onClick.AddListener(OnImportButtonClicked);

    textureToggles[(int)BlockStyle.Stone].isOn = true;

    settingsUI.assetPreImportDialog.Setup();
  }

  //todo: unhardcode
  private void AddTooltipsToShapeToggles()
  {
    ItemWithTooltipWithEventSystem full = settingsUI.shapeToggles[(int)BlockShape.Full - SHAPE_OFFSET].gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    full.SetupWithUserMain(userMain);
    full.SetDescription($"Full\n({inputControl.GetKeysForAction("ToolOption1")})");

    ItemWithTooltipWithEventSystem half = settingsUI.shapeToggles[(int)BlockShape.Half - SHAPE_OFFSET].gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    half.SetupWithUserMain(userMain);
    half.SetDescription($"Half\n({inputControl.GetKeysForAction("ToolOption2")})");

    ItemWithTooltipWithEventSystem ramp = settingsUI.shapeToggles[(int)BlockShape.Ramp - SHAPE_OFFSET].gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    ramp.SetupWithUserMain(userMain);
    ramp.SetDescription($"Ramp\n({inputControl.GetKeysForAction("ToolOption3")})");

    ItemWithTooltipWithEventSystem corner = settingsUI.shapeToggles[(int)BlockShape.Corner - SHAPE_OFFSET].gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    corner.SetupWithUserMain(userMain);
    corner.SetDescription($"Corner\n({inputControl.GetKeysForAction("ToolOption4")})");

    ItemWithTooltipWithEventSystem prev = settingsUI.toolModeText.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    prev.SetupWithUserMain(userMain);
    prev.SetDescription($"Cycle: ({inputControl.GetKeysForAction("PrevToolOption")}/{inputControl.GetKeysForAction("NextToolOption")})");

    ItemWithTooltipWithEventSystem next = settingsUI.styleHeaderText.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    next.SetupWithUserMain(userMain);
    next.SetDescription($"Cycle: ({inputControl.GetKeysForAction("PrevToolSecondaryOption")}/{inputControl.GetKeysForAction("NextToolSecondaryOption")})");

    ItemWithTooltipWithEventSystem copy = settingsUI.selectCopy.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    copy.SetupWithUserMain(userMain);
    copy.SetDescription("Copy selection");

    ItemWithTooltipWithEventSystem selectPaint = settingsUI.selectPaint.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    selectPaint.SetupWithUserMain(userMain);
    selectPaint.SetDescription("Change style of selection");

    ItemWithTooltipWithEventSystem selectDelete = settingsUI.selectDelete.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    selectDelete.SetupWithUserMain(userMain);
    selectDelete.SetDescription("Delete selection");
  }

  private void LoadTextureToggles(BlockTextureIcon[] textureIcons)
  {
    textureToggles = new UnityEngine.UI.Toggle[textureIcons.Length];
    int counter = 0;
    foreach (BlockTextureIcon texIcon in textureIcons)
    {
      ImageToggleUI texToggle = Instantiate(settingsUI.textureTogglePrefab, settingsUI.textureParent);
      ItemWithTooltipWithEventSystem tooltip = texToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
      tooltip.SetupWithUserMain(userMain);
      tooltip.SetDescription(texIcon.style.ToString());

      Destroy(texToggle.image.gameObject);
      texToggle.background.sprite = texIcon.sprite;
      texToggle.background.color = texIcon.color;

      // this is a hack to make sure the colors (which are listed first) show up at end of list
      texToggle.transform.SetAsFirstSibling();

      textureToggles[counter] = texToggle.GetComponent<UnityEngine.UI.Toggle>();
      textureToggles[counter].group = settingsUI.textureToggleGroup;
      textureToggles[counter].isOn = false;

      counter++;
    }
  }

  public void ClearTextureToggles()
  {
    for (int i = 0; i < textureToggles.Length; i++)
    {
      Destroy(textureToggles[i].gameObject);
    }
  }

  // TODO: put the uploaded textures here
  void UpdateBlockTextureIcons()
  {
    List<BlockTextureIcon> listToSend = new List<BlockTextureIcon>();
    for (int i = 0; i < NumTotalStyles; i++)
    {

      if (i < NumSolidColorStyles)
      {
        listToSend.Add(
           new BlockTextureIcon
           {
             sprite = null,
             color = terrainRendering.blockColors[i],
             style = (BlockStyle)i
           });
      }
      else
      {
        listToSend.Add(
           new BlockTextureIcon
           {
             sprite = textureIcons[i - NumSolidColorStyles],
             color = Color.white,
             style = (BlockStyle)i
           });
      }
    }

    // Add customs
    foreach (BlockStyle style in terrainManager.GetCustomStyles())
    {
      Sprite sprite = null;
      Texture2D tex = terrainManager.GetCustomStyleTexture(style);

      if (tex != null)
      {
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
      }
      var icon = new BlockTextureIcon
      {
        sprite = sprite,
        color = Color.white,
        style = style
      };
      listToSend.Add(icon);
    }
    icons = listToSend.ToArray();
  }

  // public void SetCopyPasteMode(TerrainTool.CopyPasteMode mode)
  // {
  //   settingsUI.pasteToggle.isOn = mode == TerrainTool.CopyPasteMode.Paste;
  //   settingsUI.copyToggle.isOn = mode == TerrainTool.CopyPasteMode.Copy;
  // }


  void ToggleBetweenCreateAndDeleteMode()
  {
    if (settingsUI.modeToggles[(int)TerrainTool.Mode.Create].isOn)
    {
      settingsUI.modeToggles[(int)TerrainTool.Mode.Dig].isOn = true;
    }
    else if (settingsUI.modeToggles[(int)TerrainTool.Mode.Dig].isOn)
    {
      settingsUI.modeToggles[(int)TerrainTool.Mode.Create].isOn = true;
    }
  }

  //various key shortcuts
  void Update()
  {
    if (!IsOpenedOrOpening()) return;

    if (!editMain.UserMainKeyLock())
    {
      InputUpdate();
      // if (GetMode() != TerrainTool.Mode.CopyPaste)
      // {

      // }
    }

    // settingsUI.copySettingsObject.SetActive(GetMode() == TerrainTool.Mode.CopyPaste);
    settingsUI.editSettingsObject.SetActive(GetMode() == TerrainTool.Mode.Edit);
    settingsUI.shapeSettingsObject.SetActive(GetMode() == TerrainTool.Mode.Create);
    settingsUI.styleSettingsObject.SetActive(GetMode() != TerrainTool.Mode.Dig);

    foreach (var entry in importingListItems)
    {
      float progress = entry.Value.second();
      entry.Value.first.SetProgress(progress);
    }
  }

  void InputUpdate()
  {
    if (settingsUI.shapeSettingsObject.activeSelf)
    {
      if (inputControl.GetButtonDown("Rotate"))
      {
        NextBlockDirection();
      }

      if (inputControl.GetButtonDown("ToolOption1"))
      {
        settingsUI.shapeToggles[0].isOn = true;
      }

      if (inputControl.GetButtonDown("ToolOption2"))
      {
        settingsUI.shapeToggles[1].isOn = true;
      }

      if (inputControl.GetButtonDown("ToolOption3"))
      {
        settingsUI.shapeToggles[2].isOn = true;
      }

      if (inputControl.GetButtonDown("ToolOption4"))
      {
        settingsUI.shapeToggles[3].isOn = true;
      }

    }

    if (settingsUI.styleSettingsObject.activeSelf)
    {
      if (inputControl.GetButtonDown("PrevToolSecondaryOption"))
      {
        PreviousBlockStyle();
      }

      if (inputControl.GetButtonDown("NextToolSecondaryOption"))
      {
        NextBlockStyle();
      }
    }


    if (inputControl.GetButtonDown("PrevToolOption"))
    {
      PreviousToolMode();
    }

    if (inputControl.GetButtonDown("NextToolOption"))
    {
      NextToolMode();
    }




    // //TODO: make this robust
    // if (settingsUI.modeToggles[0].isOn && inputControl.GetButtonDown("Snap"))
    // {
    //   settingsUI.modeToggles[2].isOn = true;
    // }

    // if (settingsUI.modeToggles[2].isOn && inputControl.GetButtonUp("Snap"))
    // {
    //   settingsUI.modeToggles[0].isOn = true;
    // }


    settingsUI.deleteStyleButton.gameObject.SetActive(CanDeleteCurrentBlockStyle());
  }

  private bool CanDeleteCurrentBlockStyle()
  {
    return false;
  }

  // private void NextBlockShape()
  // {
  //   int newval = (GetBlockShapeToggleIndex() + 1) % settingsUI.shapeToggles.Length;
  //   settingsUI.shapeToggles[newval].isOn = true;
  // }

  // private void PreviousBlockShape()
  // {
  //   int newval = GetBlockShapeToggleIndex() - 1;
  //   if (newval < 0) newval = settingsUI.shapeToggles.Length - 1;
  //   settingsUI.shapeToggles[newval].isOn = true;
  // }

  private void NextToolMode()
  {
    int newval = (GetModeIndex() + 1) % settingsUI.modeToggles.Length;
    settingsUI.modeToggles[newval].isOn = true;
  }

  private void PreviousToolMode()
  {
    int newval = GetModeIndex() - 1;
    if (newval < 0) newval = settingsUI.modeToggles.Length - 1;
    settingsUI.modeToggles[newval].isOn = true;
  }

  //index order is backwards on style

  private void PreviousBlockStyle()
  {
    int newval = (GetBlockStyleToggleIndex() + 1) % textureToggles.Length;
    textureToggles[newval].isOn = true;
  }

  private void NextBlockStyle()
  {
    int newval = GetBlockStyleToggleIndex() - 1;
    if (newval < 0) newval = textureToggles.Length - 1;
    textureToggles[newval].isOn = true;
  }

  int GetBlockShapeToggleIndex()
  {
    for (int i = 0; i < settingsUI.shapeToggles.Length; i++)
    {
      if (settingsUI.shapeToggles[i].isOn)
      {
        return i;
      }
    }

    return 0;
  }

  int GetBlockStyleToggleIndex()
  {
    for (int i = 0; i < textureToggles.Length; i++)
    {
      if (textureToggles[i].isOn)
      {
        return i;
      }
    }

    return 0;
  }

  public int GetModeIndex()
  {
    for (int i = 0; i < settingsUI.modeToggles.Length; i++)
    {
      if (settingsUI.modeToggles[i].isOn) return i;
    }
    return 0;
  }

  public void NextBlockDirection()
  {
    int numDirs = Util.CountEnumValues<TerrainManager.BlockDirection>();
    blockDirection = (TerrainManager.BlockDirection)(((int)blockDirection + 1) % numDirs);
    onDirectionChange?.Invoke(blockDirection);
  }

  public TerrainTool.Mode GetMode()
  {
    for (int i = 0; i < settingsUI.modeToggles.Length; i++)
    {
      if (settingsUI.modeToggles[i].isOn) return (TerrainTool.Mode)i;
    }
    return TerrainTool.Mode.Create;
  }




  public BlockShape GetBlockShape()
  {
    for (int i = 0; i < settingsUI.shapeToggles.Length; i++)
    {
      if (settingsUI.shapeToggles[i].isOn) return (BlockShape)(i + SHAPE_OFFSET);
    }
    return BlockShape.Half;
  }

  public BlockDirection GetBlockDirection()
  {
    return blockDirection;
  }

  public BlockStyle GetBlockStyle()
  {
    int selectedIcon = GetBlockStyleToggleIndex();
    return icons[selectedIcon].style;
  }

  public override void Close()
  {
    base.Close();
    settingsUI.assetPreImportDialog.Close();
  }


  void OnImportButtonClicked()
  {
#if USE_FILEBROWSER
    Crosstales.FB.ExtensionFilter[] filters = new Crosstales.FB.ExtensionFilter[] {
      new Crosstales.FB.ExtensionFilter("PNG files", "png"),
      new Crosstales.FB.ExtensionFilter("JPG files", "jpg"),
    };
    string selected = Crosstales.FB.FileBrowser.OpenSingleFile("Import image", "", filters);
    if (selected != null)
    {
      OnImportFileSelected(new string[] { selected });
    }
#else
    popups.ShowTextInput("Enter the full path to a PNG (such as C:\\my\\textures\\foo.png):", "", path =>
    {
      if (!path.IsNullOrEmpty() && File.Exists(path))
      {
        OnImportFileSelected(new string[] { path });
      }
    });
#endif
  }

  bool IsExtensionUsable(string ext)
  {
    return ext == ".jpg" || ext == ".png";
  }

  public void OnImportFileSelected(string[] selections)
  {
    if (selections == null || selections.Length == 0)
    {
      // Canceled.
      return;
    }
    string fullPath = selections[0];
    string baseName = Path.GetFileNameWithoutExtension(fullPath);
    string ext = Path.GetExtension(fullPath).ToLowerInvariant();
    if (!IsExtensionUsable(ext))
    {
      // Should not happen.
      Debug.LogError("Invalid extension for image file: " + ext);
      return;
    }

    settingsUI.assetPreImportDialog.Close();
    settingsUI.assetPreImportDialog.Open(baseName, (proceed, name) => OnPreImportClosed(proceed, name, fullPath));
  }

  void OnPreImportClosed(bool proceed, string name, string fullPath)
  {
    if (!proceed)
    {
      return;
    }
    name = (name == null || name.Trim().Length == 0) ? "Untitled Image" : name;
    string ext = Path.GetExtension(fullPath).ToLowerInvariant();
    string tempDir = Util.CreateTempDirectory();
    File.Copy(fullPath, Path.Combine(tempDir, "image" + ext));

    string id = System.Guid.NewGuid().ToString();
    ProgressBlockStyleUI listItem = Instantiate(progressItemPrefab, settingsUI.textureParent);
    listItem.transform.SetAsFirstSibling();
    listItem.name = id;
    listItem.SetThumbnail(Util.ReadPngToTexture(fullPath));
    listItem.gameObject.SetActive(true);
    importingListItems[id] = new Util.Tuple<ProgressBlockStyleUI, WorkshopAssetSource.GetUploadProgress>(
      listItem,
      () => { return 0; });

    workshopAssetSource.Put(
      tempDir, name, name, GameBuilder.SteamUtil.GameBuilderTags.BlockStyle,
      null, null, result => OnUploadComplete(name, result, id), (getProgress) =>
      {
        importingListItems[id].second = getProgress;
      });

    settingsUI.scrollRect.verticalNormalizedPosition = 1;
  }

  void OnUploadComplete(string name, Util.Maybe<ulong> maybeId, string id)
  {
    if (maybeId.IsEmpty())
    {
      popups.Show("Error uploading file to Steam Workshop:\n" + maybeId.GetErrorMessage(), "OK", () => { });
      DestroyImportFeedback(id);
      return;
    }

    terrainManager.AddCustomStyle(maybeId.Value, () => DestroyImportFeedback(id));
  }

  void DestroyImportFeedback(string id)
  {
    Util.Tuple<ProgressBlockStyleUI, WorkshopAssetSource.GetUploadProgress> tuple = importingListItems[id];
    importingListItems.Remove(id);
    Destroy(tuple.first.gameObject);
  }

  void RefreshTextureToggles()
  {
    BlockStyle currentBlockStyle = GetBlockStyle();
    ClearTextureToggles();
    UpdateBlockTextureIcons();
    LoadTextureToggles(icons);
    SetBlockStyle(currentBlockStyle);
  }

  private void SetBlockStyle(BlockStyle currentBlockStyle)
  {
    int index = (int)currentBlockStyle;
    // Find the icon corresponding to this..
    for (int i = 0; i < icons.Length; i++)
    {
      if (icons[i].style == currentBlockStyle)
      {
        textureToggles[i].isOn = true;
        break;
      }
    }
  }

  public void SetStatusText(string text)
  {
    settingsUI.statusText.text = text;
  }
}
