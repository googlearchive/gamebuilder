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
#if USE_PUN
using ExitGames.Client.Photon;
using UnityEngine;
using System.Runtime.InteropServices;
public static class VoosNetworkTypes
{

  private static short SerializeColor32(StreamBuffer outStream, object customobject)
  {
    Color32 color = (Color32)customobject;
    outStream.WriteByte(color.r);
    outStream.WriteByte(color.g);
    outStream.WriteByte(color.b);
    outStream.WriteByte(color.a);
    return 4;
  }

  private static object DeserializeColor32(StreamBuffer inStream, short length)
  {
    Color32 color = new Color32();
    color.r = (byte)inStream.ReadByte();
    color.g = (byte)inStream.ReadByte();
    color.b = (byte)inStream.ReadByte();
    color.a = (byte)inStream.ReadByte();
    return color;
  }

  private static short SerializeColor(StreamBuffer outStream, object customobject)
  {
    Color color = (Color)customobject;

    Debug.Assert(color.r <= 1f, "We currently do not support HDR colors (ie. component >1)");
    Debug.Assert(color.g <= 1f, "We currently do not support HDR colors (ie. component >1)");
    Debug.Assert(color.b <= 1f, "We currently do not support HDR colors (ie. component >1)");
    Debug.Assert(color.a <= 1f, "We currently do not support HDR colors (ie. component >1)");

    outStream.WriteByte((byte)Mathf.RoundToInt(color.r * 255));
    outStream.WriteByte((byte)Mathf.RoundToInt(color.g * 255));
    outStream.WriteByte((byte)Mathf.RoundToInt(color.b * 255));
    outStream.WriteByte((byte)Mathf.RoundToInt(color.a * 255));
    return 4;
  }

  private static object DeserializeColor(StreamBuffer inStream, short length)
  {
    Color color = new Color();
    color.r = inStream.ReadByte() / 255f;
    color.g = inStream.ReadByte() / 255f;
    color.b = inStream.ReadByte() / 255f;
    color.a = inStream.ReadByte() / 255f;
    return color;
  }

  [StructLayout(LayoutKind.Explicit)]
  public struct ShortToBytes
  {
    [FieldOffset(0)] public short data;
    [FieldOffset(0)] public byte byte0;
    [FieldOffset(1)] public byte byte1;
  }

  [StructLayout(LayoutKind.Explicit)]
  public struct UintToBytes
  {
    [FieldOffset(0)] public uint data;
    [FieldOffset(0)] public byte byte0;
    [FieldOffset(1)] public byte byte1;
    [FieldOffset(2)] public byte byte2;
    [FieldOffset(3)] public byte byte3;

    public void WriteUint24(List<byte> bytes, uint val)
    {
      this.data = val;
      bytes.Add(byte0);
      bytes.Add(byte1);
      bytes.Add(byte2);
    }

    public uint ReadUint24(byte[] bytes, ref int offset)
    {
      this.byte0 = bytes[offset];
      this.byte1 = bytes[offset + 1];
      this.byte2 = bytes[offset + 2];
      offset += 3;
      return this.data;
    }

    public void Write(List<byte> bytes, uint val)
    {
      this.data = val;
      bytes.Add(byte0);
      bytes.Add(byte1);
      bytes.Add(byte2);
      bytes.Add(byte3);
    }

    public uint Read(byte[] bytes, ref int offset)
    {
      this.byte0 = bytes[offset];
      this.byte1 = bytes[offset + 1];
      this.byte2 = bytes[offset + 2];
      this.byte3 = bytes[offset + 3];
      offset += 4;
      return this.data;
    }
  }

  [StructLayout(LayoutKind.Explicit)]
  public struct FloatToBytes
  {
    [FieldOffset(0)] public float data;
    [FieldOffset(0)] public byte byte0;
    [FieldOffset(1)] public byte byte1;
    [FieldOffset(2)] public byte byte2;
    [FieldOffset(3)] public byte byte3;

    public void SerializeTo(List<byte> bytes, float val)
    {
      this.data = val;
      bytes.Add(byte0);
      bytes.Add(byte1);
      bytes.Add(byte2);
      bytes.Add(byte3);
    }

    public float DeserializeFrom(byte[] bytes, ref int offset)
    {
      this.byte0 = bytes[offset];
      this.byte1 = bytes[offset + 1];
      this.byte2 = bytes[offset + 2];
      this.byte3 = bytes[offset + 3];
      offset += 4;
      return this.data;
    }
  }

  public static uint CompressFloatToUint24(float f)
  {
    return (uint)(Mathf.Clamp(f * 512f + 8388607, 0, 16777214));
  }
  public static float DecompressFloatFromUint24(uint data)
  {
    return ((int)data - 8388607) / 512.0f;
  }
  /// 24 bit vector3.  This is a trivial optimization, and not amazing in terms of commpression.
  private static short SerializeVector3Compressed(StreamBuffer outStream, object customobject)
  {
    Vector3 v = (Vector3)customobject;
    /// Short sacrifices one value (-32768) - but encodes 0 simply and precisely
    UintToBytes qV = new UintToBytes();
    qV.data = CompressFloatToUint24(v.x);
    outStream.WriteBytes(qV.byte0, qV.byte1, qV.byte2);
    qV.data = CompressFloatToUint24(v.y);
    outStream.WriteBytes(qV.byte0, qV.byte1, qV.byte2);
    qV.data = CompressFloatToUint24(v.z);
    outStream.WriteBytes(qV.byte0, qV.byte1, qV.byte2);

    return 9;
  }

  private static object DeserializeVector3Compressed(StreamBuffer inStream, short length)
  {
    Vector3 v = new Vector3();
    UintToBytes qV = new UintToBytes();
    qV.byte0 = (byte)inStream.ReadByte();
    qV.byte1 = (byte)inStream.ReadByte();
    qV.byte2 = (byte)inStream.ReadByte();
    v.x = DecompressFloatFromUint24(qV.data);

    qV.byte0 = (byte)inStream.ReadByte();
    qV.byte1 = (byte)inStream.ReadByte();
    qV.byte2 = (byte)inStream.ReadByte();
    v.y = DecompressFloatFromUint24(qV.data);

    qV.byte0 = (byte)inStream.ReadByte();
    qV.byte1 = (byte)inStream.ReadByte();
    qV.byte2 = (byte)inStream.ReadByte();
    v.z = DecompressFloatFromUint24(qV.data);

    return v;
  }

  public delegate bool RegisterTypeFunc(System.Type customType, byte code, SerializeStreamMethod serializeMethod, DeserializeStreamMethod constructor);

  /// <summary>Register</summary>
  public static void Register(RegisterTypeFunc register)
  {
    register(typeof(Color), (byte)'O', SerializeColor, DeserializeColor);
    register(typeof(Color32), (byte)'C', SerializeColor32, DeserializeColor32);
    register(typeof(Vector3), (byte)'V', SerializeVector3Compressed, DeserializeVector3Compressed);
  }
}
#endif