using System.Collections;
using System.Collections.Generic;

public static partial class Util
{
  [System.Serializable]
  public class Tuple<T1, T2>
  {
    public T1 first;
    public T2 second;

    private static readonly IEqualityComparer Item1Comparer = EqualityComparer<T1>.Default;
    private static readonly IEqualityComparer Item2Comparer = EqualityComparer<T2>.Default;

    public Tuple(T1 first, T2 second)
    {
      this.first = first;
      this.second = second;
    }

    public override string ToString()
    {
      return string.Format("<{0}, {1}>", first, second);
    }

    public static bool operator ==(Tuple<T1, T2> a, Tuple<T1, T2> b)
    {
      if (Tuple<T1, T2>.IsNull(a) && !Tuple<T1, T2>.IsNull(b))
        return false;

      if (!Tuple<T1, T2>.IsNull(a) && Tuple<T1, T2>.IsNull(b))
        return false;

      if (Tuple<T1, T2>.IsNull(a) && Tuple<T1, T2>.IsNull(b))
        return true;

      return
          a.first.Equals(b.first) &&
          a.second.Equals(b.second);
    }

    public static bool operator !=(Tuple<T1, T2> a, Tuple<T1, T2> b)
    {
      return !(a == b);
    }

    public override int GetHashCode()
    {
      int hash = 17;
      hash = hash * 23 + first.GetHashCode();
      hash = hash * 23 + second.GetHashCode();
      return hash;
    }

    public override bool Equals(object obj)
    {
      var other = obj as Tuple<T1, T2>;
      if (object.ReferenceEquals(other, null))
        return false;
      else
        return Item1Comparer.Equals(first, other.first) &&
              Item2Comparer.Equals(second, other.second);
    }

    private static bool IsNull(object obj)
    {
      return object.ReferenceEquals(obj, null);
    }
  }
}