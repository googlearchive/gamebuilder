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

namespace GameBuilder
{
  [System.Serializable]
  public struct Int3
  {
    public int x;
    public int y;
    public int z;

    public int this[int index]
    {
      get
      {
        switch (index)
        {
          case 0:
            return x;
          case 1:
            return y;
          default:
            return z;
        }
      }
      set
      {
        switch (index)
        {
          case 0:
            x = value; break;
          case 1:
            y = value; break;
          default:
            z = value; break;
        }
      }
    }

    public static Int3 zero()
    {
      return new Int3(0, 0, 0);
    }

    public static Int3 one()
    {
      return new Int3(1, 1, 1);
    }

    public static bool operator ==(Int3 lhs, Int3 rhs)
    {
      return lhs[0] == rhs[0] && lhs[1] == rhs[1] && lhs[2] == rhs[2];
    }
    public static bool operator !=(Int3 lhs, Int3 rhs)
    {
      return !(lhs == rhs);
    }

    public override string ToString()
    {
      return $"{x}, {y}, {z}";
    }

    // [a,b)
    public static Int3 RandomBetween(Int3 a, Int3 b)
    {
      return new Int3(
        Random.Range(a.x, b.x),
        Random.Range(a.y, b.y),
        Random.Range(a.z, b.z));
    }
    public static Int3 RandomUpto(Int3 b)
    {
      return new Int3(
        Random.Range(0, b.x),
        Random.Range(0, b.y),
        Random.Range(0, b.z));
    }

    public Int3(int x, int y, int z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    public Int3(int xyz)
    {
      this.x = xyz;
      this.y = xyz;
      this.z = xyz;
    }

    public Vector2 ToVec2()
    {
      return new Vector2(x, y);
    }

    public Vector3 ToVec3()
    {
      return new Vector3(x, y, z);
    }

    public Int3 WithY(int newY)
    {
      return new Int3(x, newY, z);
    }

    public Int3 WithX(int newX)
    {
      return new Int3(newX, y, z);
    }

    public Int3(int x, int y) : this()
    {
      this.x = x;
      this.y = y;
      this.z = 0;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is Int3))
      {
        return false;
      }

      var @int = (Int3)obj;
      return x == @int.x &&
             y == @int.y &&
             z == @int.z;
    }

    public override int GetHashCode()
    {
      var hashCode = 373119288;
      hashCode = hashCode * -1521134295 + x.GetHashCode();
      hashCode = hashCode * -1521134295 + y.GetHashCode();
      hashCode = hashCode * -1521134295 + z.GetHashCode();
      return hashCode;
    }

    public static Int3 operator +(Int3 a, Int3 b)
    {
      return new Int3(
        a.x + b.x,
        a.y + b.y,
        a.z + b.z);
    }

    public static Int3 operator -(Int3 a, Int3 b)
    {
      return new Int3(
        a.x - b.x,
        a.y - b.y,
        a.z - b.z);
    }

    public static Int3 operator *(int s, Int3 a)
    {
      return new Int3(s * a.x, s * a.y, s * a.z);
    }

    public static Int3 operator *(Int3 a, int s)
    {
      return new Int3(s * a.x, s * a.y, s * a.z);
    }

    // Component-wise multiply.
    public static Int3 operator *(Int3 a, Int3 b)
    {
      return new Int3(b.x * a.x, b.y * a.y, b.z * a.z);
    }

    // Integer division
    public static Int3 operator /(Int3 a, int s)
    {
      return new Int3(a.x / s, a.y / s, a.z / s);
    }

    public static Int3 Min(Int3 a, Int3 b)
    {
      return new Int3(
        Mathf.Min(a.x, b.x),
        Mathf.Min(a.y, b.y),
        Mathf.Min(a.z, b.z));
    }

    public static Int3 Max(Int3 a, Int3 b)
    {
      return new Int3(
        Mathf.Max(a.x, b.x),
        Mathf.Max(a.y, b.y),
        Mathf.Max(a.z, b.z));
    }

    public static Vector3 operator /(Int3 a, float s)
    {
      return new Vector3(a.x / s, a.y / s, a.z / s);
    }

    public static Vector3 operator *(float s, Int3 a)
    {
      return new Vector3(s * a.x, s * a.y, s * a.z);
    }

    public static bool operator <(Int3 a, Int3 b)
    {
      return a.x < b.x
        && a.y < b.y
        && a.z < b.z;
    }
    public static bool operator >(Int3 a, Int3 b)
    {
      return a.x > b.x
        && a.y > b.y
        && a.z > b.z;
    }
    public static bool operator <=(Int3 a, Int3 b)
    {
      return a.x <= b.x
        && a.y <= b.y
        && a.z <= b.z;
    }

    public static bool operator >=(Int3 a, Int3 b)
    {
      return a.x >= b.x
        && a.y >= b.y
        && a.z >= b.z;
    }

    public static Int3 Floor(Vector3 v)
    {
      return new Int3(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z));
    }

    public static IEnumerable<Int3> Enumerate(Int3 mins, Int3 maxsExclusive)
    {
      for (int x = mins.x; x < maxsExclusive.x; x++)
      {
        for (int y = mins.y; y < maxsExclusive.y; y++)
        {
          for (int z = mins.z; z < maxsExclusive.z; z++)
          {
            yield return new Int3(x, y, z);
          }
        }
      }
    }

    public static IEnumerable<Int3> Enumerate(Int3 maxsExclusive)
    {
      foreach (Int3 u in Enumerate(zero(), maxsExclusive))
      {
        yield return u;
      }
    }

    public static IEnumerable<Int3> EnumerateXY(Int3 mins, Int3 maxsExclusive)
    {
      for (int x = mins.x; x < maxsExclusive.x; x++)
      {
        for (int y = mins.y; y < maxsExclusive.y; y++)
        {
          yield return new Int3(x, y);
        }
      }
    }

    public static System.Collections.Generic.IEnumerable<Int3> EnumerateXY(Int3 maxsExclusive)
    {
      foreach (Int3 u in EnumerateXY(zero(), maxsExclusive))
      {
        yield return u;
      }
    }

    public struct Range
    {
      public Int3 mins;
      public Int3 maxs;

      public Range(Int3 mins, Int3 exclusiveMaxs)
      {
        this.mins = mins;
        this.maxs = exclusiveMaxs;
      }

      public Range(Int3 exclusiveMaxs)
      {
        this.mins = Int3.zero();
        this.maxs = exclusiveMaxs;
      }

      public override bool Equals(object obj)
      {
        if (!(obj is Range))
        {
          return false;
        }

        var range = (Range)obj;
        return EqualityComparer<Int3>.Default.Equals(mins, range.mins) &&
               EqualityComparer<Int3>.Default.Equals(maxs, range.maxs);
      }

      public override int GetHashCode()
      {
        var hashCode = 574072426;
        hashCode = hashCode * -1521134295 + EqualityComparer<Int3>.Default.GetHashCode(mins);
        hashCode = hashCode * -1521134295 + EqualityComparer<Int3>.Default.GetHashCode(maxs);
        return hashCode;
      }
    }
  }

  public class Int3Comparer : IEqualityComparer<Int3>
  {
    public bool Equals(Int3 x, Int3 y)
    {
      return x == y;
    }

    public int GetHashCode(Int3 obj)
    {
      return obj.GetHashCode();
    }
  }
}