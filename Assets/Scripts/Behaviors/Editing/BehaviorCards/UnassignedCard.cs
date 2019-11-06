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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Behaviors;
using System.IO;
using BehaviorProperties;

public partial class BehaviorCards
{
  public class UnassignedCard : ICardModel
  {
    static string placeHolderImageResPath = "BuiltinAssets/CardImages/placeholder";

    readonly UnassignedBehavior item;

    private string cachedBuiltinDescription = null;

    public UnassignedCard(UnassignedBehavior item) { this.item = item; }

    public CardMetadata.Data GetMetadata()
    {
      if (!item.GetMetadataJson().IsNullOrEmpty())
      {
        var existing = JsonUtility.FromJson<CardMetadata>(item.GetMetadataJson());
        if (existing.cardSystemCardData.isCard)
        {
          return existing.cardSystemCardData;
        }
      }
      // Create MD structure using the legacy fields Consider them custom cards.
      // NOTE: This will only happen for user embedded legacy behaviors, since
      // builtin legacy ones get filtered out before hand.
      var md = new CardMetadata.Data();
      md.categories = BehaviorCards.CUSTOM_CATEGORIES;
      md.description = item.GetDescription();
      md.imageResourcePath = placeHolderImageResPath;
      md.isCard = false;
      md.listPriority = 0;
      md.title = item.GetName();
      return md;
    }
    public ICollection<string> GetCategories()
    {
      return GetMetadata().categories;
    }
    public string GetDescription()
    {
      if (IsBuiltin())
      {
        // Optimization.
        if (cachedBuiltinDescription == null)
        {
          cachedBuiltinDescription = GetMetadata().description;
        }
        return cachedBuiltinDescription;
      }
      // Don't cache embedded, since those may change and there are less of them.
      return GetMetadata().description;
    }
    public string GetTitle()
    {
      return (item.IsLegacyBuiltin() ? "(DEPRECATED) " : "") + GetMetadata().title;
    }
    public UnassignedBehavior GetUnassignedBehaviorItem() { return item; }

    public string GetImagePath()
    {
      return GetMetadata().imageResourcePath;
    }

    public Sprite GetImage()
    {
      return Resources.Load(GetImagePath(), typeof(Sprite)) as Sprite;
    }

    public string GetId()
    {
      return item.GetId();
    }

    void SetMetadata(CardMetadata.Data data)
    {
      string metadataJson = item.GetMetadataJson();
      var existing = metadataJson != null && metadataJson != "" ?
      JsonUtility.FromJson<CardMetadata>(metadataJson) : CardMetadata.DefaultCardMetadata;
      existing.cardSystemCardData = data;
      item.SetMetadataJson(JsonUtility.ToJson(existing));
    }

    public void SetTitle(string title)
    {
      CardMetadata.Data data = GetMetadata();
      data.title = title;
      SetMetadata(data);
    }

    public void SetDescription(string description)
    {
      CardMetadata.Data data = GetMetadata();
      data.description = description;
      SetMetadata(data);
    }

    public void SetImagePath(string imagePath)
    {
      CardMetadata.Data data = GetMetadata();
      data.imageResourcePath = imagePath;
      SetMetadata(data);
    }

    public void SetCategories(ICollection<string> categories)
    {
      CardMetadata.Data metadata = GetMetadata();
      string[] categoriesArr = new string[categories.Count];
      categories.CopyTo(categoriesArr, 0);
      metadata.categories = categoriesArr;
      SetMetadata(metadata);
    }

    public string GetUri()
    {
      return this.item.GetBehaviorUri();
    }

    public bool IsBuiltin()
    {
      return BehaviorSystem.IsBuiltinBehaviorUri(this.item.GetBehaviorUri());
    }

    public PropEditor[] GetDefaultProperties()
    {
      return item.GetDefaultProperties();
    }

    public ICardModel MakeCopy()
    {
      UnassignedCard unassignedCard = new UnassignedCard(item.MakeCopy());
      unassignedCard.SetTitle("Copy of " + unassignedCard.GetTitle());
      return unassignedCard;
    }

    public bool IsValid()
    {
      return item.IsValid();
    }
  }

}