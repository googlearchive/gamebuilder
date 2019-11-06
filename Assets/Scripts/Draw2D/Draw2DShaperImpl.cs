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

#if USE_SHAPER_2D_LIBRARY

using UnityEngine;
using GraphicDNA;

public class Draw2DShaperImpl : IDraw2D
{
  private class BitmapFontImpl : IBitmapFont2D
  {
    public BitmapFont bitmapFont;
    public BitmapFontImpl(BitmapFont bitmapFont)
    {
      this.bitmapFont = bitmapFont;
    }
  }
  public Draw2DMode GetDraw2DMode()
  {
    return Draw2DMode.OnRenderImage;
  }

  public void StartRendering(RenderTexture source, RenderTexture destination)
  {
    Drawing2D.SetViewport(Vector2.zero, source.width, source.height);
  }
  public void EndRendering()
  {
  }
  public IBitmapFont2D CreateBitmapFont(TextAsset xmlFile, Texture2D fontImage)
  {
    return new BitmapFontImpl(BitmapFont.FromXml(xmlFile, fontImage));
  }
  public void FillRect(Rect rect, Color color)
  {
    Drawing2D.FillRect(rect, color);
  }
  public void DrawRect(Rect rect, Color color)
  {
    Drawing2D.DrawRect(rect, color);
  }
  public void DrawDashedRect(Rect rect, Color color, float lineWidth)
  {
    Drawing2D.DrawDashedRect(rect, color, lineWidth);
  }
  public void DrawText(string text, float x, float y, float rotationDegs, float worldSizePixels, Color color, IBitmapFont2D font)
  {
    Drawing2D.DrawText(text, x, y, rotationDegs, worldSizePixels, color, ((BitmapFontImpl)font).bitmapFont);
  }
  public void DrawTexture(Rect rect, Color color, Texture2D texture)
  {
    Drawing2D.DrawTexture(rect, color, texture);
  }
  public void FillQuads(Vector2[] vertices, Vector2[] texCoords, Color color, Texture2D texture)
  {
    Drawing2D.FillQuads(vertices, texCoords, color, texture);
  }
  public void DrawDashedLine(Vector2 pointA, Vector2 pointB, Color color)
  {
    Drawing2D.DrawDashedLine(pointA, pointB, color);
  }
  public void DrawLine(Vector2 pointA, Vector2 pointB, Color color)
  {
    Drawing2D.DrawLine(pointA, pointB, color);
  }
  public void DrawDashedTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, float lineWidth)
  {
    Drawing2D.DrawDashedTriangle(a, b, c, color, lineWidth);
  }
  public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, float lineWidth)
  {
    Drawing2D.DrawTriangle(a, b, c, color, lineWidth);
  }
  public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
  {
    Drawing2D.FillTriangle(a, b, c, color);
  }
  public void DrawDashedCircle(Vector2 center, float radius, int sides, Color color, float lineWidth)
  {
    Drawing2D.DrawDashedCircle(center, radius, sides, color, lineWidth);
  }
  public void DrawCircle(Vector2 center, float radius, int sides, Color color, float lineWidth)
  {
    Drawing2D.DrawCircle(center, radius, sides, color, lineWidth);
  }
  public void FillCircle(Vector2 center, float radius, Color color)
  {
    Drawing2D.FillCircle(center, radius, color);
  }

}
#endif