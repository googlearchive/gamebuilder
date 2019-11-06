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

public class CardPackUploadDialog : WorkshopUploadDialog
{
  private IEnumerable<string> pack;
  private BehaviorSystem behaviorSystem;

#if USE_STEAMWORKS
  public override void Setup()
  {
    base.Setup();
    Util.FindIfNotSet(this, ref behaviorSystem);
  }

  public void Open(IEnumerable<string> pack, Util.Maybe<ulong> workshopId)
  {
    Open(workshopId);
    this.pack = pack;
    if (!workshopId.IsEmpty())
    {
      BehaviorSystem.SavedCardPack existingPack = behaviorSystem.GetCardPack(workshopId.Get());
      nameField.text = existingPack.workshopName;
      descField.text = existingPack.workshopDesc;
    }
  }

  protected override void DoUpload(
    System.Action<Util.Maybe<ulong>> onComplete, System.Action<WorkshopAssetSource.GetUploadProgress> onStatus)
  {
    string name = nameField.text;
    string desc = descField.text;
    string tempDir = Util.CreateTempDirectory();
    behaviorSystem.WriteEmbeddedBehaviorsToDirectory(pack, tempDir);

    if (workshopId.IsEmpty())
    {
      workshopAssetSource.Put(tempDir, name, desc, GameBuilder.SteamUtil.GameBuilderTags.CardPack, null, null,
        onComplete, onStatus);
    }
    else
    {
      PublishedFileId_t publishedFileId = new PublishedFileId_t(workshopId.Value);
      workshopAssetSource.Put(tempDir, name, desc, GameBuilder.SteamUtil.GameBuilderTags.CardPack, null,
        publishedFileId,
        onComplete,
        onStatus);
    }
  }

  protected override void OnUploadSuccess(ulong result)
  {
    behaviorSystem.PutCardPack(result, nameField.text, descField.text, pack);
  }
#endif
}
