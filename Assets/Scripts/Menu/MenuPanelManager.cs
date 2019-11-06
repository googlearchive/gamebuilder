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
using System;

public class MenuPanelManager : MonoBehaviour
{
  [SerializeField] GameObject gameLibraryPrefab;
  [SerializeField] GameObject steamWorkshopPrefab;
  [SerializeField] GameObject multiplayerPrefab;
  [SerializeField] RectTransform menuPanelParent;

  MultiplayerWarning multiplayerWarning;
  GameLibraryMenu gameLibraryMenu;
  WorkshopMenu workshopMenu;
  MultiplayerMenu multiplayerMenu;

  List<IMenuPanelInterface> openPanels = new List<IMenuPanelInterface>();

  public void Setup()
  {
    SetupGameLibrary();
    SetupWorkshopLibrary();
    SetupMultiplayerMenu();
    Util.FindIfNotSet(this, ref multiplayerWarning);
  }

  public void SetLibraryHeaderText(string newtext)
  {
    gameLibraryMenu.SetHeaderText(newtext);
  }

  //returns true if there was something actually to go back on
  public bool Back()
  {
    if (openPanels.Count > 0)
    {
      openPanels.Last().Close();
      return true;
    }
    return false;
  }

  void SetupGameLibrary()
  {
    gameLibraryMenu = Instantiate(gameLibraryPrefab, menuPanelParent).GetComponent<GameLibraryMenu>();
    gameLibraryMenu.Setup();
    gameLibraryMenu.openWorkshop = OpenSteamWorkshop;
    gameLibraryMenu.SetCloseEvent(CloseGameLibrary);
  }

  public void OpenGameLibrary()
  {
    gameLibraryMenu.Open();
    openPanels.Add(gameLibraryMenu);
  }

  public void CloseGameLibrary()
  {
    openPanels.Remove(gameLibraryMenu);
  }

  public bool IsOpen()
  {
    return gameLibraryMenu.IsOpen();
  }

  void SetupMultiplayerMenu()
  {
    multiplayerMenu = Instantiate(multiplayerPrefab, menuPanelParent).GetComponent<MultiplayerMenu>();
    multiplayerMenu.gameObject.SetActive(false);
    multiplayerMenu.Setup();
    multiplayerMenu.SetCloseEvent(CloseMultiplayerMenu);
  }

  public void OpenMultiplayerMenu()
  {
    multiplayerWarning.Open(() =>
    {
      multiplayerWarning.Close();
      multiplayerMenu.Open();
      openPanels.Add(multiplayerMenu);
    });
  }

  public void CloseMultiplayerMenu()
  {
    openPanels.Remove(multiplayerMenu);
  }


  void SetupWorkshopLibrary()
  {
    workshopMenu = Instantiate(steamWorkshopPrefab, menuPanelParent).GetComponent<WorkshopMenu>();
    workshopMenu.Setup();
    workshopMenu.SetCloseEvent(CloseSteamWorkshop);
  }

  public void OpenSteamWorkshop()
  {
    workshopMenu.Open();
    openPanels.Add(workshopMenu);
  }

  void CloseSteamWorkshop()
  {
    if (gameLibraryMenu.IsOpen())
    {
      gameLibraryMenu.Refresh();
    }
    openPanels.Remove(workshopMenu);
  }

  public void SetOpen(bool on)
  {
    if (on) OpenGameLibrary();
    else Back();
  }
}

public interface IMenuPanelInterface
{
  void SetCloseEvent(System.Action action);

  void Open();
  void Close();
}
