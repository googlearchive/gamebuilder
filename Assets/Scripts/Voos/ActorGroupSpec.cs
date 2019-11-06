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

// This is a "spec" or "selector" that specifies a group of actors.
public class ActorGroupSpec
{
  public enum Mode
  {
    // No actor.
    NONE,
    // Actors with a specific tag.
    BY_TAG,
    // A specific actor.
    BY_NAME,
    // Any actor
    ANY
  };

  public readonly Mode mode;
  public readonly string tagOrName;

  private ActorGroupSpec(Mode mode, string tagOrName)
  {
    this.mode = mode;
    this.tagOrName = tagOrName;
  }

  public ActorGroupSpec WithTagOrName(string newValue)
  {
    return new ActorGroupSpec(this.mode, newValue);
  }

  public static ActorGroupSpec FromString(string actorGroupSpecString)
  {
    Mode mode;
    string tagOrName = "";
    if (string.IsNullOrEmpty(actorGroupSpecString))
    {
      mode = Mode.NONE;
    }
    else if (actorGroupSpecString.ToUpperInvariant().StartsWith("@TAG:"))
    {
      mode = Mode.BY_TAG;
      tagOrName = actorGroupSpecString.Substring("@TAG:".Length);
    }
    else if (actorGroupSpecString.ToUpperInvariant() == "@ANY")
    {
      mode = Mode.ANY;
      tagOrName = "";
    }
    else
    {
      mode = Mode.BY_NAME;
      tagOrName = actorGroupSpecString;
    }
    return new ActorGroupSpec(mode, tagOrName);
  }

  public static ActorGroupSpec NewNone()
  {
    return new ActorGroupSpec(Mode.NONE, "");
  }

  public static ActorGroupSpec NewByTag(string tag)
  {
    return new ActorGroupSpec(Mode.BY_TAG, tag);
  }

  public static ActorGroupSpec NewByName(string name)
  {
    return new ActorGroupSpec(Mode.BY_NAME, name);
  }

  public static ActorGroupSpec NewAny()
  {
    return new ActorGroupSpec(Mode.ANY, "");
  }

  public override string ToString()
  {
    switch (mode)
    {
      case Mode.NONE:
        return "";
      case Mode.BY_TAG:
        return "@TAG:" + tagOrName;
      case Mode.BY_NAME:
        return tagOrName;
      case Mode.ANY:
        return "@ANY";
      default:
        throw new System.Exception("Invalid mode " + mode);
    }
  }

  public string ToUserFriendlyString(VoosEngine engine)
  {
    switch (mode)
    {
      case Mode.NONE:
        return "(Nothing)";
      case Mode.BY_TAG:
        return (tagOrName == "player") ? "(Player)" : "Tag: " + tagOrName;
      case Mode.BY_NAME:
        VoosActor actor = engine.GetActor(tagOrName);
        return actor != null ? actor.GetDisplayName() : "(invalid)";
      case Mode.ANY:
        return "(any actor)";
      default:
        throw new System.Exception("Invalid mode " + mode);
    }
  }
}