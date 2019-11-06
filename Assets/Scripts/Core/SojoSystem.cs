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

// Manages the database of SOJOs (small JSON Objects). Responsible for maintaining the
// local database and for networking it, ensuring it's always synchronized among clients.
public class SojoSystem : MonoBehaviour
{
  SojoDatabase database = new SojoDatabase();
  PhotonView photonView;

  public delegate void OnSojoPut(Sojo sojo);
  public event OnSojoPut onSojoPut;
  public delegate void OnSojoDelete(Sojo sojo);
  public event OnSojoDelete onSojoDelete;
  public delegate void OnSojoSystemLoaded();
  public event OnSojoSystemLoaded onSojoSystemLoaded;

  void Awake()
  {
    photonView = PhotonView.Get(this);
  }

  public Sojo GetSojoById(string id)
  {
    return database.GetSojoById(id);
  }

  public Sojo GetSojoByName(string name)
  {
    return database.GetSojoByName(name);
  }

  public List<Sojo> GetAllSojosOfType(SojoType type)
  {
    return database.GetAllSojosOfType(type);
  }

  public void PutSojo(Sojo sojo)
  {
    PutSojoLocal(sojo);
    string json = JsonUtility.ToJson(sojo.Save());
    byte[] zippedJson = Util.GZipString(json);
    photonView.RPC("PutSojoRPC", PhotonTargets.AllViaServer, zippedJson);
  }

  private void PutSojoLocal(Sojo sojo)
  {
    database.PutSojo(sojo);
    onSojoPut?.Invoke(sojo);
  }

  [PunRPC]
  void PutSojoRPC(byte[] zippedJson)
  {
    string sojoJson = Util.UnGZipString(zippedJson);
    Sojo sojo = Sojo.Load(JsonUtility.FromJson<Sojo.Saved>(sojoJson));
    PutSojoLocal(sojo);
  }

  public void DeleteSojo(string sojoId)
  {
    DeleteSojoLocal(sojoId);
    photonView.RPC("DeleteSojoRPC", PhotonTargets.AllViaServer, sojoId);
  }

  private void DeleteSojoLocal(string sojoId)
  {
    Sojo sojo = database.GetSojoById(sojoId);
    if (sojo != null)
    {
      database.DeleteSojo(sojoId);
      onSojoDelete?.Invoke(sojo);
    }
  }

  [PunRPC]
  void DeleteSojoRPC(string sojoId)
  {
    DeleteSojoLocal(sojoId);
  }

  public SojoDatabase.Saved SaveDatabase()
  {
    return database.Save();
  }

  public void LoadDatabase(SojoDatabase.Saved saved)
  {
    // Issue a Sojo delete event for each sojo (because they are going away
    // in the load).
    foreach (Sojo sojo in database.GetAllSojos())
    {
      onSojoDelete?.Invoke(sojo);
    }
    // Now load the DB. This deletes all previous SOJOs.
    database.Load(saved);
    // Issue a Sojo put event for each sojo in the database (because it's as if
    // they had all been just put in the database).
    foreach (Sojo sojo in database.GetAllSojos())
    {
      onSojoPut?.Invoke(sojo);
    }
    onSojoSystemLoaded?.Invoke();
  }

  public void Reset()
  {
    database.Reset();
  }
}
