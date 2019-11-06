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

using UnityEngine;

// A simple, non-allocating but very limited rich text parser.
//
// To use it, call Parse() on the string you want to parse, then call GetExtentCount() to figure
// out how many output extents there were, and GetExtentAt(index) to get each extent.
public class SimpleRichTextParser
{
  public struct TextExtent
  {
    // Index of the start of this extent in the original string.
    public int start;
    // Length of this extent. Note that extents don't span multiple lines.
    public int length;

    public Color color;
    public bool bold;

    // If true, there is a line break at the end of this extent.
    public bool lineBreakAtEnd;
  }

  private static readonly string[] VALID_TAGS = { "color", "b", "/color", "/b" };

  private TextExtent[] outExtents = new TextExtent[128];
  private int outExtentCount = 0;

  private Color[] colorStack = new Color[128];
  private int colorStackSize = 0;

  public SimpleRichTextParser() { }

  public void Reset()
  {
    outExtentCount = 0;
    colorStackSize = 0;
  }

  public void Parse(string richText, Color startColor)
  {
    Reset();
    int currentExtent = 0;
    outExtents[0] = new TextExtent
    {
      start = 0,
      length = 0,
      color = startColor,
      bold = false
    };
    TagInfo tag = new TagInfo();
    for (int i = 0; i < richText.Length; i++)
    {
      // Process line breaks.
      if (currentExtent < outExtents.Length - 1 && richText[i] == '\n')
      {
        outExtents[currentExtent].lineBreakAtEnd = true;
        outExtents[currentExtent + 1] = outExtents[currentExtent];
        outExtents[currentExtent + 1].start = i + 1;
        outExtents[currentExtent + 1].length = 0;
        outExtents[currentExtent + 1].lineBreakAtEnd = false;
        ++currentExtent;
        continue;
      }
      // If we don't find a tag, or if we're maxed out on extents, just grow the current extent.
      else if (currentExtent >= outExtents.Length - 1 || !CheckTag(richText, i, ref tag))
      {
        outExtents[currentExtent].length++;
        continue;
      }

      // Found a tag, so start new extent.
      TextExtent newExtent = outExtents[currentExtent];
      newExtent.start = tag.tagEnd + 1;
      newExtent.length = 0;
      newExtent.lineBreakAtEnd = false;
      // Figure out how the tag mutates the new extent.
      if (MatchWordNonAlloc(richText, tag.tagNameStart, "color") && tag.valueStart > 0)
      {
        PushColor(outExtents[currentExtent].color);
        newExtent.color = ParseColor(richText, tag.valueStart);
      }
      else if (MatchWordNonAlloc(richText, tag.tagNameStart, "#"))
      {
        // Direct color code, as in <#ff0000>.
        PushColor(outExtents[currentExtent].color);
        newExtent.color = ParseColor(richText, tag.tagNameStart);
      }
      else if (MatchWordNonAlloc(richText, tag.tagNameStart, "b"))
      {
        newExtent.bold = true;
      }
      else if (MatchWordNonAlloc(richText, tag.tagNameStart, "/b"))
      {
        newExtent.bold = false;
      }
      else if (MatchWordNonAlloc(richText, tag.tagNameStart, "/color"))
      {
        newExtent.color = PopColor(startColor);
      }
      outExtents[++currentExtent] = newExtent;
      // Continue parsing after the '>'
      i = tag.tagEnd;
    }
    outExtentCount = currentExtent + 1;
  }

  public int GetExtentCount()
  {
    return outExtentCount;
  }

  public TextExtent GetExtentAt(int i)
  {
    Debug.Assert(i >= 0 && i < outExtentCount, "Bad extent# " + i + ", count " + outExtentCount);
    return outExtents[i];
  }

  private struct TagInfo
  {
    public int tagNameStart;
    public int valueStart;
    public int tagEnd;
  }

  private void PushColor(Color color)
  {
    if (colorStackSize < colorStack.Length)
    {
      colorStack[colorStackSize++] = color;
    }
  }

  private Color PopColor(Color defaultColor)
  {
    if (colorStackSize > 0)
    {
      return colorStack[--colorStackSize];
    }
    return defaultColor;
  }

  private static bool CheckTag(string text, int index, ref TagInfo tagInfo)
  {
    if (text[index] != '<') return false;
    ++index;

    bool validTag = false;
    foreach (string validTagName in VALID_TAGS)
    {
      if (MatchWordNonAlloc(text, index, validTagName))
      {
        validTag = true;
        break;
      }
    }
    // Accept '#' as a 'tag' (raw color).
    if (text[index] == '#') validTag = true;
    if (!validTag)
    {
      return false;
    }

    tagInfo.tagNameStart = index;
    int equalsIndex = text.IndexOf('=', index);
    if (equalsIndex > 0)
    {
      tagInfo.valueStart = equalsIndex + 1;
      while (text[tagInfo.valueStart] == ' ') ++tagInfo.valueStart;
      if (text[tagInfo.valueStart] == '"' || text[tagInfo.valueStart] == '\'') ++tagInfo.valueStart;
    }
    else
    {
      tagInfo.valueStart = -1;
    }
    int closeIndex = text.IndexOf('>', index);
    if (closeIndex > 0)
    {
      tagInfo.tagEnd = closeIndex;
      return true;
    }
    return false;
  }

  private static bool MatchWordNonAlloc(string text, int startIndex, string searchString)
  {
    if (startIndex + searchString.Length > text.Length) return false;
    for (int i = 0; i < searchString.Length; i++)
    {
      if (text[i + startIndex] != searchString[i]) return false;
    }
    // Must end in a word boundary (nonalphanum character)
    if (startIndex + searchString.Length < text.Length && IsValidTagNameChar(text[startIndex + searchString.Length]))
    {
      // No word boundary (word continues).
      return false;
    }
    return true;
  }

  private static Color ParseColor(string text, int pos)
  {
    if (text[pos] == '#')
    {
      return ParseColorHex(text, pos + 1);
    }
    // Try the named colors.
    if (MatchWordNonAlloc(text, pos, "black")) return Color.black;
    if (MatchWordNonAlloc(text, pos, "blue")) return Color.blue;
    if (MatchWordNonAlloc(text, pos, "green")) return Color.green;
    if (MatchWordNonAlloc(text, pos, "orange")) return new Color(1.0f, 0.5f, 0.0f);
    if (MatchWordNonAlloc(text, pos, "purple")) return new Color(0.5f, 0.0f, 1.0f); ;
    if (MatchWordNonAlloc(text, pos, "red")) return Color.red;
    if (MatchWordNonAlloc(text, pos, "white")) return Color.white;
    if (MatchWordNonAlloc(text, pos, "yellow")) return Color.yellow;
    return Color.white;
  }

  private static Color ParseColorHex(string text, int pos)
  {
    if (text[pos] == '#') pos++;
    if (pos + 5 < text.Length && GetHexCharValue(text[pos + 3], -1) >= 0)
    {
      // It's a 6-digit hex value.
      return new Color(
        (GetHexCharValue(text[pos]) * 16 + GetHexCharValue(text[pos + 1])) / 255.0f,
        (GetHexCharValue(text[pos + 2]) * 16 + GetHexCharValue(text[pos + 3])) / 255.0f,
        (GetHexCharValue(text[pos + 4]) * 16 + GetHexCharValue(text[pos + 5])) / 255.0f, 1.0f);
    }
    else if (pos + 2 < text.Length)
    {
      // It's a 3-digit hex value.
      return new Color(
        GetHexCharValue(text[pos]) / 15.0f,
        GetHexCharValue(text[pos + 1]) / 15.0f,
        GetHexCharValue(text[pos + 2]) / 15.0f, 1.0f);
    }
    // It's nothing.
    return Color.white;
  }

  private static int GetHexCharValue(char ch, int defaultValue = 0)
  {
    return (ch >= 'a' && ch <= 'f') ? ch - 'a' + 10 :
      (ch >= 'A' && ch <= 'F') ? ch - 'A' + 10 :
      (ch >= '0' && ch <= '9') ? ch - '0' : defaultValue;
  }

  private static bool IsValidTagNameChar(char ch)
  {
    return ch >= 'a' && ch <= 'z';
  }
}