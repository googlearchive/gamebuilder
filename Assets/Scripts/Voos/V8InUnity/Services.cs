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

using System.Runtime.InteropServices;
using UnityEngine;
using System.Text;
using System.Collections.Generic;

namespace V8InUnity
{
  // Bindings to the native plugin functions
  public class Services
  {
    TerrainManager terrainSystem;

    VoosEngine engine;
    GameUiMain gameUiMain;
    SoundEffectSystem soundEffectSystem;
    ParticleEffectSystem particleEffectSystem;
    UserMain userMainLazy;  // lazily queried in GetUserMain()
    VirtualPlayerManager virtualPlayerManager;
    PlayerControlsManager playerControlsManager;
    GameBuilderStage gbStage;

    public Services(
      TerrainManager terrainSystem,
      VoosEngine engine,
      GameUiMain gameUiMain,
      SoundEffectSystem soundEffectSystem,
      ParticleEffectSystem particleEffectSystem,
      VirtualPlayerManager virtualPlayerManager,
      PlayerControlsManager playerControlsManager,
      GameBuilderStage gbStage)
    {
      this.terrainSystem = terrainSystem;
      this.engine = engine;
      this.gameUiMain = gameUiMain;
      this.soundEffectSystem = soundEffectSystem;
      this.particleEffectSystem = particleEffectSystem;
      this.virtualPlayerManager = virtualPlayerManager;
      this.playerControlsManager = playerControlsManager;
      this.gbStage = gbStage;
    }

    [System.Serializable]
    struct OverlapSphereArgs
    {
      public Vector3 center;
      public float radius;
      public string tag;
    }

    [System.Serializable]
    struct PhysicsBox
    {
      public Vector3 center;
      public Vector3 dimensions;
      public Quaternion rotation;
    }

    [System.Serializable]
    struct CheckBoxArgs
    {
      public PhysicsBox box;
    }

    [System.Serializable]
    struct PhysicsRaycast
    {
      public Vector3 origin;
      public Vector3 direction;
      public float maxDistance;
      public bool sortByDistance;
    }

    [System.Serializable]
    struct PhysicsCastRequest
    {
      public Vector3 origin;
      public Vector3 dir;
      public float radius;  // 0 means raycast, > 0 is sphere cast.
      public float maxDist;
      public CastMode mode;
      public bool includeActors;
      public bool includeTerrain;
      public string excludeActor;
    }

    enum CastMode
    {
      // Keep these in sync with the enums in APIv2's casting.js
      BOOLEAN = 0,
      CLOSEST = 1,
      ALL_UNSORTED = 2,
      ALL_SORTED = 3
    }

    [System.Serializable]
    struct PhysicsCastHit
    {
      public string actor; // if empty, it's a terrain hit
      public Vector3 point;
      public float distance;
    }

    [System.Serializable]
    struct PhysicsCastResult
    {
      public PhysicsCastHit[] hits;
    }

    [System.Serializable]
    struct VirtualPlayersResult
    {
      public VirtualPlayerResultEntry[] allPlayers;
      public string localPlayerId;
    }

    [System.Serializable]
    struct VirtualPlayerResultEntry
    {
      public string id;
      public int slotNumber;
      public string nickName;
    }

    [System.Serializable]
    struct GetActorFieldArgs
    {
      public ushort actorId;
      public ushort fieldId;
    }

    [System.Serializable]
    struct SetActorColorFieldArgs
    {
      public ushort actorId;
      public ushort fieldId;
      public Color newValue;
    }

    [System.Serializable]
    struct PlaySoundRequest
    {
      public string soundId;
      // Must provide an actor name OR a position.
      public string actorName;
      public Vector3 position;
    }

    [System.Serializable]
    struct ParticleEffectRequest
    {
      public string pfxId;
      public Vector3 position;
      public Vector3 rotation;
      public float scale;
    }

    [System.Serializable]
    struct OneShotAnimationRequest
    {
      public ushort actorTempId;
      public string animationName;
    }

    [System.Serializable]
    struct GetActorScreenRectRequest
    {
      public string actor;
    }

    [System.Serializable]
    struct GetActorScreenRectResponse
    {
      public float x, y, w, h;
    }

    [System.Serializable]
    struct TempCameraOffsetRequest
    {
      public string actor;
      public Vector3 offset;
    }
    [System.Serializable]
    struct GetTerrainCellResult
    {
      public int shape;
      public int dir;
      public int style;
    }

    [System.Serializable]
    struct SphereArg
    {
      public Vector3 center;
      public float radius;
    }

    [System.Serializable]
    struct CameraInfo
    {
      public Vector3 pos;
      public Quaternion rot;
    }

    // Because Unity's JsonUtility can't handle non-object top-level JSON types:
    [System.Serializable]
    struct JsonWrapper<T>
    {
      public T value;
      public static JsonWrapper<T> Wrap(T value)
      {
        return new JsonWrapper<T> { value = value };
      }
    }

    // KEEP SYNC'D WITH HandlerApi.js.txt
    static int MAX_PHYSICS_QUERY_RESULTS = 500;

    private Collider[] SharedColliderBuffer = new Collider[MAX_PHYSICS_QUERY_RESULTS];
    private RaycastHit[] SharedRaycastHitBuffer = new RaycastHit[MAX_PHYSICS_QUERY_RESULTS];
    private StringBuilder SharedStringBuilder = new StringBuilder();

    delegate string ToStringFunction<T>(T proxy);
    delegate bool KeepFunction<T>(T proxy);

    string BuildStringArrayJson<T>(T[] objects, int numValidObjects,
    KeepFunction<T> keepFunction,
    ToStringFunction<T> toString, StringBuilder jsoner)
    {
      jsoner.Clear();
      jsoner.Append("[");
      bool gotOneString = false;
      for (int i = 0; i < numValidObjects; i++)
      {
        T obj = objects[i];
        if (!keepFunction(obj))
        {
          continue;
        }
        string theString = toString(obj);
        if (theString != null)
        {
          if (gotOneString)
          {
            jsoner.Append(",");
          }
          gotOneString = true;
          jsoner.Append("\"");
          jsoner.Append(theString);
          jsoner.Append("\"");
        }
      }
      jsoner.Append("]");
      return jsoner.ToString();
    }

    bool IsScriptReadyActor(Collider collider)
    {
      VoosActor actor = collider.GetComponentInParent<VoosActor>();
      if (actor == null)
      {
        return false;
      }
      return true;
    }

    string GetActorName(Collider collider)
    {
      return collider.GetComponentInParent<VoosActor>()?.GetName();
    }

    VoosEngine GetEngine()
    {
      return engine;
    }

    public static bool CacheReportResult = true;
    System.IntPtr firstReportResultPtr = System.IntPtr.Zero;
    Native.ReportResultFunction cachedReportResult = null;

    // Ideally this wouldn't live here, but for now, whatever.
    internal void CallService(string serviceName, string argsJson, System.IntPtr reportResultPtr)
    {
      Native.ReportResultFunction reportResult = null;
      if (CacheReportResult)
      {
        if (cachedReportResult == null)
        {
          cachedReportResult = Marshal.GetDelegateForFunctionPointer<Native.ReportResultFunction>(reportResultPtr);
          firstReportResultPtr = reportResultPtr;
        }

        reportResult = cachedReportResult;
        Debug.Assert(firstReportResultPtr == reportResultPtr, $"Different reportResultPtr was given!");
      }
      else
      {
        reportResult = Marshal.GetDelegateForFunctionPointer<Native.ReportResultFunction>(reportResultPtr);
      }

      try
      {
        switch (serviceName)
        {
          case "BeginProfileSample":
            {
              InGameProfiler.BeginSection(argsJson);
              reportResult("0");
              break;
            }
          case "EndProfileSample":
            {
              InGameProfiler.EndSection();
              reportResult("0");
              break;
            }
          case "OverlapSphere":
            using (Util.Profile(serviceName))
            {
              var args = JsonUtility.FromJson<OverlapSphereArgs>(argsJson);
              int numHits = Physics.OverlapSphereNonAlloc(args.center, args.radius, SharedColliderBuffer, VoosActor.LayerMaskValue, QueryTriggerInteraction.Collide);
              if (numHits == SharedColliderBuffer.Length)
              {
                Util.LogError($"The OverlapSphere call exceeded the maximum number of allowed results ({SharedColliderBuffer.Length}). You are probably not getting what you want. Please try to adjust the parameters so you get less hits, like reducing the radius.");
              }
              using (Util.Profile("report actors"))
              {
                var jsoner = SharedStringBuilder;
                jsoner.Clear();
                jsoner.Append("[");
                bool gotOneActor = false;
                for (int i = 0; i < numHits; i++)
                {
                  Collider obj = SharedColliderBuffer[i];
                  VoosActor actor = obj.GetComponentInParent<VoosActor>();
                  if (actor == null) continue;
                  if (!args.tag.IsNullOrEmpty())
                  {
                    if (!actor.HasTag(args.tag)) continue;
                  }

                  if (gotOneActor) jsoner.Append(",");
                  jsoner.Append("\"");
                  jsoner.Append(actor.GetName());
                  jsoner.Append("\"");
                  gotOneActor = true;
                }
                jsoner.Append("]");
                reportResult(jsoner.ToString());
              }
              break;
            }
          case "CheckBox":
            using (Util.Profile(serviceName))
            {
              var args = JsonUtility.FromJson<CheckBoxArgs>(argsJson);
              bool hitAnything = Physics.CheckBox(args.box.center, args.box.dimensions * 0.5f, args.box.rotation,
              -1, // We're probably looking for clearance, so return true if the box hits actors OR just static terrain.
              QueryTriggerInteraction.Ignore // Ignore triggers for checks. We are probably looking for clearance, in which case triggers don't matter.
              );
              reportResult(hitAnything ? "true" : "false");
              break;
            }

          case "Cast":
            using (Util.Profile(serviceName))
            {
              PhysicsCastRequest req = JsonUtility.FromJson<PhysicsCastRequest>(argsJson);

              int layerMask = (req.includeActors ? VoosActor.LayerMaskValue : 0) |
                (req.includeTerrain ? LayerMask.GetMask("Default") : 0);
              int numHits = req.radius < 0.01 ?
                Physics.RaycastNonAlloc(req.origin, req.dir, SharedRaycastHitBuffer, req.maxDist, layerMask, QueryTriggerInteraction.Collide) :
                Physics.SphereCastNonAlloc(req.origin, req.radius, req.dir, SharedRaycastHitBuffer, req.maxDist, layerMask, QueryTriggerInteraction.Collide);

              // These variables things are somewhat redundant, but for performance we keep all three and
              // only use the appropriate ones per req.castMode, to avoid unnecessary allocations.
              // List of results. Only allocate if we have to.
              List<PhysicsCastHit> results = null;
              // Did we have any hit?
              bool anyHit = false;
              // The closest hit we got. Only valid if anyHit == true.
              PhysicsCastHit closestHit = new PhysicsCastHit();

              if (req.mode == CastMode.ALL_SORTED || req.mode == CastMode.ALL_UNSORTED)
              {
                // We will need to return a list, so allocate it.
                results = new List<PhysicsCastHit>();
              }

              for (int i = 0; i < numHits; i++)
              {
                RaycastHit hit = SharedRaycastHitBuffer[i];
                PhysicsCastHit thisHit;
                if (hit.collider != null && hit.collider.gameObject != null &&
                    hit.collider.gameObject.GetComponent<IgnoreRaycastFromScript>() != null)
                {
                  continue;
                }
                if (req.includeActors && IsScriptReadyActor(hit.collider) && GetActorName(hit.collider) != req.excludeActor)
                {
                  // Hit an actor.
                  // (PhysicsCastHit is a struct, not allocating on heap)
                  thisHit = new PhysicsCastHit
                  {
                    actor = GetActorName(hit.collider),
                    distance = hit.distance,
                    point = hit.point
                  };
                }
                else if (req.includeTerrain && hit.collider.tag == "Ground")
                {
                  // Hit terrain.
                  // (PhysicsCastHit is a struct, not allocating on heap)
                  thisHit = new PhysicsCastHit
                  {
                    actor = null,
                    distance = hit.distance,
                    point = hit.point
                  };
                }
                else
                {
                  continue;
                }
                closestHit = (!anyHit || thisHit.distance < closestHit.distance) ? thisHit : closestHit;
                anyHit = true;
                results?.Add(thisHit);
                if (req.mode == CastMode.BOOLEAN)
                {
                  // If we're just returning true/false, that's all we need.
                  break;
                }
              }

              // Sort results by distance, if requested.
              if (req.mode == CastMode.ALL_SORTED)
              {
                results.Sort((a, b) => a.distance.CompareTo(b.distance));
              }

              // Report results as requested.
              if (req.mode == CastMode.ALL_SORTED || req.mode == CastMode.ALL_UNSORTED)
              {
                PhysicsCastResult result = new PhysicsCastResult { hits = results.ToArray() };
                reportResult(JsonUtility.ToJson(result));
              }
              else if (req.mode == CastMode.CLOSEST)
              {
                reportResult(anyHit ? JsonUtility.ToJson(closestHit) : "null");
              }
              else
              {
                reportResult(anyHit ? "true" : "false");
              }
              break;
            }

          case "GetPlayerActors":
            using (Util.Profile(serviceName))
            {
              var jsoner = SharedStringBuilder;
              jsoner.Clear();
              jsoner.Append("[");
              bool gotOneActor = false;

              foreach (VoosActor actor in engine.EnumerateActors())
              {
                if (actor.GetIsPlayerControllable())
                {
                  if (gotOneActor) jsoner.Append(",");
                  jsoner.Append("\"");
                  jsoner.Append(actor.GetName());
                  jsoner.Append("\"");
                  gotOneActor = true;
                }
              }
              jsoner.Append("]");
              reportResult(jsoner.ToString());
              break;
            }

          case "SetTerrainCell":
            {
              var args = JsonUtility.FromJson<TerrainManager.SetCellRpcJsonable>(argsJson);
              terrainSystem.SetCellValue(args.cell, args.value);
              reportResult("true");
              break;
            }

          case "GetTerrainCell":
            {
              Vector3 coords = JsonUtility.FromJson<Vector3>(argsJson);
              TerrainManager.CellValue cellValue = terrainSystem.GetCellValue(
                new TerrainManager.Cell((int)coords.x, (int)coords.y, (int)coords.z));
              GetTerrainCellResult result = new GetTerrainCellResult
              {
                shape = (int)cellValue.blockType,
                dir = (int)cellValue.direction,
                style = (int)cellValue.style
              };
              reportResult(JsonUtility.ToJson(result));
              break;
            }

          case "IsMultiplayer":
            {
              reportResult(GameBuilderApplication.CurrentGameOptions.playOptions.isMultiplayer ? "true" : "false");
              break;
            }

          case "TransferPlayerControl":
            {
              VoosEngine.TransferPlayerControlRequest request =
                  JsonUtility.FromJson<VoosEngine.TransferPlayerControlRequest>(argsJson);
              // Engine will handle this asynchronously because the actor might not be immediately
              // available (maybe it was a clone that was just created, for instance).
              GetEngine().RequestTransferPlayerControl(request);
              reportResult("true");
              break;
            }

          case "GetPlayerControlledActor":
            {
              VoosActor actor = GetUserMain().GetPlayerActor();
              reportResult(actor == null ? "null" : "\"" + actor.GetName() + "\"");
              break;
            }

          case "IsMasterClient":
            {
              reportResult(PhotonNetwork.isMasterClient ? "true" : "false");
              break;
            }

          case "GetPlayersInfo":
            {
              Dictionary<int, string> nicknames = new Dictionary<int, string>();
              int i;
              for (i = 0; i < PhotonNetwork.playerList.Length; i++)
              {
                int id = PhotonNetwork.playerList[i].ID;
                string nick = PhotonNetwork.playerList[i].NickName;
                nicknames[id] = nick;
              }

              VirtualPlayersResult result = new VirtualPlayersResult();
              result.allPlayers = new VirtualPlayerResultEntry[virtualPlayerManager.GetVirtualPlayerCount()];

              i = 0;
              foreach (VirtualPlayerManager.VirtualPlayerInfo virtualPlayer in virtualPlayerManager.EnumerateVirtualPlayers())
              {
                Debug.Assert(i < result.allPlayers.Length);
                result.allPlayers[i].id = virtualPlayer.virtualId;
                result.allPlayers[i].slotNumber = virtualPlayer.slotNumber;
                result.allPlayers[i].nickName = virtualPlayer.nickName;
                ++i;
              }
              Debug.Assert(i == result.allPlayers.Length);

              result.localPlayerId = playerControlsManager.GetVirtualPlayerId();
              reportResult(JsonUtility.ToJson(result));
              break;
            }

          case "RequestUi":
            {
              using (Util.Profile("RequestUi"))
              {
                GameUiMain.UiCommandList list = JsonUtility.FromJson<GameUiMain.UiCommandList>(argsJson);
                gameUiMain.SetUiCommands(list);
                reportResult("true");
              }
              break;
            }

          case "GetActorColorField":
            {
              GetActorFieldArgs args = JsonUtility.FromJson<GetActorFieldArgs>(argsJson);
              reportResult(JsonUtility.ToJson(engine.GetActorColor(args.actorId, args.fieldId)));
              break;
            }

          case "SetActorColorField":
            {
              SetActorColorFieldArgs args = JsonUtility.FromJson<SetActorColorFieldArgs>(argsJson);
              engine.SetActorColor(args.actorId, args.fieldId, args.newValue);
              reportResult("true");
              break;
            }

          case "CloneActor":
            using (Util.Profile(serviceName))
            {
              VoosEngine.CloneActorRequest args = JsonUtility.FromJson<VoosEngine.CloneActorRequest>(argsJson);
              VoosEngine.CloneActorResponse response = GetEngine().CloneActorForScript(args);
              reportResult(JsonUtility.ToJson(response));
              break;
            }

          case "InstantiatePrefab":
            using (Util.Profile(serviceName))
            {
              VoosEngine.InstantiatePrefab.Request args = JsonUtility.FromJson<VoosEngine.InstantiatePrefab.Request>(argsJson);
              VoosEngine.InstantiatePrefab.Response response = GetEngine().InstantiatePrefabForScript(args);
              reportResult(JsonUtility.ToJson(response));
              break;
            }

          case "DestroyActors":
            using (Util.Profile(serviceName))
            {
              VoosEngine.DestroyActorsRequest args = JsonUtility.FromJson<VoosEngine.DestroyActorsRequest>(argsJson);
              GetEngine().DestroyActorsForScript(args);
              reportResult("true");
              break;
            }

          case "PlaySound":
            using (Util.Profile(serviceName))
            {
              PlaySoundRequest args = JsonUtility.FromJson<PlaySoundRequest>(argsJson);
              SoundEffect sfx = soundEffectSystem.GetSoundEffect(args.soundId);
              if (sfx != null)
              {
                if (string.IsNullOrEmpty(args.actorName))
                {
                  soundEffectSystem.PlaySoundEffect(sfx, null, args.position);
                }
                else
                {
                  VoosActor actor = engine.GetActor(args.actorName);
                  if (actor == null)
                  {
                    Debug.LogError("Could not play sound on actor. Actor not found: " + args.actorName);
                    reportResult("false");
                  }
                  soundEffectSystem.PlaySoundEffect(sfx, actor, Vector3.zero);
                }
                reportResult("true");
              }
              else
              {
                Debug.LogWarning("No SFX with ID: " + args.soundId);
                reportResult("false");
              }
              break;
            }
          case "SpawnParticleEffect":
            using (Util.Profile(serviceName))
            {
              ParticleEffectRequest args = JsonUtility.FromJson<ParticleEffectRequest>(argsJson);
              ParticleEffect pfx = particleEffectSystem.GetParticleEffect(args.pfxId);
              if (pfx != null)
              {
                particleEffectSystem.SpawnParticleEffect(
                  pfx, args.position, args.rotation * Mathf.Rad2Deg, args.scale);
                reportResult("true");
              }
              else
              {
                Debug.LogWarning("No particle effect with ID: " + args.pfxId);
                reportResult("false");
              }
              break;
            }
          case "PlayOneShotAnimation":
            {
              OneShotAnimationRequest req = JsonUtility.FromJson<OneShotAnimationRequest>(argsJson);
              VoosActor actor = engine.GetActorByTempId(req.actorTempId);
              if (actor != null)
              {
                actor.PlayOneShotAnimation(req.animationName);
              }
              else
              {
                Util.LogError($"PlayOneShotAnimation: Could not find actor for temp ID {req.actorTempId}. Ignoring.");
              }
              reportResult("true");
              break;
            }

          case "ProjectPoint":
            {
              Camera cam = GetUserMain().GetCamera();
              Vector3 point = JsonUtility.FromJson<Vector3>(argsJson);
              Vector3 screenPoint = cam.WorldToScreenPoint(point);
              if (screenPoint.z > 0)
              {
                Vector2 gameUiPoint = gameUiMain.UnityScreenPointToGameUiPoint(screenPoint);
                reportResult(JsonUtility.ToJson(gameUiPoint));
              }
              else
              {
                reportResult("null");
              }
              break;
            }

          case "ProjectSphere":
            {
              Camera cam = GetUserMain().GetCamera();
              SphereArg inSphere = JsonUtility.FromJson<SphereArg>(argsJson);
              Vector3 screenCenter = cam.WorldToScreenPoint(inSphere.center);
              if (screenCenter.z > 0)
              {
                Vector3 centerGameUi = gameUiMain.UnityScreenPointToGameUiPoint(screenCenter);
                Vector3 rightPoint = cam.WorldToScreenPoint(inSphere.center + cam.transform.right * inSphere.radius);
                Vector3 rightPointGameUi = gameUiMain.UnityScreenPointToGameUiPoint(rightPoint);
                SphereArg outSphere = new SphereArg
                {
                  center = centerGameUi,
                  radius = Mathf.Abs(rightPointGameUi.x - centerGameUi.x)
                };
                reportResult(JsonUtility.ToJson(outSphere));
              }
              else
              {
                reportResult("null");
              }
              break;
            }
          case "GetActorScreenRect":
            {
              string actorName = JsonUtility.FromJson<GetActorScreenRectRequest>(argsJson).actor;
              VoosActor actor = engine.GetActor(actorName);
              Bounds worldBounds = new Bounds(actor.GetWorldRenderBoundsCenter(), actor.GetWorldRenderBoundsSize());

              // Depending on the orientation, any of the 8 corners of the world-space bounding box
              // can contribute to the screen-space bounding box, so we have to go through them all.
              Bounds screenBounds = new Bounds();
              bool success = true;
              for (int i = 0; i < 8; i++)
              {
                Vector3 worldPoint = new Vector3(
                  (i & 1) > 0 ? worldBounds.min.x : worldBounds.max.x,
                  (i & 2) > 0 ? worldBounds.min.y : worldBounds.max.y,
                  (i & 4) > 0 ? worldBounds.min.z : worldBounds.max.z);
                Vector3 screenPoint = GetUserMain().GetCamera().WorldToScreenPoint(worldPoint);
                if (screenPoint.z < 0)
                {
                  // Off-screen (behind camera).
                  success = false;
                  break;
                }
                Vector2 gameUiPoint = gameUiMain.UnityScreenPointToGameUiPoint(screenPoint);
                if (i == 0)
                {
                  // Note: due to the Bounds() constructor assuming Vector3.zero as the center
                  // (known Unity bug), we have to reinitialize it here:
                  screenBounds = new Bounds(gameUiPoint, Vector3.zero);
                }
                else
                {
                  screenBounds.Encapsulate(gameUiPoint);
                }
              }
              reportResult(success ? JsonUtility.ToJson(new GetActorScreenRectResponse
              {
                x = screenBounds.min.x,
                y = screenBounds.min.y,
                w = screenBounds.size.x,
                h = screenBounds.size.y
              }) : "null");
              break;
            }

          case "GetCameraInfo":
            {
              Transform cameraTransform = GetUserMain().GetCamera().transform;
              CameraInfo info = new CameraInfo
              {
                pos = cameraTransform.position,
                rot = cameraTransform.rotation
              };
              reportResult(JsonUtility.ToJson(info));
              break;
            }

          case "ReportBehaviorException":
            {
              Util.LogError($"ReportBehaviorException {argsJson}");
              VoosEngine.BehaviorLogItem e = JsonUtility.FromJson<VoosEngine.BehaviorLogItem>(argsJson);
              engine.HandleBehaviorException(e);
              reportResult("true");
              break;
            }

          case "LogBehaviorMessage":
            {
              Util.Log($"LogBehaviorMessage {argsJson}");
              VoosEngine.BehaviorLogItem msg = JsonUtility.FromJson<VoosEngine.BehaviorLogItem>(argsJson);
              engine.HandleBehaviorLogMessage(msg);
              reportResult("true");
              break;
            }

          case "GetCameraActor":
            {
              string cameraActorName = GetUserMain().GetCameraActor()?.GetName();
              reportResult(cameraActorName == null ? "null" : ("\"" + cameraActorName + "\""));
              break;
            }

          case "RequestTempCameraOffset":
            {
              TempCameraOffsetRequest request = JsonUtility.FromJson<TempCameraOffsetRequest>(argsJson);
              if (request.actor == GetUserMain().GetPlayerActor()?.GetName())
              {
                GetUserMain().GetNavigationControls().RequestTemporaryCameraOffset(request.offset);
              }
              reportResult("true");
              break;
            }

          case "GetScreenInfo":
            {
              reportResult(JsonUtility.ToJson(gameUiMain.GetScreenInfoForScript()));
              break;
            }

          case "SetSkyType":
            {
              JsonWrapper<string> request = JsonUtility.FromJson<JsonWrapper<string>>(argsJson);
              GameBuilderStage.SkyType skyType;
              if (Util.TryParseEnum(request.value ?? "", out skyType, true))
              {
                gbStage.SetSkyType(skyType);
                reportResult("true");
              }
              else
              {
                Debug.LogError("Invalid sky type requested: " + request.value);
                reportResult("false");
              }
              break;
            }

          case "GetSkyType":
            {
              reportResult(JsonUtility.ToJson(JsonWrapper<string>.Wrap(gbStage.GetSkyType().ToString())));
              break;
            }

          case "SetSkyColor":
            {
              JsonWrapper<Color> request = JsonUtility.FromJson<JsonWrapper<Color>>(argsJson);
              gbStage.SetSkyColor(request.value);
              reportResult("true");
              break;
            }

          case "GetSkyColor":
            {
              reportResult(JsonUtility.ToJson(JsonWrapper<Color>.Wrap(gbStage.GetSkyColor())));
              break;
            }

          case "SetSceneLighting":
            {
              JsonWrapper<string> request = JsonUtility.FromJson<JsonWrapper<string>>(argsJson);
              GameBuilderStage.SceneLightingMode sceneLightingMode;
              if (Util.TryParseEnum<GameBuilderStage.SceneLightingMode>(request.value, out sceneLightingMode, ignoreCase: true))
              {
                gbStage.SetSceneLightingMode(sceneLightingMode);
                reportResult("true");
              }
              else
              {
                Debug.LogError("Invalid scene lighting mode: " + request.value);
                reportResult("false");
              }
              break;
            }

          case "GetSceneLighting":
            {
              reportResult(JsonUtility.ToJson(JsonWrapper<string>.Wrap(gbStage.GetSceneLightingMode().ToString().ToUpperInvariant())));
              break;
            }

          default:
            Util.LogError($"VOOS script tried to call unknown service {serviceName}.");
            break;
        }
      }
      catch (System.Exception e)
      {
        // We cannot let exceptions escape. It will tend to crash the process.
        Util.LogError($"Exception during CallService({serviceName}):\n{e}");
        reportResult("false");
      }
    }

    private UserMain GetUserMain()
    {
      // Sadly we need to do this lazily instead receiving this in the constructor because
      // UserMain is only instantiated after we are constructured.
      if (userMainLazy == null)
      {
        userMainLazy = GameObject.FindObjectOfType<UserMain>();
        if (userMainLazy == null)
        {
          throw new System.Exception("Services could not find UserMain.");
        }
      }
      return userMainLazy;
    }

    private struct RaycastHitDistanceComparer : IComparer<RaycastHit>
    {
      public int Compare(RaycastHit a, RaycastHit b)
      {
        return a.distance.CompareTo(b.distance);
      }
    }
  }
}