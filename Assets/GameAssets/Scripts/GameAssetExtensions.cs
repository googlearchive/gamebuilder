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

public static class GameAssetExtensions
{
    public static Vector3 WithY(this Vector3 v, float y)
    {
        return new Vector3(v.x, y, v.z);
    }

    public static Vector3 GetRight(this Vector3 lookForward)
    {
        Quaternion lookRot = Quaternion.LookRotation(lookForward, Vector3.up);
        return lookRot * Vector3.right;
    }

    // Essentially, the vector projected onto the XZ plane.
    public static Vector3 GetForwardHeading(this Vector3 lookForward)
    {
        return lookForward.WithY(0f).normalized;
    }

    public static Vector3 GetLeft(this Vector3 lookForward)
    {
        Quaternion lookRot = Quaternion.LookRotation(lookForward, Vector3.up);
        return lookRot * Vector3.left;
    }

    public static Vector3 AsXZVec(this Vector2 v)
    {
        return new Vector3(v.x, 0f, v.y);
    }

    public static Vector2 GetXZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static Vector3 TransformOffset(this Transform t, Vector3 o)
    {
        return t.TransformDirection(o.normalized) * o.magnitude;
    }
}