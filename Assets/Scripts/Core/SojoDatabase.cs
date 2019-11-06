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
using System.Collections.Generic;

// Database of SOJOs (Small JSON Objects).
// This class is responsible for maintaining the database locally,
// but NOT responsible for networking it (see SojoSystem for that).
public class SojoDatabase
{
  private const string BUILTIN_SOJOS_RESOURCE_FILE = "BuiltinSojos";

  // SOJOs by ID.
  private Dictionary<string, Sojo> sojosById = new Dictionary<string, Sojo>();
  // Lazy cache of SOJOs by name. More than one SOJO can have the same name, so this
  // stores an arbitrary one.
  private Dictionary<string, Sojo> cacheByName = new Dictionary<string, Sojo>();
  // Names known NOT to exist (performance optimization in case some script code wants to
  // query for a name repeatedly even though it's not in the DB).
  private HashSet<string> namesKnownNotToExist = new HashSet<string>();

  public SojoDatabase() { }

  public void Reset()
  {
    sojosById.Clear();
    cacheByName.Clear();
    namesKnownNotToExist.Clear();
  }

  public void PutSojo(Sojo sojo)
  {
    // We must first delete it to keep our cache consistent.
    DeleteSojo(sojo.id);
    // Now add it.
    sojosById[sojo.id] = sojo;
    cacheByName[sojo.name] = sojo;
    namesKnownNotToExist.Remove(sojo.name);
  }

  public Sojo GetSojoById(string id)
  {
    Sojo sojo;
    return sojosById.TryGetValue(id, out sojo) ? sojo : null;
  }

  // Gets a SOJO with the given name. If there is more than one, returns an arbitrary one.
  public Sojo GetSojoByName(string name)
  {
    Sojo sojo;
    if (namesKnownNotToExist.Contains(name))
    {
      return null;
    }
    else if (cacheByName.TryGetValue(name, out sojo))
    {
      return sojo;
    }
    // Linear search.
    foreach (KeyValuePair<string, Sojo> pair in sojosById)
    {
      if (pair.Value.name == name)
      {
        // Found it. Cache it.
        cacheByName[name] = pair.Value;
        return pair.Value;
      }
    }
    // We now know that a Sojo by this name does not exist, so cache this
    // knowledge for performance in case we get asked again.
    namesKnownNotToExist.Add(name);
    return null;
  }

  public void DeleteSojo(string sojoId)
  {
    Sojo sojo;
    if (sojosById.TryGetValue(sojoId, out sojo))
    {
      sojosById.Remove(sojoId);
      Sojo cachedSojo;
      if (cacheByName.TryGetValue(sojo.name, out cachedSojo) && sojo == cachedSojo)
      {
        cacheByName.Remove(sojo.name);
      }
    }
  }

  // Returns a list of Sojo's of the given type. This is a linear-time operation that shouldn't
  // be called too often. Maybe once when populating a list is OK, but don't call every frame.
  public List<Sojo> GetAllSojosOfType(SojoType type)
  {
    List<Sojo> sojos = new List<Sojo>();
    foreach (Sojo sojo in sojosById.Values)
    {
      if (sojo.contentType == type)
      {
        sojos.Add(sojo);
      }
    }
    return sojos;
  }

  public List<Sojo> GetAllSojos()
  {
    return new List<Sojo>(sojosById.Values);
  }

  public Saved Save()
  {
    Saved saved = new Saved();
    saved.sojos = new Sojo.Saved[sojosById.Count];
    int i = 0;
    foreach (Sojo sojo in sojosById.Values)
    {
      saved.sojos[i++] = sojo.Save();
    }
    return saved;
  }

  public void Load(Saved database)
  {
    Reset();
    string builtinSojoJson = Resources.Load<TextAsset>(BUILTIN_SOJOS_RESOURCE_FILE).text;
    SojoDatabase.Saved builtIn = JsonUtility.FromJson<SojoDatabase.Saved>(builtinSojoJson);
    foreach (Sojo.Saved saved in builtIn.sojos)
    {
      PutSojo(Sojo.Load(saved));
    }
    foreach (Sojo.Saved saved in database.sojos)
    {
      PutSojo(Sojo.Load(saved));
    }
  }

  [System.Serializable]
  public struct Saved
  {
    public Sojo.Saved[] sojos;
  }
}

public enum SojoType
{
  // DO NOT CHANGE the names of these enum values. They are used in serialized data.
  SoundEffect,
  ParticleEffect,
  Image,
  ActorPrefab
}

public class Sojo
{
  // Unique ID (GUID)
  public readonly string id;
  // User-facing (display) name
  public readonly string name;
  // Sojo content type
  public readonly SojoType contentType;
  // Content (JSON).
  public readonly string content;

  [System.Serializable]
  public struct Saved
  {
    public string id;
    public string name;
    public string contentType;
    public string content;
  }

  public Sojo(string id, string name, SojoType contentType, string content)
  {
    this.id = id;
    this.name = name;
    this.contentType = contentType;
    this.content = content;
  }

  public static Sojo Load(Saved saved)
  {
    return new Sojo(saved.id, saved.name, Util.ParseEnum<SojoType>(saved.contentType), saved.content);
  }

  public Saved Save()
  {
    Saved json = new Saved();
    json.id = id;
    json.name = name;
    json.content = content;
    json.contentType = contentType.ToString();
    return json;
  }

  public override string ToString()
  {
    return string.Format("SOJO id:{0}, name:{1}, type:{2}, content:{3}", id, name, contentType, content);
  }
}
