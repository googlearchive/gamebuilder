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

using UnityEngine;
using Behaviors;
using System;

public partial class BehaviorCards
{
  [System.Serializable]
  struct BrainMetadata
  {
    public PanelManager.ViewState panelViewState;
    public PanelNote.Data[] panelNotes;
    public CardPanel.PanelUse miscPanelUseMetadata;
  }

  // The metadata JSON in the behavior entry for panels should match this.
  [System.Serializable]
  public struct PanelMetadata
  {
    [System.Serializable]
    public struct Data
    {
      public bool isPanel;
      public string title;
      public Color color;
      public string description;
      public string iconResourcePath;
      public bool hidden;
    }

    public Data cardSystemPanelData;

    public static PanelMetadata.Data Get(Behavior data)
    {
      return data.metadataJson.IsNullOrEmpty() ? new PanelMetadata.Data() :
        JsonUtility.FromJson<PanelMetadata>(data.metadataJson).cardSystemPanelData;
    }
  }

  // The metadata JSON in the behavior entry for cards should match this.
  [System.Serializable]
  public struct CardMetadata
  {
    [System.Serializable]
    public struct Data
    {
      public bool isCard;
      public string title;
      public string description;
      public string[] categories;
      public string imageResourcePath;
      public int listPriority;
      public bool hidden;

      // If present, this is the behavior ID that this card should have.
      // This is currently only enforced if this card is hot-loaded from disk via BehaviorHotLoader.
      public string userProvidedId;
    }

    public Data cardSystemCardData;

    public static CardMetadata DefaultCardMetadata = new CardMetadata
    {
      cardSystemCardData = new CardMetadata.Data
      {
        isCard = true,
        title = "New card",
        description = "An empty card.\nClick on it and select 'Edit JavaScript' to customize.",
        categories = new string[0],
        imageResourcePath = null,
        listPriority = 0,
      }
    };

    public static string[] GetEffectiveCardCategories(Behavior data)
    {
      if (data.metadataJson == null || data.metadataJson.Length == 0)
      {
        // Legacy cards just go in the misc category
        return CUSTOM_CATEGORIES;
      }
      else
      {
        CardMetadata.Data md = GetMetaDataFor(data);
        if (md.isCard)
        {
          return md.categories;
        }
        else
        {
          // Probably a panel.
          return null;
        }
      }
    }

    public static bool IsCardOfCategory(Behavior data, string category)
    {
      string[] cats = GetEffectiveCardCategories(data);
      if (cats == null)
      {
        // Not even a card
        return false;
      }
      else
      {
        return Array.IndexOf(cats, category) != -1;
      }
    }

    public static CardMetadata.Data GetMetaDataFor(Behavior data)
    {
      return data.metadataJson == null || data.metadataJson == "" ? DefaultCardMetadata.cardSystemCardData :
        JsonUtility.FromJson<CardMetadata>(data.metadataJson).cardSystemCardData;
    }
  }

  public static bool IsCard(Behavior b)
  {
    return !b.metadataJson.IsNullOrEmpty()
        && JsonUtility.FromJson<CardMetadata>(b.metadataJson).cardSystemCardData.isCard;
  }

  public static bool IsPanel(Behavior b)
  {
    return !b.metadataJson.IsNullOrEmpty()
        && JsonUtility.FromJson<PanelMetadata>(b.metadataJson).cardSystemPanelData.isPanel;
  }
}