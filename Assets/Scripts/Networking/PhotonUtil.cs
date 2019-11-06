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
using UnityEngine.Networking;

public static class PhotonUtil
{
  public static bool ActuallyConnected()
  {
    return PhotonNetwork.connected && !PhotonNetwork.offlineMode;
  }

  public static void SendColor(this PhotonStream stream, Color c)
  {
#if USE_PUN
    Debug.Assert(stream.isWriting);
    stream.SendNext(c.r);
    stream.SendNext(c.g);
    stream.SendNext(c.b);
    stream.SendNext(c.a);
#endif
  }

  public static Color ReceiveColor(this PhotonStream stream)
  {
#if USE_PUN
    Debug.Assert(stream.isReading);
    return new Color((float)stream.ReceiveNext(),
      (float)stream.ReceiveNext(),
      (float)stream.ReceiveNext(),
      (float)stream.ReceiveNext());
#endif
    return Color.white;
  }

  public static string GetPhotonStateString()
  {
#if USE_PUN
    var sb = new System.Text.StringBuilder();
    if (PhotonNetwork.connected) sb.Append("C");
    if (PhotonNetwork.connectedAndReady) sb.Append("D");
    if (PhotonNetwork.inRoom) sb.Append("R");
    if (PhotonNetwork.insideLobby) sb.Append("L");
    if (PhotonNetwork.isMasterClient) sb.Append("M");
    if (PhotonNetwork.offlineMode) sb.Append("O");
    return sb.ToString();
#else
    return "";
#endif
  }

  public static void CheckNoOtherPlayers()
  {
    if (PhotonNetwork.playerList.Length > 1)
    {
      throw new System.Exception("This operation is invalid if any other players are in the same game/room.");
    }
  }

  // Hacky, but mimicks NetworkingPeer.ResetPhotonViewsOnSerialize
  public static void ForceResendToAllPlayers(this PhotonView reliableView)
  {
#if USE_PUN
    reliableView.lastOnSerializeDataSent = null;
#endif
  }
}
