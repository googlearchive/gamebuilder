// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace PolyToolkitInternal {
/// <summary>
/// Utility class that converts an object to string by reflection, for debug purposes.
///
/// To use it, simply annotate your type with AutoStringifiable and add a ToString method
/// that calls this class:
///
///     [AutoStringifiable]
///     public class Foo {
///      private int age;
///      private string name;
///      private List<string> favoriteColors;
///
///      public override string ToString() {
///        AutoStringify.Stringify(this);
///      }
///     }
///
/// This will result in something like:
///     [Foo]
///       age: 35,
///       name: "Bruno Oliveira",
///       favoriteColors: List`1
///         [0]: blue
///         [1]: red
///         [2]: green
///
/// It will also recursively stringify any contained types marked as AutoStringifiable.
/// If a given field is too large to go in the string, you can mark it as AutoStringifyAbridged:
///
///     [AutoStringifyAbridged]
///     public string reallyLongText;
///
/// And then it will come out just as type + length:
///
///     reallyLongText: string (length: 38142)
///
/// Same thing works for arrays:
///
///     [AutoStringifyAbridged]
///     public byte[] body;
///
/// Will generate:
///
///     body: byte[] (length: 154864)
///
/// </summary>
public static class AutoStringify {
  private const string INDENT = "  ";

  /// <summary>
  /// Converts the given object to a string by reflection. The object must be annotated with
  /// the AutoStringifiable annotation.
  /// </summary>
  /// <param name="obj">The object to convert.</param>
  /// <param name="indent">The string to use as indentation (optional).</param>
  /// <returns>The string that represents the object.</returns>
  public static string Stringify(object obj, string indent = "") {
    if (obj != null && !TypeIsStringifiable(obj.GetType())) {
      throw new ArgumentException("Can't stringify a type that's not marked AutoStringifiable: " + obj.GetType().Name);
    }
    return Stringify(obj, indent, objectsOnCallStack: new HashSet<object>()).Trim();
  }

  /// <summary>
  /// Stringify an object (recursive).
  /// </summary>
  /// <param name="obj">The object to stringify</param>
  /// <param name="indent">The indent to use.</param>
  /// <param name="objectsOnCallStack">The "call stack" of objects we are stringifying, so we can detect
  /// circular references and avoid infinite recursion.</param>
  /// <returns>The stringified object.</returns>
  private static string Stringify(object obj, string indent, HashSet<object> objectsOnCallStack) {
    if (obj == null) return "(null)\n";
    StringBuilder sb = new StringBuilder();

    // If this is an array or a collection, print each element.
    if (obj is Array || (obj is ICollection && obj is IEnumerable)) {
      // NOTE: we can't just test for "is IEnumerable" because strings are IEnumerable, but we wouldn't
      // want to print them as a collection of characters.
      IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
      objectsOnCallStack.Add(obj);
      sb.AppendLine(obj.GetType().Name);
      int index = 0;
      while (enumerator.MoveNext()) {
        sb.Append(indent).Append("[").Append(index).Append("]: ")
            .Append(Stringify(enumerator.Current, indent + INDENT, objectsOnCallStack));
        ++index;
      }
      objectsOnCallStack.Remove(obj);
      return sb.ToString();
    }

    // If this object is NOT marked as Stringifiable, just call its standard ToString() method.
    Type type = obj.GetType();
    if (!TypeIsStringifiable(type)) {
      return obj.ToString() + "\n";
    }

    sb.Append("[").Append(type.Name).AppendLine("]");

    if (objectsOnCallStack.Contains(obj)) {
      // Avoid infinite recursion on circular references.
      sb.Append(indent).AppendLine("*** (circular ref)");
      return sb.ToString();
    }

    // Print every field.
    foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Instance |
        BindingFlags.NonPublic | BindingFlags.Public)) {
      if (fieldInfo.MemberType != MemberTypes.Field) continue;
      if (fieldInfo.IsStatic) continue;
      object value = fieldInfo.GetValue(obj);
      sb.Append(indent).Append(fieldInfo.Name).Append(": ");
      if (value == null) {
        sb.AppendLine("(null)");
        continue;
      }
      if (IsFieldAbridged(fieldInfo)) {
        // Field marked as abridged. Just say the type.
        sb.Append(value.GetType().Name);
        if (value is string) {
          // If it's a string, also say the length of the string.
          sb.AppendFormat(" (length: {0})", ((string) value).Length);
        } else if (value is Array) {
          // If it's an array, also say the length of the array.
          sb.AppendFormat(" (length: {0})", ((Array) value).Length);
        }
        sb.AppendLine();
        continue;
      }

      objectsOnCallStack.Add(obj);
      sb.Append(Stringify(value, indent + INDENT, objectsOnCallStack));
      objectsOnCallStack.Remove(obj);
    }
    return sb.ToString();
  }

  private static bool TypeIsStringifiable(Type type) {
    return TypeHasAttribute(type, typeof(AutoStringifiable));
  }

  private static bool IsFieldAbridged(FieldInfo info) {
    object[] attribs = info.GetCustomAttributes(typeof(AutoStringifyAbridged), inherit: true);
    bool abridged = (attribs != null && attribs.Length > 0);
    return abridged;
  }

  private static bool TypeHasAttribute(Type type, Type attributeType) {
   object[] attribs = type.GetCustomAttributes(attributeType, inherit: true);
   return (attribs != null && attribs.Length > 0);
  }
}

/// <summary>
/// Attribute that marks a type as auto-stringifiable.
/// </summary>
public class AutoStringifiable : Attribute {}

/// <summary>
/// Attribute for fields in AutoStringifiable types. Indicates that the field should be stringified
/// in "abridged" mode (not full contents), even if the type is auto-stringifiable.
/// </summary>
public class AutoStringifyAbridged : Attribute {}

}
