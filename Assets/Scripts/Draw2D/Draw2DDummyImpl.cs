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

// NOTE: This is just an example (and minimalistic!) implementation for this.
// If using this for real, you should either enable the Shaper2D implementation
// with the right plugin (and use Draw2DShaperImpl), or use another library
// that implements the functions below more thoroughly:
public class Draw2DDummyImpl : IDraw2D
{
  GUIStyle guiStyle;
  Texture2D rectTex;

  private class BitmapFontImpl : IBitmapFont2D
  {
    public BitmapFontImpl()
    {
    }
  }

  public Draw2DDummyImpl()
  {
    guiStyle = new GUIStyle();
    rectTex = new Texture2D(1, 1);
  }

  public Draw2DMode GetDraw2DMode()
  {
    return Draw2DMode.OnGui;
  }

  public void StartRendering(RenderTexture source, RenderTexture destination)
  {
    guiStyle = new GUIStyle();
    guiStyle.fontSize = 18;
  }
  public void EndRendering()
  {
  }
  public IBitmapFont2D CreateBitmapFont(TextAsset xmlFile, Texture2D fontImage)
  {
    return new BitmapFontImpl();
  }
  public void FillRect(Rect rect, Color color)
  {
    rectTex.SetPixel(0, 0, color);
    rectTex.Apply();
    GUI.skin.box.normal.background = rectTex;
    GUI.Box(rect, "");
  }
  public void DrawRect(Rect rect, Color color)
  {
  }
  public void DrawDashedRect(Rect rect, Color color, float lineWidth)
  {
  }
  public void DrawText(string text, float x, float y, float rotationDegs, float worldSizePixels, Color color, IBitmapFont2D font)
  {
    guiStyle.normal.textColor = color;
    GUI.Label(new Rect(x, y, Screen.width, Screen.height), text, guiStyle);
  }
  public void DrawTexture(Rect rect, Color color, Texture2D texture)
  {
  }
  public void FillQuads(Vector2[] vertices, Vector2[] texCoords, Color color, Texture2D texture)
  {
  }
  public void DrawDashedLine(Vector2 pointA, Vector2 pointB, Color color)
  {
  }
  public void DrawLine(Vector2 pointA, Vector2 pointB, Color color)
  {
  }
  public void DrawDashedTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, float lineWidth)
  {
  }
  public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, float lineWidth)
  {
  }
  public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
  {
  }
  public void DrawDashedCircle(Vector2 center, float radius, int sides, Color color, float lineWidth)
  {
  }
  public void DrawCircle(Vector2 center, float radius, int sides, Color color, float lineWidth)
  {
  }
  public void FillCircle(Vector2 center, float radius, Color color)
  {
  }
}