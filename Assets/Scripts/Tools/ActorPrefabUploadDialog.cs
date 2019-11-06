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
#if USE_STEAMWORKS
using Steamworks;
#endif

public class ActorPrefabUploadDialog : WorkshopUploadDialog
{
  [SerializeField] UnityEngine.UI.Button snapshotButton;

#if USE_STEAMWORKS
  private ActorPrefab actorPrefab;
  private SceneActorLibrary sceneActorLibrary;
  private SnapshotCamera snapshotCamera;

  public override void Setup()
  {
    base.Setup();
    Util.FindIfNotSet(this, ref sceneActorLibrary);
    Util.FindIfNotSet(this, ref snapshotCamera);
    snapshotButton.onClick.AddListener(() =>
    {
      thumbnailImage.texture = snapshotCamera.SnapshotGameView();
    });
  }

  public void Open(ActorPrefab actorPrefab, Util.Maybe<ulong> workshopId)
  {
    Open(workshopId);

    SceneActorLibrary.SavedActorPacks actorPacks = sceneActorLibrary.GetActorPacks();
    this.actorPrefab = actorPrefab;
    thumbnailImage.texture = actorPrefab.GetThumbnail();

    if (!workshopId.IsEmpty())
    {
      SceneActorLibrary.SavedActorPack existingPack = sceneActorLibrary.GetActorPack(workshopId.Get());
      nameField.text = existingPack.workshopName;
      descField.text = existingPack.workshopDesc;
    }
    else
    {
      nameField.text = actorPrefab.GetLabel();
      descField.text = actorPrefab.GetDescription();
    }
  }

  protected override void DoUpload(
    System.Action<Util.Maybe<ulong>> onComplete, System.Action<WorkshopAssetSource.GetUploadProgress> onStatus)
  {
    string name = nameField.text;
    string desc = descField.text;
    string contentDir = Util.CreateTempDirectory();
    sceneActorLibrary.WritePrefabToDir(actorPrefab.GetId(), contentDir);
    string thumbnailDir = Util.CreateTempDirectory();
    string thumbnailPath = Path.Combine(thumbnailDir, "thumbnail.png");
    Util.SaveTextureToPng((Texture2D)thumbnailImage.texture, thumbnailPath);
    PublishedFileId_t? maybePublishedId;
    if (workshopId.IsEmpty())
      maybePublishedId = null;
    else
    {
      PublishedFileId_t publishedFileId = new PublishedFileId_t(workshopId.Value);
      maybePublishedId = publishedFileId;
    }
    workshopAssetSource.Put(contentDir, name, desc, GameBuilder.SteamUtil.GameBuilderTags.Actor, thumbnailPath,
      maybePublishedId, onComplete, onStatus);
  }

  protected override void OnUploadSuccess(ulong result)
  {
    sceneActorLibrary.PutActors(result, nameField.text, descField.text, new List<string> { actorPrefab.GetId() });
  }
#endif
}
