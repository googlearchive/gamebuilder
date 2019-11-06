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

// Controls the display of script-generated UI.
//
// This is an immediate-mode UI system where, on each frame, the game's scripting
// code makes calls to draw UI elements. For a UI element to remain onscreen, it must be
// re-requested on every frame. When the script code stops requesting a UI element,
// it will disappear from the screen. Similar to Unity's old IMGUI system.
//
// Let's name our coordinate systems:
//
// SCREEN COORDINATES: This is defined by Unity. The bottom-left is 0,0 and the top-right
// is (Screen.width - 1, Screen.height - 1). This covers the entire screen, not just our
// rendering viewport.
//
// NORMALIZED VIEWPORT COORDINATES: This is (0,0) at the bottom-left of the viewport,
// and (1,1) at the top-right. This is also defined by Unity.
//
// SURFACE COORDINATES: This is the coordinate system used by the Drawing2D system.
// It's weird. It's given in pixels, and expects x = 0 to the the left edge of the SCREEN
// (not the viewport) but expects Y = 0 to be the top of the VIEWPORT (not screen).
//
// GAME UI COORDINATES: This is the coordinate system used by users of Game Builder.
// This has (0,0) at the top-left and the width is ALWAYS 1600. The height depends on
// the aspect ration of the screen. On a 16:9 screen the height would be 900.

public class GameUiMain : MonoBehaviour
{
  [SerializeField] Texture2D bitmapFontImage;
  [SerializeField] TextAsset bitmapFontXml;

  private IBitmapFont2D bitmapFont;
  private SimpleRichTextParser simpleRichTextParser = new SimpleRichTextParser();
  private IDraw2D draw2D;

  // TODO: enable this back when we implement Steam Workshop based uploading.
  private static readonly bool ENABLE_IMAGES = false;

  // If true, shows font calibration pattern.
  // Each character should neatly fit in one of the calibration pattern's boxes.
  private static readonly bool DEBUG_FONT_SIZE_TEST = false;

  // Screen width in game UI coordinates. This is fixed at 1600.
  private const float SCREEN_WIDTH_GAMEUI_COORDS = 1600;

  // Desired font height in game UI coordinate space.
  // This value was manually calibrated to make each character fit in a box
  // whose size is 13 x 20 (see DEFAULT_CHAR_WIDTH and DEFAULT_CHAR_HEIGHT below).
  const float DEFAULT_TEXT_SIZE = 29.5f; // keep this in sync with widgets.js.txt

  // The default width of a char and height of a line when the default text size
  // is used. This is in game UI space.
  // NOTE: Previous version had this at precisely 13 and 20 respectively, so changing this
  // might break the layout of existing games. So try not to change these values.
  // Instead, DEAULT_TEXT_SIZE was chosen to fit these values.
  // Keep this in sync with widgets.js.txt
  const float DEFAULT_CHAR_WIDTH = 13;
  const float DEFAULT_LINE_HEIGHT = 20;

  // Keep this list of types in sync with widgets.js.txt
  const int CMD_TEXT = 1;
  const int CMD_RECT = 2;
  const int CMD_BUTTON = 3;
  const int CMD_IMAGE = 4;
  const int CMD_CIRCLE = 5;
  const int CMD_TRIANGLE = 6;
  const int CMD_LINE = 7;
  const int CMD_IMAGE_SLICE = 8;

  // Represents a command to draw a UI element. These are prepared by the game's Javascript to indicate
  // which UI elements it wants on the screen.
  //
  // WARNING: If you change this struct, make sure that the UI API functions in apiv2/ui.js.txt
  // are also adapted to your changes.
  [System.Serializable]
  public struct UiCommand
  {
    public string actorName;
    public int cmd;

    public string text;
    public UiRect rect;
    public string clickMessageName;
    public string imageId;

    public string style;
    public string clickMessageArgJson;
    public int backgroundColor;
    public int textColor;
    public float textSize;

    public float opacity;

    public float radius;
    public Vector3[] points;
    public UiRect srcRect; // for CMD_IMAGE_SLICE
    public bool noFilter; // for CMD_IMAGE and CMD_IMAGE_SLICE
  }

  [System.Serializable]
  public struct UiRect
  {
    public float x, y, w, h;
    public Rect ToUnityRect()
    {
      return new Rect(x, y, w, h);
    }

    public override string ToString()
    {
      return $"UiRect(x={x},y={y},w={w},h={h})";
    }
  }

  [System.Serializable]
  public struct UiCommandList
  {
    public UiCommand[] commands;
  }

  [System.Serializable]
  public struct ScreenInfoForScript
  {
    public float width, height;
    public int pixelWidth, pixelHeight;
  }

  private UiCommandList currentCommands;
  private UserMain userMain;
  private VoosEngine engine;
  private ImageSystem imageSystem;
  private ImageLoader imageLoader;
  private Vector2? surfaceSizeCached;

  public void SetUiCommands(UiCommandList commandsList)
  {
    this.currentCommands = commandsList;
  }

  public ScreenInfoForScript GetScreenInfoForScript()
  {
    Vector2 vpSize = GetSurfaceSize();
    return new ScreenInfoForScript
    {
      // Width is always fixed at 1600 in game UI coordinates.
      width = SCREEN_WIDTH_GAMEUI_COORDS,
      // Height is depends on the aspect ratio
      height = Mathf.CeilToInt(vpSize.y * SCREEN_WIDTH_GAMEUI_COORDS / vpSize.x),
      pixelWidth = (int)vpSize.x,
      pixelHeight = (int)vpSize.y,
    };
  }

  public void OnGUI()
  {
    Render(Draw2DMode.OnGui, null, null);
  }

  public void Render(Draw2DMode mode, RenderTexture source, RenderTexture destination)
  {
    if (draw2D.GetDraw2DMode() != mode) return;

    draw2D.StartRendering(source, destination);

    if (userMain == null) return;  // Not ready yet
    if (currentCommands.commands == null) return;

    for (int i = 0; i < currentCommands.commands.Length; i++)
    {
      HandleCommand(ref currentCommands.commands[i]);
    }

#if UNITY_EDITOR
    if (DEBUG_FONT_SIZE_TEST)
    {
      FontSizeTest();
    }
#endif

    draw2D.EndRendering();
  }

  private void FontSizeTest()
  {
    UiRect rect = new UiRect();
    for (int i = 0; i < 3; i++)
    {
      for (int j = 0; j < 100; j++)
      {
        rect.x = j * DEFAULT_CHAR_WIDTH;
        rect.y = 200 + i * DEFAULT_LINE_HEIGHT;
        rect.w = DEFAULT_CHAR_WIDTH;
        rect.h = DEFAULT_LINE_HEIGHT;
        draw2D.FillRect(ToSurfaceRect(rect), (i + j) % 2 == 0 ? Color.green : Color.blue);
      }
    }
    UiCommand cmd = new UiCommand
    {
      rect = new UiRect
      {
        x = 0,
        y = 200
      },
      textColor = 0xffffff,
      text = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789\n" +
        "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789\n" +
        "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789",
      opacity = 1
    };
    HandleCommandText(ref cmd);
  }

  private void Awake()
  {
#if USE_SHAPER_2D_LIBRARY
    draw2D = new Draw2DShaperImpl();
#else
    draw2D = new Draw2DDummyImpl();
    Debug.LogWarning("The USE_SHAPER_2D_LIBRARY preprocessor flag is not set. You will have NO game-generated 2D graphics! If you want game-generated 2D graphics, see README. If this is intentional, you can also just remove this error message :)");
#endif
    bitmapFont = draw2D.CreateBitmapFont(bitmapFontXml, bitmapFontImage);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref imageSystem);
    Util.FindIfNotSet(this, ref imageLoader);
  }

  private void Update()
  {
    userMain = userMain ?? GameObject.FindObjectOfType<UserMain>();
    if (userMain == null) return;
    CheckButtonClicks();
    // Invalidate surface size cache (it's only valid for 1 frame).
    surfaceSizeCached = null;
  }

  // Converts a point in Unity's SCREEN COORDINATE system to our 
  // GAME UI COORD system (see comment at the top of file).
  public Vector2 UnityScreenPointToGameUiPoint(Vector3 screenPoint)
  {
    Vector2 surfPoint;
    if (userMain != null)
    {
      Vector3 normalizedVpPoint = userMain.GetCamera().ScreenToViewportPoint(screenPoint);
      // normalizedVpPoint has (0,0) at bottom-left, (1,1) at top-right.
      Rect pixelRect = userMain.GetCamera().pixelRect;
      surfPoint = new Vector2(
        // Drawing2D expects X coordinates to be screen coordinates, 0 being the left edge.
        pixelRect.x + normalizedVpPoint.x * pixelRect.width,
        // Drawing2D expects Y coordinates to be relative to the top of the viewport, not screen (strangely!)
        (1 - normalizedVpPoint.y) * pixelRect.height);
    }
    else
    {
      // No userMain yet. Temporarily use full screen as viewport.
      surfPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
    }
    return SurfacePointToGameUiPoint(surfPoint);
  }

  public Vector2 SurfacePointToGameUiPoint(Vector3 surfPoint)
  {
    Vector2 surfSize = GetSurfaceSize();
    float ratio = GetSurfToGameUiCoordRatio();
    return new Vector2(surfPoint.x / ratio, surfPoint.y / ratio);
  }

  private float GetSurfToGameUiCoordRatio()
  {
    // Drawing2D apparently ignores the viewport when it interprets the X coordinate,
    // so use Screen.width instead of viewport width:
    return Screen.width / SCREEN_WIDTH_GAMEUI_COORDS;
  }

  private float ToSurfaceX(float gameUiX)
  {
    return gameUiX * GetSurfToGameUiCoordRatio();
  }

  private float ToSurfaceLength(float gameUiWidth)
  {
    return ToSurfaceX(gameUiWidth); // same formula
  }

  private float ToSurfaceY(float gameUiY)
  {
    return gameUiY * GetSurfToGameUiCoordRatio();
  }

  private Rect ToSurfaceRect(UiRect gameUiRect)
  {
    return new Rect(
      ToSurfaceX(gameUiRect.x), ToSurfaceY(gameUiRect.y),
      ToSurfaceLength(gameUiRect.w), ToSurfaceLength(gameUiRect.h));
  }

  private Vector2 ToSurfacePoint(Vector3 gameUiPoint)
  {
    return ToSurfacePoint(gameUiPoint.x, gameUiPoint.y);
  }

  private Vector2 ToSurfacePoint(float gameUiX, float gameUiY)
  {
    return new Vector2(ToSurfaceX(gameUiX), ToSurfaceY(gameUiY));
  }

  private static Color DecodeColor(int color, float opacity)
  {
    return new Color(
        ((color & 0xff0000) >> 16) / 255.0f,
        ((color & 0xff00) >> 8) / 255.0f,
        ((color & 0xff) / 255.0f),
        opacity);
  }

  private void HandleCommand(ref UiCommand command)  // ref is for performance (avoid copying struct)
  {
    switch (command.cmd)
    {
      case CMD_TEXT: HandleCommandText(ref command); break;
      case CMD_RECT: HandleCommandRect(ref command); break;
      case CMD_IMAGE: HandleCommandImage(ref command); break;
      case CMD_BUTTON: HandleCommandButton(ref command); break;
      case CMD_CIRCLE: HandleCommandCircle(ref command); break;
      case CMD_LINE: HandleCommandLine(ref command); break;
      case CMD_TRIANGLE: HandleCommandTriangle(ref command); break;
      case CMD_IMAGE_SLICE: HandleCommandImageSlice(ref command); break;
      default:
        throw new System.Exception("Unknown UI command " + command.cmd);
    }
  }

  private void HandleCommandText(ref UiCommand command)
  {
    float x = command.rect.x;
    float y = command.rect.y;
    Color color = DecodeColor(command.textColor, command.opacity);
    float textSize = command.textSize > 0 ? command.textSize : DEFAULT_TEXT_SIZE;
    float lineHeight = GetLineHeightForSize(textSize);
    RenderRichText(command.text, x, y, textSize, color);
  }

  private void RenderRichText(string text, float x, float y, float textSize, Color startColor)
  {
    simpleRichTextParser.Parse(text, startColor);
    int extentCount = simpleRichTextParser.GetExtentCount();
    float startX = x;
    for (int i = 0; i < extentCount; i++)
    {
      RenderSingleExtent(text, simpleRichTextParser.GetExtentAt(i), startX, textSize, ref x, ref y);
    }
  }

  private void RenderSingleExtent(string text, SimpleRichTextParser.TextExtent extent, float startX, float textSize, ref float x, ref float y)
  {
    string extentText = (extent.start == 0 && extent.length == text.Length) ?
      text : text.Substring(extent.start, extent.length);
    draw2D.DrawText(extentText, ToSurfaceX(x), ToSurfaceY(y), 0.0f,
       ToSurfaceLength(textSize), extent.color, bitmapFont);
    if (extent.bold)
    {
      // Double-strike to simulate bold.
      draw2D.DrawText(extentText, ToSurfaceX(x) + 1, ToSurfaceY(y), 0.0f,
       ToSurfaceLength(textSize), extent.color, bitmapFont);
    }
    if (extent.lineBreakAtEnd)
    {
      x = startX;
      y += GetLineHeightForSize(textSize);
    }
    else
    {
      x += GetCharWidthForSize(textSize) * extent.length;
    }
  }

  private void RenderCenteredSingleLineText(string text, float x, float y, Color color, float size)
  {
    float textWidth = text.Length * GetCharWidthForSize(size);
    float textHeight = GetLineHeightForSize(size);
    float top = y - textHeight / 2;
    float left = x - textWidth / 2;
    draw2D.DrawText(text, ToSurfaceX(left), ToSurfaceY(top), 0.0f,
       ToSurfaceLength(size), color, bitmapFont);
  }

  private float GetCharWidthForSize(float textSize)
  {
    return DEFAULT_CHAR_WIDTH * textSize / DEFAULT_TEXT_SIZE;
  }

  private float GetLineHeightForSize(float textSize)
  {
    return DEFAULT_LINE_HEIGHT * textSize / DEFAULT_TEXT_SIZE;
  }

  private void HandleCommandRect(ref UiCommand command)
  {
    if (command.style == "BORDER")
    {
      draw2D.DrawRect(ToSurfaceRect(command.rect), DecodeColor(command.backgroundColor, command.opacity));
    }
    else if (command.style == "DASHED")
    {
      draw2D.DrawDashedRect(ToSurfaceRect(command.rect), DecodeColor(command.backgroundColor, command.opacity), 1);
    }
    else
    {
      draw2D.FillRect(ToSurfaceRect(command.rect), DecodeColor(command.backgroundColor, command.opacity));
    }
  }

  private void HandleCommandImage(ref UiCommand command)
  {
    string url = imageSystem.GetImageUrl(command.imageId);
    if (string.IsNullOrEmpty(url))
    {
      draw2D.FillRect(ToSurfaceRect(command.rect), Color.gray);
    }
    else
    {
      Texture2D tex = imageLoader.GetOrRequest(url);
      if (tex == null)
      {
        draw2D.FillRect(ToSurfaceRect(command.rect), Color.black);
      }
      else
      {
        UiRect gameUiRect = command.rect;
        if (gameUiRect.w < 0)
        {
          // Means "auto compute from image".
          gameUiRect.w = tex.width;
        }
        if (gameUiRect.h < 0)
        {
          // Means "auto compute to keep aspect ratio".
          gameUiRect.h = tex.width > 0 ? (tex.height * gameUiRect.w / tex.width) : 1;
        }
        Rect screenRect = ToSurfaceRect(gameUiRect);
        tex.filterMode = command.noFilter ? FilterMode.Point : FilterMode.Bilinear;
        draw2D.DrawTexture(screenRect, Color.white, tex);
      }
    }
  }

  private Vector2[] tmpVerts_HandleCommandImageSlice = new Vector2[4];
  private Vector2[] tmpTexCoords_HandleCommandImageSlice = new Vector2[4];
  private void HandleCommandImageSlice(ref UiCommand command)
  {
    Vector2[] verts = tmpVerts_HandleCommandImageSlice;
    Vector2[] texCoords = tmpTexCoords_HandleCommandImageSlice;
    string url = imageSystem.GetImageUrl(command.imageId);
    Texture2D tex = !string.IsNullOrEmpty(url) ? imageLoader.GetOrRequest(url) : null;
    if (tex == null) return;
    Rect destRect = ToSurfaceRect(command.rect);
    Rect srcRect = command.srcRect.ToUnityRect();
    RectToVertices(destRect, 1, 1, verts);
    RectToVertices(srcRect, tex.width, tex.height, texCoords);
    texCoords[0].y = 1 - texCoords[0].y;
    texCoords[1].y = 1 - texCoords[1].y;
    texCoords[2].y = 1 - texCoords[2].y;
    texCoords[3].y = 1 - texCoords[3].y;
    tex.filterMode = command.noFilter ? FilterMode.Point : FilterMode.Bilinear;
    draw2D.FillQuads(verts, texCoords, Color.white, tex);
  }

  private static void RectToVertices(Rect rect, float divisorX, float divisorY, Vector2[] outVerts)
  {
    outVerts[0].x = outVerts[1].x = rect.x / divisorX;
    outVerts[2].x = outVerts[3].x = (rect.x + rect.width) / divisorX;
    outVerts[0].y = outVerts[3].y = rect.y / divisorY;
    outVerts[1].y = outVerts[2].y = (rect.y + rect.height) / divisorY;
  }

  private void HandleCommandButton(ref UiCommand command)
  {
    float textSize = command.textSize > 0 ? command.textSize : DEFAULT_TEXT_SIZE;
    draw2D.FillRect(ToSurfaceRect(command.rect), DecodeColor(command.backgroundColor, command.opacity));
    draw2D.DrawRect(ToSurfaceRect(command.rect), DecodeColor(0xffffff, command.opacity));
    RenderCenteredSingleLineText(command.text, command.rect.ToUnityRect().center.x,
      command.rect.ToUnityRect().center.y, DecodeColor(command.textColor, 1), textSize);
  }

  private void HandleCommandLine(ref UiCommand command)
  {
    Debug.Assert(command.points != null, "CMD_LINE needs points. Was null.");
    Debug.Assert(command.points.Length == 2, "CMD_LINE must have 2 points, had " + command.points.Length);
    Vector2 screenPointA = ToSurfacePoint(command.points[0]);
    Vector2 screenPointB = ToSurfacePoint(command.points[1]);
    Color color = DecodeColor(command.backgroundColor, command.opacity);
    if (command.style == "DASHED")
    {
      draw2D.DrawDashedLine(screenPointA, screenPointB, color);
    }
    else
    {
      draw2D.DrawLine(screenPointA, screenPointB, color);
    }
  }

  private void HandleCommandTriangle(ref UiCommand command)
  {
    Debug.Assert(command.points != null, "CMD_TRIANGLE needs points. Was null.");
    Debug.Assert(command.points.Length == 3, "CMD_TRIANGLE must have 3 points, had " + command.points.Length);
    Vector2 screenPointA = ToSurfacePoint(command.points[0]);
    Vector2 screenPointB = ToSurfacePoint(command.points[1]);
    Vector2 screenPointC = ToSurfacePoint(command.points[2]);
    Color color = DecodeColor(command.backgroundColor, command.opacity);
    if (command.style == "DASHED")
    {
      draw2D.DrawDashedTriangle(screenPointA, screenPointB, screenPointC, color, 1);
    }
    else if (command.style == "BORDER")
    {
      draw2D.DrawTriangle(screenPointA, screenPointB, screenPointC, color, 1);
    }
    else
    {
      draw2D.FillTriangle(screenPointA, screenPointB, screenPointC, color);
    }
  }

  private void HandleCommandCircle(ref UiCommand command)
  {
    Debug.Assert(command.points != null, "CMD_CIRCLE needs points. Was null.");
    Debug.Assert(command.points.Length == 1, "CMD_CIRCLE must have 1 point, had " + command.points.Length);
    Vector2 screenCenter = ToSurfacePoint(command.points[0]);
    Color color = DecodeColor(command.backgroundColor, command.opacity);
    float screenRadius = ToSurfaceLength(command.radius);
    int sides = CalculateCircleSidesForRadius(screenRadius);
    if (command.style == "DASHED")
    {
      draw2D.DrawDashedCircle(screenCenter, screenRadius, sides, color, 1);
    }
    else if (command.style == "BORDER")
    {
      draw2D.DrawCircle(screenCenter, screenRadius, sides, color, 1);
    }
    else
    {
      // Due to Drawing2D bugs, we fill the circle TWICE using different methods:
      // (each has its own problems but if we do both, it works).

      // Method 1 (normal FillCircle way):
      // This leaves jagged edges but fills the center well.
      draw2D.FillCircle(screenCenter, Mathf.Max(1, screenRadius - 2), color);
      // Method 2 (abuse "border width" parameter):
      // This draws the edge perfectly but leaves random 1-pixel holes in the center
      // (covered by our call above).
      draw2D.DrawCircle(screenCenter, screenRadius / 2, sides, color, screenRadius);
    }
  }

  private static int CalculateCircleSidesForRadius(float screenRadius)
  {
    return Mathf.CeilToInt(Mathf.Clamp(Mathf.PI * screenRadius, 3, 300));
  }

  // Gets mouse position in game UI coordinates.
  private Vector2 GetMousePos()
  {
    return UnityScreenPointToGameUiPoint(Input.mousePosition);
  }

  private void CheckButtonClicks()
  {
    // No button clicks while in edit mode.
    if (userMain == null || userMain.InEditMode()) return;
    // Must have button down.
    if (!Input.GetMouseButtonDown(0)) return;
    // Must have UI commands.
    if (currentCommands.commands == null) return;
    Vector2 mousePos = GetMousePos();
    // Let's see if that landed on any buttons.
    // Iterate backwards because the latest buttons have priority over the earlier ones.
    for (int i = currentCommands.commands.Length - 1; i >= 0; i--)
    {
      if (currentCommands.commands[i].cmd == CMD_BUTTON && currentCommands.commands[i].rect.ToUnityRect().Contains(mousePos))
      {
        // Button clicked.
        UiCommand thisCommand = currentCommands.commands[i];
        engine.EnqueueMessage(new VoosEngine.ActorMessage
        {
          name = string.IsNullOrEmpty(thisCommand.clickMessageName) ? "ButtonClicked" : thisCommand.clickMessageName,
          targetActor = thisCommand.actorName,
          argsJson = string.IsNullOrEmpty(thisCommand.clickMessageArgJson) ? "{}" : thisCommand.clickMessageArgJson
        });
        return;
      }
    }
  }

  private Vector2 GetSurfaceSize()
  {
    if (surfaceSizeCached == null)
    {
      if (userMain != null)
      {
        Camera camera = userMain.GetCamera();
        surfaceSizeCached = new Vector2(camera.pixelWidth, camera.pixelHeight);
      }
      else
      {
        // Temporarily use the full screen as viewport size while UserMain is initializing.
        // This only happens for a few frames at startup, so it doesn't matter.
        surfaceSizeCached = new Vector2(Screen.width, Screen.height);
      }
    }
    return surfaceSizeCached.Value;
  }
}
