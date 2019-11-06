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

public enum Draw2DMode
{
  OnGui,
  OnRenderImage
}

public interface IDraw2D
{
  Draw2DMode GetDraw2DMode();
  void StartRendering(RenderTexture source, RenderTexture destination);
  void EndRendering();
  IBitmapFont2D CreateBitmapFont(TextAsset xmlFile, Texture2D fontImage);
  void FillRect(Rect rect, Color color);
  void DrawRect(Rect rect, Color color);
  void DrawDashedRect(Rect rect, Color color, float lineWidth);
  void DrawText(string text, float x, float y, float rotationDegs, float worldSizePixels, Color color, IBitmapFont2D font);
  void DrawTexture(Rect rect, Color color, Texture2D texture);
  void FillQuads(Vector2[] vertices, Vector2[] texCoords, Color color, Texture2D texture);
  void DrawDashedLine(Vector2 pointA, Vector2 pointB, Color color);
  void DrawLine(Vector2 pointA, Vector2 pointB, Color color);
  void DrawDashedTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, float lineWidth);
  void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, float lineWidth);
  void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color);
  void DrawDashedCircle(Vector2 center, float radius, int sides, Color color, float lineWidth);
  void DrawCircle(Vector2 center, float radius, int sides, Color color, float lineWidth);
  void FillCircle(Vector2 center, float radius, Color color);
}

public interface IBitmapFont2D
{
}
