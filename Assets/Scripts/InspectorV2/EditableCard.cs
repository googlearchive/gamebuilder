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

public class EditableCard : Card
{

  [SerializeField] TMPro.TMP_InputField nameInput;
  [SerializeField] TMPro.TMP_InputField descriptionInput;
  [SerializeField] TMPro.TMP_Dropdown categoryDropdown;
  [SerializeField] UnityEngine.UI.Button editIconButton;

  public event System.Action onChangesToCommit;

  private BehaviorSystem behaviorSystem;
  private VoosEngine voosEngine;
  private List<TMPro.TMP_Dropdown.OptionData> categoryOptions;

  protected override void Awake()
  {
    base.Awake();
  }

  public void Setup()
  {
    // Todo: Make Card class also use Setup
    Util.FindIfNotSet(this, ref behaviorSystem);
    Util.FindIfNotSet(this, ref voosEngine);

    nameInput.onEndEdit.AddListener((value) =>
    {
      if (card.GetTitle() != value)
      {
        cardUI.nameField.text = value;
        onChangesToCommit?.Invoke();
      }
    });

    descriptionInput.onEndEdit.AddListener((value) =>
    {
      if (card.GetDescription() != value)
      {
        cardUI.descriptionField.text = value;
        onChangesToCommit?.Invoke();
      }
    });

    categoryDropdown.onValueChanged.AddListener((value) =>
    {
      onChangesToCommit?.Invoke();
    });

    editIconButton.onClick.AddListener(OnEditIconButtonClicked);
    editIconButton.gameObject.SetActive(true);
  }

  public override void Populate(ICardModel card, bool withDetail = true)
  {
    if (card == null || !card.IsValid())
    {
      return;
    }
    base.Populate(card, withDetail);
    nameInput.text = card.GetTitle();
    descriptionInput.text = card.GetDescription();

    categoryDropdown.ClearOptions();
    categoryOptions = new List<TMPro.TMP_Dropdown.OptionData>();
    bool needsCustomCategory = true;
    foreach (string category in behaviorSystem.GetCategories())
    {
      if (category == BehaviorCards.CUSTOM_CATEGORY) needsCustomCategory = false;
      categoryOptions.Add(new TMPro.TMP_Dropdown.OptionData(category));
    }
    if (needsCustomCategory) categoryOptions.Add(new TMPro.TMP_Dropdown.OptionData(BehaviorCards.CUSTOM_CATEGORY));
    categoryDropdown.AddOptions(categoryOptions);

    // Hack: for now, assume that custom cards only have one category
    string cardCategory = new List<string>(card.GetCategories())[0];
    categoryDropdown.value = categoryOptions.FindIndex((option) => option.text == cardCategory);

    bool canEdit = !card.GetUnassignedBehaviorItem().IsBehaviorReadOnly();
    nameInput.interactable = canEdit;
    descriptionInput.interactable = canEdit;
    categoryDropdown.interactable = canEdit;
    editIconButton.gameObject.SetActive(canEdit);
  }

  protected override void Update()
  {
    base.Update();
    if (this.card == null) return;
    VoosActor user = voosEngine.FindOneActorUsing(this.card.GetUri());
    // Only allow changing the category if no one is using this card.
    categoryDropdown.gameObject.SetActive(user == null);
  }

  public void CommitChanges()
  {
    if (card.GetTitle() != nameInput.text)
    {
      card.SetTitle(nameInput.text);
    }
    if (card.GetDescription() != descriptionInput.text)
    {
      card.SetDescription(descriptionInput.text);
    }
    string cardCategory = new List<string>(card.GetCategories())[0];
    string newCardCategory = categoryOptions[categoryDropdown.value].text;
    if (newCardCategory != cardCategory)
    {
      card.SetCategories(new List<string> { newCardCategory });
    }
  }

  private void OnEditIconButtonClicked()
  {
    IconPickerDialog.Launch(pickedIcon =>
    {
      if (pickedIcon == null) return;
      card.SetImagePath("icon:" + pickedIcon);
      onChangesToCommit?.Invoke();
      ReloadCardImage();
    });
  }
}
