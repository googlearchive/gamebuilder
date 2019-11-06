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

using System.Collections.Generic;
using UnityEngine;

public class GlobalUnreliableData : MonoBehaviour
{
  const byte UnreliableEventCode = NetworkingEventCodes.GLOBAL_UNRELIABLE_DATA;
  const int size = 2 + 1 + 9 + 16 + 4 + 9;

  public static bool SnapRigidbodyOnRecv = false;

  //The maximum time used for prediction values.
  const float maxTimeDelta = 0.5f;

  RaiseEventOptions raiseEventOptions = new RaiseEventOptions
  {
    Receivers = ReceiverGroup.Others,
    CachingOption = EventCaching.DoNotCache
  };

  [System.Flags]
  enum UnreliableBitmask : byte
  {
    None = 0,
    Teleport = 1,
    EnablePhysics = 2
  }

  public class NetworkedActor
  {
    public VoosActor actor;
    public Vector3 lastPosition;
    public Quaternion lastRotation;
    public Color32 lastColor;

    /// Local
    public int lastTransformUpdateFrame;
    public int messagesSinceLastUpdate;
    public float updateProbability;


    /// Remote
    public Vector3 posDampVelocity;
    public float lastRecvTimestamp;
    public Vector3 linearVelocity;
  }

  // View ID -> actor data
  SortedDictionary<short, NetworkedActor> actors = new SortedDictionary<short, NetworkedActor>();
  //byte[] rawData = new byte[100 * 16];
  public int MaxUpdatesPerSecond = 20;
  public int MaxPositionUpdatesPerTick = 40;
  public float snapDistance = 3;
  float minimumProbabilityOfUpdate = 0.001f;

  private List<byte> serializationBytes = new List<byte>(2048);
  private float lastUpdateTime = -1;

  // A bunch of diagnostics.
  [System.Serializable]
  public class Diagnostics
  {
    public float lastUpdateTime;
    public float lastEventReceivedTime;
    public int lastEventContentLength;
    public int numUnknownViewIds;
    public int numNullActorEntries;
    public int numSenderOwnerMismatches;
  }
  Diagnostics diag = new Diagnostics();
  public Diagnostics GetDiagnostics() { return diag; }

  /// Monitoring (static globals..)
  private static int totalBytesSent = 0;
  const int PACKET_MONITOR_COUNT = 100;
  private static int[] lastNPackets = new int[PACKET_MONITOR_COUNT];
  private static int packetIndex = 0;
  private static float averageTraffic = 0;

  public static float AverageBytesPerSecond { get { return averageTraffic; } }
  public static int TotalBytesSent { get { return totalBytesSent; } }

#if USE_PUN
  void LateUpdate()
  {
    /// Lets only send traffic if someone is listening
    if (!PhotonNetwork.offlineMode && PhotonNetwork.otherPlayers.Length > 0)
    {

      float nextUpdateTime = lastUpdateTime + 1.0f / MaxUpdatesPerSecond;
      if (Time.realtimeSinceStartup > nextUpdateTime)
      {
        lastUpdateTime = Time.realtimeSinceStartup;
        diag.lastUpdateTime = lastUpdateTime;
        BroadcastUpdate();
      }
    }
    else
    {
      averageTraffic = 0;
    }
  }

  void BroadcastUpdate()
  {
    byte[] bytes = Serialize();

    if (bytes != null)
    {
      totalBytesSent += bytes.Length;
      lastNPackets[packetIndex] = bytes.Length;
      packetIndex = (packetIndex + 1) % PACKET_MONITOR_COUNT;
      averageTraffic = 0;
      for (int i = 0; i < PACKET_MONITOR_COUNT; i++)
      {
        averageTraffic += lastNPackets[i];
      }
      averageTraffic /= ((float)PACKET_MONITOR_COUNT / (float)MaxUpdatesPerSecond);

      PhotonNetwork.RaiseEvent(UnreliableEventCode, bytes, false, raiseEventOptions);
    }
  }

  public void OnEnable()
  {
    PhotonNetwork.OnEventCall += OnEvent;
  }

  public void OnDisable()
  {
    PhotonNetwork.OnEventCall -= OnEvent;
  }

  public void OnEvent(byte eventCode, object content, int senderId)
  {
    if (eventCode == UnreliableEventCode)
    {
      diag.lastEventReceivedTime = Time.realtimeSinceStartup;
      byte[] bytes = (byte[])content;
      diag.lastEventContentLength = bytes.Length;
      Deserialize(bytes, senderId);
    }
  }

  void ResetAllActorsToUnsent()
  {
    foreach (KeyValuePair<short, NetworkedActor> na in actors)
    {
      na.Value.messagesSinceLastUpdate = 0;
    }
  }

  void OnPhotonPlayerConnected(PhotonPlayer player)
  {
    Debug.Log("Player has joined, resending all actor data");
    ResetAllActorsToUnsent();
  }

  byte[] Serialize()
  {
    serializationBytes.Clear();

    VoosNetworkTypes.ShortToBytes stb = new VoosNetworkTypes.ShortToBytes();
    VoosNetworkTypes.UintToBytes utb = new VoosNetworkTypes.UintToBytes();
    int totalCount = 0;
    float totalProbability = 0;
    foreach (KeyValuePair<short, NetworkedActor> na in actors)
    {
      VoosActor va = na.Value.actor;
      if (va != null && va.reliablePhotonView.isMine && !va.IsParented())
      {
        Vector3 p = va.transform.position;
        Quaternion q = va.transform.rotation;
        if (na.Value.lastPosition != p || na.Value.lastRotation != q || na.Value.lastColor != va.GetTint())
        {
          na.Value.lastPosition = p;
          na.Value.lastRotation = q;
          na.Value.lastColor = va.GetTint();
          na.Value.lastTransformUpdateFrame = Time.frameCount;
          na.Value.messagesSinceLastUpdate = 0;
        }
        na.Value.updateProbability = Mathf.Clamp(1.0f / (float)(1 + na.Value.messagesSinceLastUpdate), minimumProbabilityOfUpdate, 1);
        totalProbability += na.Value.updateProbability;
      }
      totalCount++;
    }
    float probabilityOfPositionUpdate = 1;
    if (totalCount > MaxPositionUpdatesPerTick)
    {
      probabilityOfPositionUpdate = (float)MaxPositionUpdatesPerTick / totalProbability;
    }

    /// Message Header
    /// Transform count - will modify after the fact.
    serializationBytes.Add(0);
    serializationBytes.Add(0);

    /// Timestamp for prediction.
    utb.data = VoosNetworkTypes.CompressFloatToUint24(Time.time);
    serializationBytes.Add(utb.byte0);
    serializationBytes.Add(utb.byte1);
    serializationBytes.Add(utb.byte2);
    int index = 0;
    foreach (KeyValuePair<short, NetworkedActor> na in actors)
    {
      VoosActor va = na.Value.actor;
      if (va != null && va.reliablePhotonView.isMine && !va.IsParented())
      {

        if (Random.value > (probabilityOfPositionUpdate * na.Value.updateProbability))
        {
          continue;
        }
        na.Value.messagesSinceLastUpdate++;
        /// View ID
        stb.data = (short)va.reliablePhotonView.viewID;
        serializationBytes.Add(stb.byte0);
        serializationBytes.Add(stb.byte1);

        /// Network data bitmask
        UnreliableBitmask mask = UnreliableBitmask.None;
        mask |= va.GetTeleport() ? UnreliableBitmask.Teleport : UnreliableBitmask.None;
        mask |= va.GetEnablePhysics() ? UnreliableBitmask.EnablePhysics : UnreliableBitmask.None;
        serializationBytes.Add((byte)mask);

        // Unset the "teleport" flag, as it's transient and should only be used for one network packet.
        va.SetTeleport(false);

        // SUBTLE: If physics is not enabled, then pos/rot is not likely to
        // change. However, any inaccuracies are also more likely to be noticed
        // (such as static walls being slightly tilted). So given this, always
        // send full precision updates for static objects. Sure, this is less
        // efficient for things like moving platforms which do not have physics
        // but often change, but that's likely to be a minority.

        /// Local Position
        utb.WriteUint24(serializationBytes, VoosNetworkTypes.CompressFloatToUint24(na.Value.lastPosition.x));
        utb.WriteUint24(serializationBytes, VoosNetworkTypes.CompressFloatToUint24(na.Value.lastPosition.y));
        utb.WriteUint24(serializationBytes, VoosNetworkTypes.CompressFloatToUint24(na.Value.lastPosition.z));

        // Static - send full precision rotation on change. TODO I'm sure
        // there's some way to compress it to 8 bytes without loss.
        var ftb = new VoosNetworkTypes.FloatToBytes();
        Quaternion q = na.Value.lastRotation;
        ftb.SerializeTo(serializationBytes, q.x);
        ftb.SerializeTo(serializationBytes, q.y);
        ftb.SerializeTo(serializationBytes, q.z);
        ftb.SerializeTo(serializationBytes, q.w);

        //Color
        serializationBytes.Add(na.Value.lastColor.r);
        serializationBytes.Add(na.Value.lastColor.g);
        serializationBytes.Add(na.Value.lastColor.b);
        serializationBytes.Add(na.Value.lastColor.a);

        /// Linear Velocity
        Rigidbody rb = va.GetComponent<Rigidbody>();
        Vector3 lv = Vector3.zero;
        if (rb != null && !rb.isKinematic)
        {
          lv = rb.velocity;
        }
        utb.data = VoosNetworkTypes.CompressFloatToUint24(lv.x);
        serializationBytes.Add(utb.byte0);
        serializationBytes.Add(utb.byte1);
        serializationBytes.Add(utb.byte2);
        utb.data = VoosNetworkTypes.CompressFloatToUint24(lv.y);
        serializationBytes.Add(utb.byte0);
        serializationBytes.Add(utb.byte1);
        serializationBytes.Add(utb.byte2);
        utb.data = VoosNetworkTypes.CompressFloatToUint24(lv.z);
        serializationBytes.Add(utb.byte0);
        serializationBytes.Add(utb.byte1);
        serializationBytes.Add(utb.byte2);
        index++;
      }
    }
    stb.data = (short)index;
    serializationBytes[0] = stb.byte0;
    serializationBytes[1] = stb.byte1;

    if (index == 0)
    {
      return null;
    }
    return serializationBytes.ToArray();
  }

  void FixedUpdate()
  {
    foreach (KeyValuePair<short, NetworkedActor> na in actors)
    {
      VoosActor va = na.Value.actor;
      if (va != null && !va.reliablePhotonView.isMine && !va.IsParented())
      {
        Rigidbody rb = va.GetComponent<Rigidbody>();
        bool isDynamic = rb != null && va.GetEnablePhysics();

        Vector3 pos = rb != null ? rb.position : va.transform.position;
        Quaternion rot = rb != null ? rb.rotation : va.transform.rotation;

        if (!isDynamic || va.GetReplicantCatchUpMode())
        {
          // Kinematic or non-RB. Lerp it.
          Quaternion lerpedRot = Quaternion.Lerp(rot, na.Value.lastRotation, Mathf.Clamp01(Time.deltaTime * 5));
          var damped = Vector3.SmoothDamp(pos, na.Value.lastPosition,
            ref na.Value.posDampVelocity, 0.2f);

          va.ForceReplicantPosition(damped);
          va.ForceReplicantRotation(lerpedRot);
        }
        else
        {
          rb.angularVelocity *= 0.9f;

          if (!SnapRigidbodyOnRecv)
          {
            // Let physics simulate the position with our corrective velocity,
            // but we still lerp the rotation.
            Quaternion lerpedRot = Quaternion.Lerp(rot, na.Value.lastRotation, Mathf.Clamp01(Time.deltaTime * 5));
            rb.rotation = lerpedRot;
          }
        }
      }
    }
  }

  void Deserialize(byte[] bytes, int senderId)
  {
    VoosNetworkTypes.ShortToBytes stb = new VoosNetworkTypes.ShortToBytes();
    VoosNetworkTypes.UintToBytes utb = new VoosNetworkTypes.UintToBytes();

    if (bytes.Length < 6) { return; }

    stb.byte0 = bytes[0];
    stb.byte1 = bytes[1];
    short positionCount = stb.data;

    utb.byte0 = bytes[1];
    utb.byte1 = bytes[2];
    utb.byte2 = bytes[3];
    utb.byte3 = 0;
    float timestamp = VoosNetworkTypes.DecompressFloatFromUint24(utb.data);
    if (bytes.Length < 5 + positionCount * size) { return; }
    for (int i = 0; i < positionCount; i++)
    {
      int offset = 5 + i * size;
      /// View Id
      stb.byte0 = bytes[offset];
      stb.byte1 = bytes[offset + 1];
      offset += 2;

      short viewId = stb.data;
      if (actors.ContainsKey(viewId))
      {
        NetworkedActor na = actors[viewId];
        VoosActor va = na.actor;

        if (va == null)
        {
          diag.numNullActorEntries++;
        }
        if (va != null && va.reliablePhotonView.ownerId != senderId)
        {
          diag.numSenderOwnerMismatches++;
        }

        if (va != null && va.reliablePhotonView.ownerId == senderId)
        {
          va.lastUnreliableUpdateTime = Time.realtimeSinceStartup;
          va.unrel = na;

          float timeDelta = Mathf.Clamp(timestamp - na.lastRecvTimestamp, 0.01f, maxTimeDelta);
          na.lastRecvTimestamp = timestamp;

          UnreliableBitmask mask = (UnreliableBitmask)bytes[offset];
          offset += 1;

          Vector3 oldP = na.lastPosition;
          /// Local Position
          utb.byte0 = bytes[offset];
          utb.byte1 = bytes[offset + 1];
          utb.byte2 = bytes[offset + 2];
          utb.byte3 = 0;
          na.lastPosition.x = VoosNetworkTypes.DecompressFloatFromUint24(utb.data);
          utb.byte0 = bytes[offset + 3];
          utb.byte1 = bytes[offset + 4];
          utb.byte2 = bytes[offset + 5];
          utb.byte3 = 0;
          na.lastPosition.y = VoosNetworkTypes.DecompressFloatFromUint24(utb.data);
          utb.byte0 = bytes[offset + 6];
          utb.byte1 = bytes[offset + 7];
          utb.byte2 = bytes[offset + 8];
          utb.byte3 = 0;
          offset += 9;
          na.lastPosition.z = VoosNetworkTypes.DecompressFloatFromUint24(utb.data);
          Vector3 predictionError = na.lastPosition - va.transform.position;

          Quaternion oldQ = na.lastRotation;

          // Full precision rotation for static
          var ftb = new VoosNetworkTypes.FloatToBytes();
          float x = ftb.DeserializeFrom(bytes, ref offset);
          float y = ftb.DeserializeFrom(bytes, ref offset);
          float z = ftb.DeserializeFrom(bytes, ref offset);
          float w = ftb.DeserializeFrom(bytes, ref offset);
          na.lastRotation = new Quaternion(x, y, z, w);

          /// Color
          Color32 c = new Color32();
          c.r = bytes[offset];
          c.g = bytes[offset + 1];
          c.b = bytes[offset + 2];
          c.a = bytes[offset + 3];
          offset += 4;
          va.SetTint(c);

          /// Linear V
          utb.byte0 = bytes[offset];
          utb.byte1 = bytes[offset + 1];
          utb.byte2 = bytes[offset + 2];
          utb.byte3 = 0;
          na.linearVelocity.x = VoosNetworkTypes.DecompressFloatFromUint24(utb.data);
          utb.byte0 = bytes[offset + 3];
          utb.byte1 = bytes[offset + 4];
          utb.byte2 = bytes[offset + 5];
          utb.byte3 = 0;
          na.linearVelocity.y = VoosNetworkTypes.DecompressFloatFromUint24(utb.data);
          utb.byte0 = bytes[offset + 6];
          utb.byte1 = bytes[offset + 7];
          utb.byte2 = bytes[offset + 8];
          utb.byte3 = 0;
          na.linearVelocity.z = VoosNetworkTypes.DecompressFloatFromUint24(utb.data);

          Rigidbody rb = va.GetComponent<Rigidbody>();
          bool isDynamic = rb != null && va.GetEnablePhysics();

          // Hard Snap if the teleport flag is on. Otherwise we smoothly interpolate.
          if ((mask & UnreliableBitmask.Teleport) == UnreliableBitmask.Teleport)
          {
            predictionError = Vector3.zero;
            va.transform.position = na.lastPosition;
            va.transform.rotation = na.lastRotation;

            if (rb != null)
            {
              rb.velocity = Vector3.zero;
              rb.angularVelocity = Vector3.zero;
            }
          }

          if (va.IsParented())
          {
            continue;
          }

          if (isDynamic)
          {
            if (SnapRigidbodyOnRecv)
            {
              rb.velocity = na.linearVelocity;
              rb.MovePosition(na.lastPosition);
              rb.MoveRotation(na.lastRotation);
            }
            else
            {
              const float catchUpEnterThresh = 1f;
              const float catchUpExitThresh = 0.5f;
              float errorDist = predictionError.magnitude;
              if (!va.GetReplicantCatchUpMode())
              {
                if (errorDist > catchUpEnterThresh)
                {
                  va.SetReplicantCatchUpMode(true);
                }
              }
              else
              {
                if (va.debug)
                {
                  Util.Log($"in catch up mode... err dist: {errorDist}");
                }
                if (errorDist < catchUpExitThresh)
                {
                  va.SetReplicantCatchUpMode(false);
                }
              }

              if (va.GetReplicantCatchUpMode())
              {
                // Let the fixed update directly lerp this.
              }
              else
              {
                // Best-effort corrective velocity only.
                // TODO expose some tunable error -> correction velocity parameter?
                rb.velocity = na.linearVelocity + predictionError;
              }
            }
          }
        }
      }
      else
      {
        diag.numUnknownViewIds++;
      }
    }  
  }
#endif

  public void AddActor(VoosActor va)
  {
#if USE_PUN
    if (va.reliablePhotonView == null)
    {
      throw new System.Exception("GlobalUnreliableData only accepts actors with reliablePhotonView (not local drafts)");
    }
    if (actors.ContainsKey((short)va.reliablePhotonView.viewID))
    {
      Debug.LogWarning("Adding an actor that is already added.  Is this an ownership transfer?");
      return;
    }
    NetworkedActor na = new NetworkedActor();
    na.actor = va;
    na.lastPosition = va.transform.position;
    na.lastRotation = va.transform.rotation;
    na.lastColor = va.GetTint();
    na.lastTransformUpdateFrame = Time.frameCount;
    na.messagesSinceLastUpdate = 0;
    actors.Add((short)va.reliablePhotonView.viewID, na);
#endif
  }

  public void RemoveActor(VoosActor va)
  {
#if USE_PUN
    if (va.reliablePhotonView != null && actors.ContainsKey((short)va.reliablePhotonView.viewID))
    {
      actors.Remove((short)va.reliablePhotonView.viewID);
    }
    else
    {
      Util.LogError($"ignoring RemoveActor request of {va.name}");
    }
#endif
  }
}
