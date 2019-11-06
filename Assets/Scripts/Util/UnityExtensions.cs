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
using NET = UnityEngine.Networking;

public static class UnityExtensions
{
  public static Vector3 Abs(this Vector3 v)
  {
    return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
  }

  public static Vector2 XZ(this Vector3 v)
  {
    return new Vector2(v.x, v.z);
  }

  public static float MaxAbsComponent(this Vector3 v)
  {
    return Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
  }

  public static float MinAbsComponent(this Vector3 v)
  {
    return Mathf.Min(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
  }

  public static string ToFourDecimalPlaces(this float f)
  {
    return System.String.Format("{0:0.####}", f);
  }

  public static string ToTwoDecimalPlaces(this float f)
  {
    return System.String.Format("{0:0.##}", f);
  }

  public static float Quantize(this float x, float step)
  {
    return Mathf.Round(x / step) * step;
  }

  public static int Quantize(this int x, int step)
  {
    return Mathf.RoundToInt(Mathf.Round(x * 1f / step) * step);
  }

  public static string ToFourDecimalPlaces(this Vector3 v)
  {
    return $"({v.x.ToFourDecimalPlaces()}, {v.y.ToFourDecimalPlaces()}, {v.z.ToFourDecimalPlaces()})";
  }

  public static string ToFourDecimalPlaces(this Quaternion q)
  {
    return $"({q.x.ToFourDecimalPlaces()}, {q.y.ToFourDecimalPlaces()}, {q.z.ToFourDecimalPlaces()}, {q.w.ToFourDecimalPlaces()})";
  }

  const byte VN_EMPTY = 0;
  const byte VN_GUID = 1;
  const byte VN_UTF16 = 2;

  // A "Voos Name" may be a 32-char GUID or just some string, like
  // __DEFAULT_BEHAVIOR__
  public static void WriteVoosName(this NET.NetworkWriter writer, string name)
  {
    if (name.IsNullOrEmpty())
    {
      writer.Write(VN_EMPTY);
      return;
    }

    System.Guid guid;
    if (System.Guid.TryParseExact(name, "N", out guid))
    {
      writer.Write(VN_GUID); // Indicate GUID
      byte[] bytes = guid.ToByteArray();
      Debug.Assert(bytes.Length == 16);
      for (int i = 0; i < 16; i++)
      {
        writer.Write(bytes[i]);
      }
    }
    else
    {
      // Some string, maybe null/empty
      writer.Write(VN_UTF16);
      writer.WriteUtf16(name);
    }
  }

  // A "Voos Name" may be a 32-char GUID or just some string, like
  // __DEFAULT_BEHAVIOR__
  public static string ReadVoosName(this NET.NetworkReader reader)
  {
    byte header = reader.ReadByte();

    if (header == VN_EMPTY)
    {
      return "";
    }

    if (header == VN_GUID)
    {
      // Guid
      byte[] bytes = new byte[16];
      for (int i = 0; i < 16; i++)
      {
        bytes[i] = reader.ReadByte();
      }
      return (new System.Guid(bytes)).ToString("N");
    }
    else
    {
      Debug.Assert(header == VN_UTF16);
      return reader.ReadUtf16();
    }
  }

  public static void WriteUtf16(this UnityEngine.Networking.NetworkWriter writer, string s)
  {
    if (s == null)
    {
      writer.Write((ushort)0);
      return;
    }

    if (s.Length >= 65535)
    {
      throw new System.Exception("We do not support serializing strings of length beyond 65k chars");
    }
    writer.Write((ushort)s.Length);
    for (int i = 0; i < s.Length; i++)
    {
      writer.Write((ushort)s[i]);
    }
  }

  private static System.Text.StringBuilder ReadUtf16Builder = new System.Text.StringBuilder();

  // NOTE: Not thread-safe.
  public static string ReadUtf16(this UnityEngine.Networking.NetworkReader reader)
  {
    var builder = ReadUtf16Builder;
    builder.Clear();
    ushort length = reader.ReadUInt16();
    // TODO this could be more efficient..with ReadBytes or something.
    for (int i = 0; i < length; i++)
    {
      char c = (char)reader.ReadUInt16();
      builder.Append(c);
    }

    return builder.ToString();
  }

  public static void WriteColor(this UnityEngine.Networking.NetworkWriter writer, Color c)
  {
    writer.Write(c.r);
    writer.Write(c.g);
    writer.Write(c.b);
    writer.Write(c.a);
  }

  public static Color ReadColor(this UnityEngine.Networking.NetworkReader reader)
  {
    return new Color(
      reader.ReadSingle(),
      reader.ReadSingle(),
      reader.ReadSingle(),
      reader.ReadSingle());
  }

  public static void WriteVoosBoolean(this UnityEngine.Networking.NetworkWriter writer, bool val)
  {
    writer.Write((byte)(val ? 1 : 0));
  }

  public static bool ReadVoosBoolean(this UnityEngine.Networking.NetworkReader reader)
  {
    return reader.ReadByte() == 1;
  }

  public static void WriteVoosVector3(this UnityEngine.Networking.NetworkWriter writer, Vector3 v)
  {
    writer.Write(v.x);
    writer.Write(v.y);
    writer.Write(v.z);
  }

  public static Vector3 ReadVoosVector3(this UnityEngine.Networking.NetworkReader reader)
  {
    return new Vector3(
      reader.ReadSingle(),
      reader.ReadSingle(),
      reader.ReadSingle()
    );
  }

  public static void SetAllMaterialsToShared(this GameObject root, Material sharedMaterial)
  {
    foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
    {
      Material[] mats = r.sharedMaterials;
      for (int i = 0; i < mats.Length; i++)
      {
        mats[i] = sharedMaterial;
      }
      r.sharedMaterials = mats;
    }
  }

  /**
   Example:
   
    var screenRect = GetComponent<RectTransform>().ToScreenSpace();
    GUILayout.BeginArea(screenRect);
    GUILayout.Button("Foo");
    GUILayout.EndArea();
   */
  public static Rect ToScreenSpace(this RectTransform transform)
  {
    Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
    Rect rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.x, size.y);
    rect.x -= (transform.pivot.x * size.x);

    rect.y -= ((1.0f - transform.pivot.y) * size.y);
    return rect;
  }

  public static Vector3 Inverse(this Vector3 u)
  {
    return new Vector3(1f / u.x, 1f / u.y, 1f / u.z);
  }

  public static Vector3 DivideComponents(this Vector3 u, Vector3 v)
  {
    return new Vector3(u.x / v.x, u.y / v.y, u.z / v.z);
  }

  public static Vector3 Remainder(this Vector3 u, float s)
  {
    return new Vector3(u.x % s, u.y % s, u.z % s);
  }

  public static void DestroyAllAndClear(this ICollection<GameObject> gameObjects)
  {
    foreach (var go in gameObjects)
    {
      GameObject.Destroy(go);
    }
    gameObjects.Clear();
  }

  public static string GetFullPath(this GameObject go)
  {
    string rv = go.name;
    Transform curr = go.transform.parent;
    while (curr != null)
    {
      rv = curr.name + "/" + rv;
      curr = curr.parent;
    }
    return rv;
  }

  public static bool ApproxEquals(this Quaternion a, Quaternion b)
  {
    float angleA, angleB;
    Vector3 axisA, axisB;
    a.ToAngleAxis(out angleA, out axisA);
    b.ToAngleAxis(out angleB, out axisB);
    return (axisA - axisB).magnitude < 1e-4
     && Mathf.Abs(angleA - angleB) < 1e-4;
  }

  public static Vector4 ToHomogeneousPosition(this Vector3 p)
  {
    return new Vector4(p.x, p.y, p.z, 1f);
  }
}