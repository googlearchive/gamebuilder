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

// This is a fake, stubbed version of PhotonView. If you actually install PUN
// Classic, definitely overwrite this with the real one. We need to have it here
// so references in our prefabs are preserved.

using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public class PhotonView : MonoBehaviour
{
  public bool isMine { get { return true; } }
  public int viewID = 0;
  public int ownerId = 0;
  internal PhotonPlayer owner = PhotonNetwork.masterClient;

  public void RPC(string methodName, PhotonTargets target, params object[] rpcParams)
  {
    switch (target)
    {
      case PhotonTargets.All:
      case PhotonTargets.AllViaServer:
      case PhotonTargets.MasterClient:
        bool called = false;
        foreach (MonoBehaviour behavior in this.gameObject.GetComponents<MonoBehaviour>())
        {
          if (behavior == null) continue;
          System.Type behType = behavior.GetType();
          MethodInfo rpcMethod = behType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
          if (rpcMethod != null)
          {
            var invokeParams = new List<object>();
            invokeParams.AddRange(rpcParams);

            // If last expected arg is PhotonMessageInfo..
            var methodParams = rpcMethod.GetParameters();
            if (methodParams.Length > 0 && methodParams.GetLast().ParameterType == typeof(PhotonMessageInfo))
            {
              invokeParams.Add(new PhotonMessageInfo { sender = PhotonNetwork.masterClient });
            }

            Debug.Assert(methodParams.Length == invokeParams.Count);

            rpcMethod.Invoke(behavior, invokeParams.ToArray());
            called = true;
            break;
          }
        }
        Debug.Assert(called, $"Failed to call RPC method named: {methodName}");
        break;
      default:
        break;
    }
  }

  public void RPC(string methodName, PhotonPlayer target, params object[] rest)
  {
    if (target == PhotonNetwork.masterClient)
    {
      this.RPC(methodName, PhotonTargets.MasterClient, rest);
    }
  }

  public static PhotonView Get(UnityEngine.MonoBehaviour b) { return b.gameObject.GetComponent<PhotonView>(); }

  internal void TransferOwnership(int iD) { }
}
