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

public partial class BehaviorCards
{
  interface Unassigner
  {
    void Unassign(CardAssignment card);
  }

  class CardAssignment : ICardAssignmentModel
  {
    readonly AssignedBehavior use;

    readonly Unassigner unassigner;

    public CardAssignment(AssignedBehavior use, Unassigner unassigner)
    {
      this.unassigner = unassigner;
      this.use = use;
    }
    public AssignedBehavior GetAssignedBehavior() { return use; }
    public ICardModel GetCard() { return new UnassignedCard(new UnassignedBehavior(use.GetBehaviorUri(), use.GetBehaviorSystem())); }

    public void Unassign()
    {
      this.unassigner.Unassign(this);
    }

    public string GetId()
    {
      return use.useId;
    }

    public bool IsValid()
    {
      return use.IsValid();
    }

    public PropEditor[] GetProperties()
    {
      return use.GetProperties();
    }

    public void SetProperties(PropEditor[] props)
    {
      use.SetProperties(props);
    }
  }
}