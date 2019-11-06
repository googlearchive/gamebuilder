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

#if !USE_PUN

using System;

namespace Photon
{
  public class PunBehaviour : UnityEngine.MonoBehaviour
  {
    public virtual void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer) { }
    public virtual void OnPhotonPlayerConnected(PhotonPlayer newPlayer) { }
    public virtual void OnJoinedRoom() { }

    public PhotonView photonView;

    void Awake()
    {
      photonView = GetComponent<PhotonView>();
    }
  }
  public class MonoBehaviour : UnityEngine.MonoBehaviour { }
}

[System.AttributeUsage(System.AttributeTargets.Method)]
public class PunRPC : System.Attribute { }

public interface IPunObservable
{
  void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info);
}

public class PhotonPlayer
{
  public string NickName = "playerDummy";
  public int ID = 0;
  public static PhotonPlayer Find(object a) { return new PhotonPlayer(); }
  public int GetRoomIndex() { return 0; }
}

namespace ExitGames
{
  namespace UtilityScripts
  {
  }
}

public enum ReceiverGroup { Others }
public enum EventCaching { DoNotCache }

public struct RaiseEventOptions
{
  public ReceiverGroup Receivers;
  public EventCaching CachingOption;
}

public enum DisconnectCause { }

public class PhotonRoom
{
  public string name = "dummy";
  public string Name = "dummy";
  public bool IsVisible = false;
}

public static class PhotonNetwork
{
  public static PhotonRoom room = new PhotonRoom();
  public static bool connected = true;
  public static bool isMasterClient = true;
  public static bool offlineMode = true;
  public static bool inRoom = true;

  public static void Disconnect() { }

  public static void RaiseEvent(object a, object b, object c, object d) { }

  internal static void UnAllocateViewID(int viewId) { }

  public static PhotonPlayer player = new PhotonPlayer();
  public static PhotonPlayer masterClient { get { return player; } }

  public static Action<byte, object, int> OnEventCall { get; internal set; }

  public static PhotonPlayer[] playerList = new PhotonPlayer[] { new PhotonPlayer() };
  public static PhotonPlayer[] otherPlayers = new PhotonPlayer[0];
  internal static bool autoCleanUpPlayerObjects = false;
  internal static bool autoJoinLobby;

  internal static int AllocateViewID()
  {
    return 0;
  }

  internal static void ReleaseIdOfView(PhotonView reliablePhotonView) { }

  internal static void LeaveRoom()
  {
  }

  internal static void DestroyAll()
  {
  }
}

public class PhotonStream { }

public class PhotonMessageInfo
{
  public PhotonPlayer sender = PhotonNetwork.masterClient;
}

public class RoomInfo { }

public enum CloudRegionCode { }
public enum PhotonTargets { Others, AllViaServer, All, MasterClient }

#endif