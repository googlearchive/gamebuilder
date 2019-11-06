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
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System;
using IO = System.IO;

public static partial class Util
{
  public static void Clear(this System.Text.StringBuilder sb)
  {
    sb.Length = 0;
  }

  // Call during FixedUpdate
  public static void RealizeTargetVelocity(GameObject target, Vector3 targetVelocity)
  {
    var rb = target.GetComponent<Rigidbody>();
    Vector3 deltaVel = (targetVelocity - rb.velocity);
    rb.AddForce(deltaVel, ForceMode.VelocityChange);
  }

  // Call during FixedUpdate
  public static void RealizeTargetVelocityXYZ(Rigidbody rb, Vector3 targetVelocity)
  {
    Vector3 deltaVel = (targetVelocity - rb.velocity);
    rb.AddForce(deltaVel, ForceMode.VelocityChange);
  }

  public static void RealizeTargetVelocityXZ(Rigidbody rb, Vector2 targetVelocityXZ)
  {
    Vector3 targetVelocity = new Vector3(targetVelocityXZ.x, rb.velocity.y, targetVelocityXZ.y);
    Vector3 deltaVel = (targetVelocity - rb.velocity);
    Debug.Assert(Mathf.Abs(deltaVel.y) < 1e-4);
    rb.AddForce(deltaVel, ForceMode.VelocityChange);
  }

  public static bool HoldingModiferKeys()
  {
    return IsControlOrCommandHeld() || IsShiftHeld();
  }

  public static string LesserOf(string a, string b)
  {
    return a.CompareTo(b) == -1 ? a : b;
  }

  public static string GreaterOf(string a, string b)
  {
    return a.CompareTo(b) == 1 ? a : b;
  }

  public struct Maybe<T>
  {
    private T value;

    private bool filled;

    private string errorMessage;

    public static Maybe<T> CreateEmpty()
    {
      return new Maybe<T>
      {
        value = default(T),
        filled = false,
        errorMessage = null
      };
    }

    public static Maybe<T> CreateError(string errorMessage)
    {
      return new Maybe<T>
      {
        errorMessage = errorMessage,
        value = default(T),
        filled = false
      };
    }

    public static Maybe<T> CreateWith(T value)
    {
      Maybe<T> rv;
      rv.value = value;
      rv.filled = true;
      rv.errorMessage = null;
      return rv;
    }

    public bool IsEmpty()
    {
      return !filled;
    }

    public T Get()
    {
      return value;
    }

    public T Value
    {
      get { return Get(); }
    }

    public T GetOr(T defaultValue)
    {
      if (IsEmpty())
      {
        return defaultValue;
      }
      else
      {
        return Get();
      }
    }

    public string GetErrorMessage() { return errorMessage; }

    public override string ToString()
    {
      return !IsEmpty() ? $"Value: {value}" : $"Empty with error: {errorMessage}";
    }
  }

  public struct GUILayoutFrobArea : System.IDisposable
  {
    public GUILayoutFrobArea(Vector3 worldPos, int width, int height)
    {
      Debug.Assert(Camera.main != null);

      Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
      screenPos.y = Screen.height - screenPos.y;

      // Are we behind the camera?
      if (Vector3.Dot((worldPos - Camera.main.transform.position), Camera.main.transform.forward) < 0f)
      {
        // Hide by pushing out of screen bounds.
        screenPos = new Vector2(Screen.width, Screen.height);
      }

      GUILayout.BeginArea(new Rect(screenPos, new Vector2(width, height)));
    }

    public void Dispose()
    {
      GUILayout.EndArea();
    }
  }

  [System.Serializable]
  public struct PlainStringPair
  {
    public string a;
    public string b;
  }
  public static PlainStringPair[] CreatePairList(IReadOnlyCollection<Util.Tuple<string, string>> pairs)
  {
    var rv = new PlainStringPair[pairs.Count];
    int i = 0;
    foreach (var col in pairs)
    {
      rv[i].a = col.first;
      rv[i].b = col.second;
      i++;
    }
    return rv;
  }

  public class KeyComboPoller
  {
    KeyCode[] keys;

    bool wasAllHeld = false;
    bool wasJustDown = false;
    bool wasJustUp = false;

    public KeyComboPoller(KeyCode[] keys)
    {
      this.keys = keys;
    }

    // You must call this once per frame.
    public void Update()
    {
      bool allHeld = true;
      foreach (KeyCode key in keys)
      {
        if (!Input.GetKey(key))
        {
          allHeld = false;
          break;
        }
      }

      wasJustDown = !wasAllHeld && allHeld;
      wasJustUp = wasAllHeld && !allHeld;
      // Must be last.
      wasAllHeld = allHeld;
    }

    public bool GetDown()
    {
      return wasJustDown;
    }

    public bool GetUp()
    {
      return wasJustUp;
    }

    public bool GetHeld()
    {
      return wasAllHeld;
    }
  }

  public struct ProfileBlock : System.IDisposable
  {
    public ProfileBlock(string label)
    {
      UnityEngine.Profiling.Profiler.BeginSample(label);
    }
    public void Dispose()
    {
      UnityEngine.Profiling.Profiler.EndSample();
    }
  }

  public static int Clamp(this int x, int min, int max)
  {
    if (x < min) return min;
    if (x > max) return max;
    else return x;
  }

  public static float Clamp01(this float x)
  {
    return Mathf.Max(0, Mathf.Min(1, x));
  }

  // It's usually good practice to make users expliciltly hook up dependencies in the editor.
  // However, this can be inconvenient when prefabs come into play, so it's often tempting to use FindObjectOfType, or 
  // even worse, a static singleton. This is a compromise: Allow the option of an explicit hook up, but if it's not set,
  // just auto-fallback to FindObjectOfType.
  //
  // Usage: declare a public reference to some component type, like:
  //  [SerializeField] MasterController master; // prefer SerializeField over public
  // Then in Awake(), do this:
  //  Util.FindIfNotSet(this, ref master);
  public static void FindIfNotSet<ComponentType>(MonoBehaviour self, ref ComponentType reference) where ComponentType : MonoBehaviour
  {
    if (reference == null)
    {
      // Prefer ancestors
      reference = self.GetComponentInParent<ComponentType>();
    }

    if (reference == null)
    {
      reference = MonoBehaviour.FindObjectOfType<ComponentType>();
    }

    if (reference == null)
    {
      string componentName = typeof(ComponentType).FullName;
      throw new System.InvalidProgramException("Game object '" + self.gameObject.name + "' needs a dependency of component type '"
        + componentName + "', but none was found in the scene. Set the reference manually in the editor, OR make sure one exists in the scene. "
        + "FYI instances of " + componentName + " in the ancestry of " + self.name + " will be preferred over other instances in the scene.");
    }
  }

  public static void FitUnitCubeIntoCorners(Transform cube, Vector3 oneCorner, Vector3 anotherCorner)
  {
    Vector3 center = Vector3.Lerp(oneCorner, anotherCorner, 0.5f);
    cube.position = center;

    // This is assuming identity scale up the hierarchy
    Vector3 size = (anotherCorner - oneCorner).Abs();
    cube.localScale = size;
  }

  // Counts normal characters and spaces as a single 'rune',
  // but counts sprites (ex. "<sprite=1>") as a single one. For now, we just do very basic/hacky
  // implementation where < and > are treated as special chars, so this won't work if you have them normally in your text.
  // If the string is out of chars, this returns the length.
  // It is an error to call this with start >= s.Length
  public static int ToNextTMProRuneStart(string s, int start)
  {
    // If at a sprite, go to its end.
    if (s[start] == '<')
    {
      while (s[start] != '>' && start < s.Length)
      {
        start++;
      }

      if (start == s.Length)
      {
        throw new System.Exception("Could not find closing '>' in TMPro string: " + s);
      }
    }

    start++;

    return start;
  }

  public static int CountRunes(string s)
  {
    int numRunes = 0;
    for (int cursor = 0; cursor < s.Length; cursor = ToNextTMProRuneStart(s, cursor))
    {
      numRunes++;
    }
    return numRunes;
  }

  private static Dictionary<System.Type, System.Array> EnumGetValuesCache = new Dictionary<System.Type, System.Array>();

  static System.Array RawValuesOf<T>()
  {
    System.Array rv;
    if (EnumGetValuesCache.TryGetValue(typeof(T), out rv))
    {
      return rv;
    }
    else
    {
      rv = System.Enum.GetValues(typeof(T));
      EnumGetValuesCache[typeof(T)] = rv;
      return rv;
    }
  }


  public static int CountEnumValues<TEnum>()
  {
    return RawValuesOf<TEnum>().Length;
  }

  // NOTE: Apparently "enum generic type constraints" is only supported in .NET 7. Oh well.
  public static TEnum ParseEnum<TEnum>(string stringValue)
  {
    try
    {
      return (TEnum)System.Enum.Parse(typeof(TEnum), stringValue);
    }
    catch (System.ArgumentException e)
    {
      string validValues = "";
      foreach (TEnum val in ValuesOf<TEnum>())
      {
        validValues += val.ToString() + ", ";
      }
      throw new System.Exception($"Error while trying to parse '{stringValue}' into an enum {typeof(TEnum).Name}. Valid values are: {validValues}", e);
    }
  }

  public static bool TryParseEnum<TEnum>(string stringValue, out TEnum outValue, bool ignoreCase = false)
  {
    try
    {
      outValue = (TEnum)System.Enum.Parse(typeof(TEnum), stringValue, ignoreCase);
      return true;
    }
    catch (System.ArgumentException e)
    {
      outValue = default(TEnum);
      return false;
    }
  }

  public static T[] ValuesOf<T>()
  {
    return (T[])RawValuesOf<T>();
  }

  static int pid = System.Diagnostics.Process.GetCurrentProcess().Id;

  // Just a variant that includes the frame count.
  public static void Log(string msg)
  {
    Debug.Log($"P{pid} F{Time.frameCount}: {msg}");
  }

  public static void LogError(string msg)
  {
    Debug.LogError($"P{pid} F{Time.frameCount}: {msg}");
  }

  public static void LogWarning(string msg)
  {
    Debug.LogWarning($"P{pid} F{Time.frameCount}: {msg}");
  }

  public static void MatchBounds(GameObject oldObj, GameObject newObj)
  {
    Renderer[] oldRends = oldObj.GetComponentsInChildren<Renderer>();
    Renderer[] newRends = newObj.GetComponentsInChildren<Renderer>();
    if (oldRends.Length == 0 || newRends.Length == 0)
    {
      Debug.LogError("Something not right here");
      return;
    }

    Bounds oldBounds = oldRends[0].bounds;
    for (int i = 1; i < oldRends.Length; i++)
    {
      oldBounds.Encapsulate(oldRends[i].bounds);
    }

    Bounds newBounds = newRends[0].bounds;
    for (int i = 1; i < newRends.Length; i++)
    {
      newBounds.Encapsulate(newRends[i].bounds);
    }

    //get multiplier to fit magnitude of size
    float mod = oldBounds.size.magnitude / newBounds.size.magnitude;
    newObj.transform.localScale *= mod;

    //recalc
    newBounds = newRends[0].bounds;
    for (int i = 1; i < newRends.Length; i++)
    {
      newBounds.Encapsulate(newRends[i].bounds);
    }

    //x and z are based on center
    Vector3 delta = oldBounds.center - newBounds.center;
    //y is so the bottoms of the bounds are the same
    delta.y = oldBounds.min.y - newBounds.min.y;

    //mod position
    newObj.transform.position += delta;
  }

  // Returns the resulting height in world space.
  public static Bounds ComputeWorldRenderBounds(GameObject obj)
  {
    Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
    return ComputeWorldRenderBounds(renderers);
  }

  public static Bounds ComputeWorldRenderBounds(Renderer[] renderers)
  {
    Debug.Assert(renderers.Length > 0);

    Bounds originalWorldBounds = renderers[0].bounds;
    for (int i = 1; i < renderers.Length; i++)
    {
      originalWorldBounds.Encapsulate(renderers[i].bounds);
    }
    return originalWorldBounds;
  }

  public class Table<T>
  {
    public struct Entry
    {
      public string id;
      public T value;

      public override string ToString()
      {
        return $"id={id} value={value.ToString()}";
      }
    }

    Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

    // NOTE: This is gross, and it's mainly because I couldn't figure out how to make JsonUtility play nice with generics :(
    public void GetJsonables(ref string[] ids, ref T[] values)
    {
      ids = new string[entries.Count];
      values = new T[entries.Count];
      int i = 0;
      foreach (var entry in entries.Values)
      {
        ids[i] = entry.id;
        values[i] = entry.value;
        i++;
      }
    }

    public void LoadJsonables(string[] ids, T[] values)
    {
      Debug.Assert(ids != null);
      Debug.Assert(values != null);
      Debug.Assert(values.Length == ids.Length);
      entries.Clear();
      for (int i = 0; i < values.Length; i++)
      {
        this.Set(ids[i], values[i]);
      }
    }

    public bool Exists(string id)
    {
      return entries.ContainsKey(id);
    }

    public IEnumerable<string> ComplementOf(HashSet<string> ids)
    {
      foreach (Entry entry in GetAll())
      {
        if (!ids.Contains(entry.id))
        {
          yield return entry.id;
        }
      }
    }

    public T Get(string id)
    {
      if (Exists(id))
      {
        return entries[id].value;
      }
      else
      {
        throw new System.Exception($"Id '{id}' not found in database of {typeof(T).Name}'s");
      }
    }

    // Returns true if calling 'Set' with the same arguments would actually change the database.
    public bool SetWouldChange(string id, T newValue)
    {
      if (!entries.ContainsKey(id))
      {
        // Yes, setting would add this.
        return true;
      }
      else
      {
        return !entries[id].value.Equals(newValue);
      }
    }

    public bool Delete(string id)
    {
      if (entries.ContainsKey(id))
      {
        entries.Remove(id);
        return true;
      }
      return false;
    }

    public void DeleteAll(IEnumerable<string> ids)
    {
      foreach (string id in ids)
      {
        Delete(id);
      }
    }

    // Updates or adds.
    public void Set(string id, T newValue)
    {
      entries[id] = new Entry { id = id, value = newValue };
    }

    public Dictionary<string, Entry>.ValueCollection GetAll()
    {
      return entries.Values;
    }

    public void DebugLog(string prefix)
    {
      foreach (var entry in entries)
      {
        Debug.Log($"{prefix}: {entry.Value.ToString()}");
      }
    }

    public int GetCount()
    {
      return entries.Values.Count;
    }
  }

  // Returns true if either shift key is held down, according to Unity's Input module.
  public static bool IsShiftHeld()
  {
    return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
  }

  // Returns true if either control or command key is held down, according to Unity's Input module.
  public static bool IsControlOrCommandHeld()
  {
    return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
    || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
  }

  public static bool IsControlOrCommandDown()
  {
    return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)
    || Input.GetKeyDown(KeyCode.LeftCommand) || Input.GetKeyDown(KeyCode.RightCommand);
  }

  public static bool IsControlOrCommandUp()
  {
    return Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl)
    || Input.GetKeyUp(KeyCode.LeftCommand) || Input.GetKeyUp(KeyCode.RightCommand);
  }


  // Checks if number key pressed this frame and returns -1 for none.
  public static int GetNumberKeyDown()
  {
    for (int i = 0; i < 10; i++)
    {
      if (Input.GetKeyDown((KeyCode)(48))) return 9;
      if (Input.GetKeyDown((KeyCode)(48 + i))) return i - 1;
    }
    return -1;
  }


  // Returns true if either alt key is held down, according to Unity's Input module.
  public static bool IsAltHeld()
  {
    return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
  }

  public static void LaunchNativeFileExplorer(string absolutePath)
  {
    Application.OpenURL($"file://{absolutePath}");
  }

  public static V GetOrSet<K, V>(this Dictionary<K, V> dict, K key, V defaultValue)
  {
    V existing = defaultValue;
    if (dict.TryGetValue(key, out existing))
    {
      return existing;
    }
    else
    {
      dict[key] = defaultValue;
      return defaultValue;
    }
  }

  public static V GetOr<K, V>(this Dictionary<K, V> dict, K key, V defaultValue)
  {
    V existing = defaultValue;
    if (dict.TryGetValue(key, out existing))
    {
      return existing;
    }
    else
    {
      // Does NOT set
      return defaultValue;
    }
  }

  public static V GetOrSetEvaled<K, V>(this Dictionary<K, V> dict, K key, System.Func<V> evalDefault)
  {
    V existing = default(V);
    if (dict.TryGetValue(key, out existing))
    {
      return existing;
    }
    else
    {
      V newVal = evalDefault();
      dict[key] = newVal;
      return newVal;
    }
  }

  // Use this to guard against Windows 10 turning random files ReadOnly..
  public static void SetNormalFileAttributes(string path)
  {
    if (System.IO.File.Exists(path))
    {
      System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
    }
  }

  public static void SaveTextureToPng(Texture2D texture, string pngPath)
  {
    byte[] bytes = texture.EncodeToPNG();
    System.IO.File.WriteAllBytes(pngPath, bytes);
  }

  public static Texture2D ReadPngToTexture(string pngPath)
  {
    if (!System.IO.File.Exists(pngPath))
    {
      return null;
    }
    byte[] thumbnailBytes = System.IO.File.ReadAllBytes(pngPath);
    // For highest quality, use RGBA32 and DISABLE mipChian.
    Texture2D thumbnail = new Texture2D(1, 1, TextureFormat.RGBA32, false);
    thumbnail.LoadImage(thumbnailBytes);
    return thumbnail;
  }

  public static byte[] TextureToZippedJpeg(Texture2D texture)
  {
    byte[] bytes = texture.EncodeToJPG();
    return Util.GZip(bytes);
  }

  public static Texture2D ZippedJpegToTexture2D(byte[] zippedJpegBytes, bool nonReadable = true)
  {
    Texture2D rv = new Texture2D(1, 1);
    byte[] jpegBytes = Util.UnGZip(zippedJpegBytes);
    rv.LoadImage(jpegBytes, nonReadable);
    return rv;
  }

  public static IEnumerable<string> EnumerateLines(this string s)
  {
    using (System.IO.StringReader reader = new System.IO.StringReader(s))
    {
      while (true)
      {
        string line = reader.ReadLine();
        if (line == null)
        {
          break;
        }
        yield return line;
      }
    }
  }

  public struct LineWithNumber
  {
    public int number;   // base 1
    public string line;
  }

  public static IEnumerable<LineWithNumber> EnumerateNumberedLines(this string s)
  {
    using (System.IO.StringReader reader = new System.IO.StringReader(s))
    {
      int num = 0;
      while (true)
      {
        string line = reader.ReadLine();
        num++;
        if (line == null)
        {
          break;
        }
        yield return new LineWithNumber { number = num, line = line };
      }
    }
  }

  // Common paths that vary from machine to machine.
  public enum AbstractLocation
  {
    StreamingAssets = 0,
    UserDocuments = 1,
    PersistentData = 2,
  }

  public static string GetConcretePath(this AbstractLocation abstractPath)
  {
    switch (abstractPath)
    {
      case AbstractLocation.StreamingAssets:
        return Application.streamingAssetsPath;
      case AbstractLocation.UserDocuments:
        // This will be the PARENT directory of GameBuilderUserData
#if UNITY_EDITOR
        return IO.Directory.GetParent(IO.Directory.GetCurrentDirectory()).FullName;
#else
      // Keep things in our Steam install folder (just cwd). Confirmed
      // that uninstall from Steam does NOT delete these extra files. Also, Steam
      // Cloud Sync is configured for "GameBuilderUserData" in this folder.
        return IO.Directory.GetCurrentDirectory();
#endif
      case AbstractLocation.PersistentData:
        return Application.persistentDataPath;
      default:
        throw new System.Exception($"Unknown AbstractPath value: {abstractPath}");
    }
  }

  [System.Serializable]
  public struct AbstractPath
  {
    public AbstractLocation root;

    // The rest of the path relative to the abstract root.
    public string relativePath;

    public string GetAbsolute()
    {
      return System.IO.Path.Combine(root.GetConcretePath(), relativePath);
    }
  }

  public static string NormalizeLineEndings(this string original)
  {
    return original.Replace("\r\n", "\n");
  }

  public static bool IsNotNullNorEmpty(string s)
  {
    return s != null && s.Length > 0;
  }

  public static T ReadFromJson<T>(string filePath)
  {
    if (!System.IO.File.Exists(filePath))
    {
      return default(T);
    }
    return JsonUtility.FromJson<T>(System.IO.File.ReadAllText(filePath));
  }

  public static int WithBit(this int x, int bitIndex, bool onOroff)
  {
    if (onOroff)
    {
      return x | (1 << bitIndex);
    }
    else
    {
      return x & ~(1 << bitIndex);
    }
  }

  public static ulong WithBit(this ulong x, int bitIndex, bool onOroff)
  {
    if (onOroff)
    {
      return x | (ulong)(1 << bitIndex);
    }
    else
    {
      return x & ~(ulong)(1 << bitIndex);
    }
  }

  public static bool GetBit(this ulong x, int bitIndex)
  {
    return (x & (ulong)(1 << bitIndex)) == 0 ? false : true;
  }

  public static void DebugDrawBox(Vector3 center, Vector3 halfExtents, Color color, float duration)
  {
    Vector3 c = center;
    Vector3 h = halfExtents;
    Vector3 hx = new Vector3(h.x, 0, 0);
    Vector3 hy = new Vector3(0, h.y, 0);
    Vector3 hz = new Vector3(0, 0, h.z);
    Vector3 t0 = c - hx - hz + hy;
    Vector3 t1 = c - hx + hz + hy;
    Vector3 t2 = c + hx + hz + hy;
    Vector3 t3 = c + hx - hz + hy;
    Vector3 b0 = c - hx - hz - hy;
    Vector3 b1 = c - hx + hz - hy;
    Vector3 b2 = c + hx + hz - hy;
    Vector3 b3 = c + hx - hz - hy;

    Debug.DrawLine(t0, t1, color, duration);
    Debug.DrawLine(t1, t2, color, duration);
    Debug.DrawLine(t2, t3, color, duration);
    Debug.DrawLine(t3, t0, color, duration);

    Debug.DrawLine(b0, b1, color, duration);
    Debug.DrawLine(b1, b2, color, duration);
    Debug.DrawLine(b2, b3, color, duration);
    Debug.DrawLine(b3, b0, color, duration);

    Debug.DrawLine(t0, b0, color, duration);
    Debug.DrawLine(t1, b1, color, duration);
    Debug.DrawLine(t2, b2, color, duration);
    Debug.DrawLine(t3, b3, color, duration);
  }

  public static void AssertUniformScale(Vector3 scale)
  {
#if UNITY_EDITOR
    //Debug.Assert(Mathf.Abs(scale.x - scale.y) < 1e-4);
    //Debug.Assert(Mathf.Abs(scale.x - scale.z) < 1e-4);
#endif
  }

  public static TComponent GetAndCache<TComponent>(MonoBehaviour self, ref TComponent cached)
  {
    if (cached == null)
    {
      cached = self.GetComponent<TComponent>();
    }
    return cached;
  }

  public static void AssertAllUnique<T>(T[] values)
  {
    HashSet<T> seen = new HashSet<T>();
    foreach (T val in values)
    {
      if (seen.Contains(val))
      {
        throw new System.Exception($"Duplicate found: {val}");
      }
      seen.Add(val);
    }
  }

  public static void ForSortedGroups<T, K>(
    IEnumerable<T> sortedItems,
    System.Func<T, K> getKeyFunction,
    System.Action<K, IEnumerable<T>> processGroupFunction)
  {
    K lastKey = default(K);

    // Maybe we can be more clever and do this without garbage..like some kind
    // of nesting of enumerables?
    List<T> currentGroup = new List<T>();

    foreach (T item in sortedItems)
    {
      K currentKey = getKeyFunction(item);

      if (!currentKey.Equals(lastKey) && currentGroup.Count > 0)
      {
        processGroupFunction(lastKey, currentGroup);
        currentGroup.Clear();
      }

      currentGroup.Add(item);
      lastKey = currentKey;
    }

    // Do the last one
    if (currentGroup.Count > 0)
    {
      processGroupFunction(lastKey, currentGroup);
      currentGroup.Clear();
    }
  }

  // Make sure this is kept in sync with our Steam Cloud Sync settings. In auto-cloud directories.
  public static string UserDataDirName = "GameBuilderUserData";

  public static string GetUserDataDir()
  {
    return IO.Path.Combine(AbstractLocation.UserDocuments.GetConcretePath(), UserDataDirName);
  }

  public static bool UpgradeUserDataDir()
  {
    // We used to store things in Documents, but that has issues, such as Windows security blocking us.
    string oldDirectory = IO.Path.Combine(
      System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
      "GameBuilderData");

    if (!IO.Directory.Exists(oldDirectory) || !Util.AnyFilesInDirectory(oldDirectory))
    {
      // Don't bother - there's nothing here of value.
      return false;
    }

    // Copy to new if it DNE
    string newDirectory = GetUserDataDir();

    if (IO.Directory.Exists(newDirectory))
    {
      // Don't do anything - already upgraded.
      return false;
    }

    Util.Log($"Copying {oldDirectory} to {newDirectory}");
    Util.CopyDirectoryRecursive(oldDirectory, newDirectory);
    return true;
  }

  static MD5 SharedMd5 = MD5.Create();

  public static string GetMd5Hash(string input)
  {
    return BitConverter.ToString(SharedMd5.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLowerInvariant();
  }

  public class TimeWarning : System.IDisposable
  {
    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

    string label;
    int maxMilliseconds;

    public TimeWarning(string label, int maxMilliseconds)
    {
      this.label = label;
      this.maxMilliseconds = maxMilliseconds;
      sw.Restart();
    }

    public void Dispose()
    {
      long elapsedMs = sw.ElapsedMilliseconds;
      if (elapsedMs > maxMilliseconds)
      {
        Util.LogWarning($"WARNING: code block '{label}' exceeded {maxMilliseconds} ms. Actually took {elapsedMs} ms.");
      }
    }
  }

  public static TimeWarning WarnIfSlow(string label, int maxMilliseconds)
  {
    return new TimeWarning(label, maxMilliseconds);
  }

  static Color[] PlayerColors = new Color[] {
    new Color(212/255f, 34f/255f, 0.0f),
    new Color(0, 123f/255f, 186f/255f),
    new Color(6f/255f, 188f/255f, 117f/255f),
    new Color(255f/255f, 153f/255f, 0f/255f),
    new Color(151f/255f, 27f/255f, 158f/255f),
    new Color(73f/255f, 73f/255f, 73f/255f),
    new Color(250f/255f, 250f/255f, 250f/255f), // Not full white because of hacky tint thing we do.
    new Color(255f/255f, 255f/255f, 0f/255f),
  };

  static string[] PlayerNames = new string[] {
    "Red", "Blue", "Green", "Orange", "Purple", "Black", "White", "Yellow"
  };

  public static Color GetPlayerColor(int playerSlotNumber)
  {
    playerSlotNumber = Mathf.Max(0, playerSlotNumber);
    return PlayerColors[playerSlotNumber % PlayerColors.Length];
  }

  public static string GetPlayerName(int playerSlotNumber)
  {
    playerSlotNumber = Mathf.Max(0, playerSlotNumber);
    return PlayerNames[playerSlotNumber % PlayerNames.Length];
  }

  public static string Generate32CharGuid()
  {
    return System.Guid.NewGuid().ToString("N");
  }

  public static bool IsNullOrEmpty(this string s)
  {
    return s == null || s.Length == 0;
  }

  public static string OrDefault(this string s, string defaultValue)
  {
    return s.IsNullOrEmpty() ? defaultValue : s;
  }

  public static string DenormalizeToSystemSlashes(this string path)
  {
    return path.Replace('/', System.IO.Path.DirectorySeparatorChar);
  }

  public static bool ContainsDirectorySeparators(this string path)
  {
    return path.Contains("/") || path.Contains("" + System.IO.Path.DirectorySeparatorChar);
  }

  public static string NormalizeSlashes(this string path)
  {
    return path.Replace(System.IO.Path.DirectorySeparatorChar, '/');
  }

  public static ProfileBlock Profile(string label)
  {
    return new ProfileBlock(label);
  }

  public static void SetLayerRecursively(GameObject root, int newLayer)
  {
    root.layer = newLayer;
    foreach (Transform child in root.transform)
    {
      SetLayerRecursively(child.gameObject, newLayer);
    }
  }

  public static string NullIfEmpty(this string s)
  {
    return s == "" ? null : s;
  }

  public static string EmptyIfNull(this string s)
  {
    return s == null ? "" : s;
  }

  public struct PinnedHandle : System.IDisposable
  {
    public GCHandle handle;

    public PinnedHandle(object objectToPin)
    {
      handle = GCHandle.Alloc(objectToPin, GCHandleType.Pinned);
    }

    public void Dispose()
    {
      handle.Free();
    }

    public System.IntPtr GetPointer()
    {
      return handle.AddrOfPinnedObject();
    }
  }

  public static PinnedHandle Pin(object objectToPin)
  {
    return new PinnedHandle(objectToPin);
  }

  public static byte[] GZip(byte[] inBytes)
  {
    using (var ms = new System.IO.MemoryStream())
    {
      using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress))
      {
        gz.Write(inBytes, 0, inBytes.Length);
      }
      return ms.ToArray();
    }
  }

  public static byte[] GZipString(string str)
  {
    return GZip(Encoding.UTF8.GetBytes(str));
  }

  public static string UnGZipString(byte[] zipped)
  {
    return Encoding.UTF8.GetString(UnGZip(zipped));

  }

  public static System.IO.Compression.GZipStream UnzipStream(byte[] zippedBytes)
  {
    var zipped = new System.IO.MemoryStream(zippedBytes, 0, zippedBytes.Length);
    return new System.IO.Compression.GZipStream(zipped, System.IO.Compression.CompressionMode.Decompress);
  }

  public static byte[] UnGZip(byte[] zippedBytes)
  {
    using (var os = new System.IO.MemoryStream())
    {
      using (var zipped = new System.IO.MemoryStream(zippedBytes, 0, zippedBytes.Length))
      using (var gz = new System.IO.Compression.GZipStream(zipped, System.IO.Compression.CompressionMode.Decompress))
      {
        gz.CopyTo(os);
      }

      return os.ToArray();
    }
  }

  // Computes moving average for growth rate of a monotonicly growing value.
  // NOTE: We assume *monotonic growth*, so if the value ever goes down, we'll
  // assume this indicates a reset and thus reset the moving window.
  public class GrowthRateMovingAverage
  {
    struct Sample
    {
      public float value;
      public float absoluteTimeSeconds;
    }

    // Most recent is last
    private LinkedList<Sample> sampleWindow = new LinkedList<Sample>();
    private float previousValue;
    private int windowSize;

    public GrowthRateMovingAverage(int windowSize)
    {
      this.windowSize = windowSize;
    }

    public void AddSample(float value, float absoluteTimeSeconds)
    {
      // Less than..
      if (value < previousValue)
      {
        // Stats must have been reset. Reset our windows too.
        sampleWindow.Clear();
      }
      previousValue = value;

      sampleWindow.AddLast(new Sample { value = value, absoluteTimeSeconds = absoluteTimeSeconds });

      while (sampleWindow.Count > windowSize)
      {
        sampleWindow.RemoveFirst();
      }
    }

    public bool CanCompute()
    {
      // Wait for a filled window before computing.
      return sampleWindow.Count >= windowSize;
    }

    public float ComputeGrowthRate()
    {
      if (!CanCompute())
      {
        throw new System.Exception("Do not call this if CanCompute is false!");
      }
      Sample first = sampleWindow.First.Value;
      Sample last = sampleWindow.Last.Value;
      float delta = (last.value - first.value);
      float elapsedSecs = (last.absoluteTimeSeconds - first.absoluteTimeSeconds);
      return delta / elapsedSecs;
    }
  }

  public class MaxTracker
  {
    private GameBuilder.CircularBuffer<float> window;
    private float minValue;

    public MaxTracker(int windowSize, float minValue = 0f)
    {
      this.minValue = minValue;
      window = new GameBuilder.CircularBuffer<float>(windowSize);
    }

    public void RecordValue(float value)
    {
      window.Add(value);
    }

    public float GetMax()
    {
      float max = minValue;
      foreach (float val in window)
      {
        max = Mathf.Max(val, max);
      }
      return max;
    }
  }

  public static void CopyToUserClipboard(string contents)
  {
    TextEditor editor = new TextEditor();
    editor.text = contents;
    editor.SelectAll();
    editor.Copy();
  }

  public static Color ColorFromHex(string hexCode)
  {
    Color rv;
    if (!ColorUtility.TryParseHtmlString(hexCode, out rv))
    {
      throw new System.Exception($"Could not parse hex color code: {hexCode}");
    }
    return rv;
  }

  public class ReusableListPool<T>
  {
    private int currentIndex = 0;
    private List<T> pool;

    private System.Func<T> createInstance;

    public T Get()
    {
      if (currentIndex >= pool.Count)
      {
        pool.Add(createInstance());
      }
      return pool[currentIndex++];
    }

    public void Reset()
    {
      currentIndex = 0;
    }

    public ReusableListPool(System.Func<T> createInstance)
    {
      this.createInstance = createInstance;
    }
  }

  public static T GetLast<T>(this T[] array)
  {
    return array[array.Length - 1];
  }

  public static string ReplaceAllSequentially(this string s, Dictionary<string, string> newByOlds)
  {
    foreach (var entry in newByOlds)
    {
      s = s.Replace(entry.Key, entry.Value);
    }

    return s;
  }

  public static string MakeRelativePath(this string absPath, string relativeTo)
  {
    System.Uri absUri = new System.Uri(absPath, System.UriKind.Absolute);
    System.Uri relativeToUri = new System.Uri(relativeTo, System.UriKind.Absolute);
    return System.Uri.UnescapeDataString(relativeToUri.MakeRelativeUri(absUri).ToString());
  }

  public static Sprite TextureToSprite(Texture2D texture)
  {
    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
  }

  public static void FindRectCornersFromDifferentCanvas(RectTransform referenceRect, RectTransform canvasRect, out Vector2 cornerMin, out Vector2 cornerMax)
  {
    Vector3[] referenceCorners = new Vector3[4];
    referenceRect.GetWorldCorners(referenceCorners);

    Vector2 referenceScreenCornerMin = RectTransformUtility.WorldToScreenPoint(null, referenceCorners[0]);
    Vector2 referenceScreenCornerMax = RectTransformUtility.WorldToScreenPoint(null, referenceCorners[2]);
    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, referenceScreenCornerMin, null, out cornerMin);
    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, referenceScreenCornerMax, null, out cornerMax);
  }

  public static Vector2 FindRectTransformScreenPoint(RectTransform referenceRect)//,out Vector2 screenPoint)
  {
    Vector3[] referenceCorners = new Vector3[4];
    referenceRect.GetWorldCorners(referenceCorners);

    Vector2 referenceScreenCornerMin = RectTransformUtility.WorldToScreenPoint(null, referenceCorners[0]);
    Vector2 referenceScreenCornerMax = RectTransformUtility.WorldToScreenPoint(null, referenceCorners[2]);
    return Vector2.Lerp(referenceScreenCornerMin, referenceScreenCornerMax, 0.5f);
  }

  // TODO add error handler
  public static void DownloadFileToDisk(string url, string destPath, System.Action onComplete, System.Action<string> onError)
  {
    var req = UnityEngine.Networking.UnityWebRequest.Get(url);
    req.method = UnityEngine.Networking.UnityWebRequest.kHttpVerbGET;
    var downloadHandler = new UnityEngine.Networking.DownloadHandlerFile(destPath);
    downloadHandler.removeFileOnAbort = true;
    req.downloadHandler = downloadHandler;
    req.SendWebRequest().completed += op =>
    {
      if (req.error != null)
      {
        onError?.Invoke(req.error);
      }
      else
      {
        onComplete();
      };
    };
  }

  // 'onError' will be called only once, with a collection of all errors encountered.
  public static void DownloadFilesToDisk(Dictionary<string, string> url2path, System.Action onComplete, System.Action<IEnumerable<string>> onError)
  {
    HashSet<string> awaitingUrls = new HashSet<string>(url2path.Keys);
    List<string> errors = new List<string>();

    System.Action onChange = () =>
    {
      if (awaitingUrls.Count == 0)
      {
        if (errors.Count == 0)
        {
          onComplete?.Invoke();
        }
        else
        {
          onError?.Invoke(errors);
        }
      }
    };

    foreach (var entry in url2path)
    {
      string url = entry.Key;
      string localPath = entry.Value;

      Util.DownloadFileToDisk(url, localPath,
      () =>
      {
        // Done downloading
        awaitingUrls.Remove(url);
        onChange();
      },
      error =>
      {
        awaitingUrls.Remove(url);
        errors.Add(error);
        onChange();
      }
      );
    }
  }

  public static string CreateTempDirectory()
  {
    string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
    System.IO.Directory.CreateDirectory(path);
    return path;
  }

  public static bool BoolWave(float t, float halfPeriod)
  {
    Debug.Assert(t > 0f);
    Debug.Assert(halfPeriod > 0f);
    return ((t / (2f * halfPeriod)) % 1f) > 0.5f;
  }

  // Returns a new'd instance if json string is null or empty
  public static T FromJsonSafe<T>(string json) where T : new()
  {
    if (json.IsNullOrEmpty())
    {
      return new T();
    }
    else
    {
      return JsonUtility.FromJson<T>(json);
    }
  }

  public interface IDeepCloneable<T>
  {
    T DeepClone();
  }

  public static T[] DeepClone<T>(this T[] source) where T : IDeepCloneable<T>
  {
    if (source == null)
    {
      return null;
    }
    T[] rv = new T[source.Length];
    for (int i = 0; i < source.Length; i++)
    {
      rv[i] = source[i].DeepClone();
    }
    return rv;
  }

  public static int IndexOfWhere<T>(this T[] array, System.Func<T, bool> clause)
  {
    for (int i = 0; i < array.Length; i++)
    {
      if (clause(array[i]))
      {
        return i;
      }
    }
    return -1;
  }

  public static bool Includes<T>(this T[] array, T needle)
  {
    return Array.IndexOf(array, needle) != -1;
  }

  public struct DummyDisposable : IDisposable
  {
    public void Dispose()
    {
    }
  }

  public static void ShuffleList<T>(List<T> list, System.Random rng)
  {
    int n = list.Count;
    while (n > 1)
    {
      n--;
      int k = rng.Next(n + 1);
      T value = list[k];
      list[k] = list[n];
      list[n] = value;
    }
  }

  public static T[] ExpensiveWith<T>(this T[] array, T newItem)
  {
    List<T> list = new List<T>(array);
    list.Add(newItem);
    return list.ToArray();
  }

  public static T AtFractionalPosition<T>(this T[] array, float fraction) where T : IComparable<T>
  {
    // Sanity check that this is sorted ascending.
    Debug.Assert(array[0].CompareTo(array.GetLast()) <= 0);
    Debug.Assert(0 <= fraction);
    Debug.Assert(fraction <= 1f);
    // See unit test for why we use floor.
    int i = Mathf.CeilToInt(fraction * (array.Length - 1));
    return array[i];
  }

  public static bool AnyFilesInDirectory(string dir)
  {
    foreach (string newPath in IO.Directory.GetFiles(dir, "*.*",
        IO.SearchOption.AllDirectories))
    {
      return true;
    }
    return false;
  }

  // Suppose directory 'A' has files 'foo' and 'bar' immediately in it. Then you called: CopyDirectoryRecursive("A", "B").
  // Then, directory "B" would have files 'foo' and 'bar' immediately in it.
  public static void CopyDirectoryRecursive(string theDirectory, string newParentDir)
  {
    foreach (string dirPath in IO.Directory.GetDirectories(theDirectory, "*",
        IO.SearchOption.AllDirectories))
    {
      IO.Directory.CreateDirectory(dirPath.Replace(theDirectory, newParentDir));
    }

    foreach (string newPath in IO.Directory.GetFiles(theDirectory, "*.*",
        IO.SearchOption.AllDirectories))
    {
      IO.File.Copy(newPath, newPath.Replace(theDirectory, newParentDir), true);
    }
  }

  public static void SetFilterModeOnAllTextures(GameObject root, FilterMode mode)
  {
    foreach (var render in root.GetComponentsInChildren<Renderer>())
    {
      foreach (var mat in render.sharedMaterials)
      {
        if (mat == null) continue;
        foreach (int texProp in mat.GetTexturePropertyNameIDs())
        {
          if (mat.GetTexture(texProp) != null)
          {
            mat.GetTexture(texProp).filterMode = mode;
          }
        }
      }
    }
  }

  public static Vector3 GetClosestPointOnRayFromRay(Ray targetRay, Ray otherRay)
  {
    Vector3 betweenRayOrigins = otherRay.origin - targetRay.origin;

    float raySquared = Vector3.Dot(otherRay.direction, otherRay.direction);
    float raysDot = Vector3.Dot(otherRay.direction, targetRay.direction);
    float axisRaySquared = Vector3.Dot(targetRay.direction, targetRay.direction);

    float rayDotBetween = Vector3.Dot(otherRay.direction, betweenRayOrigins);
    float axisRayDotBetween = Vector3.Dot(targetRay.direction, betweenRayOrigins);

    //saw in two example equations but have no idea what this means
    return targetRay.GetPoint((raySquared * axisRayDotBetween - rayDotBetween * raysDot) / (raySquared * axisRaySquared - raysDot * raysDot));
  }

  public static bool AreListsEqual<T>(IList<T> a, IList<T> b) where T : IEquatable<T>
  {
    if (a.Count != b.Count)
    {
      return false;
    }

    for (int i = 0; i < a.Count; i++)
    {
      if (!a[i].Equals(b[i])) return false;
    }

    return true;
  }

  public static string ToValidFileName(this string path)
  {
    return String.Join("_",
         path.Split(IO.Path.GetInvalidFileNameChars(),
         StringSplitOptions.RemoveEmptyEntries));
  }

  const string WorkshopUrlPrefix = "https://steamcommunity.com/sharedfiles/filedetails/?";

  public static ulong ExtractIdFromWorkshopUrl(string rawUrl)
  {
    try
    {
      if (rawUrl.IsNullOrEmpty()) return 0;
      if (!rawUrl.ToLowerInvariant().StartsWith(WorkshopUrlPrefix.ToLowerInvariant())) return 0;
      var parameters = rawUrl.ToLowerInvariant().Substring(WorkshopUrlPrefix.Length);
      foreach (string paramEx in parameters.Split('&'))
      {
        var parts = paramEx.Split('=');
        if (parts.Length != 2) continue;
        if (parts[0] != "id") continue;
        return System.UInt64.Parse(parts[1]);
      }
    }
    catch (System.Exception)
    {
    }
    return 0;
  }
}
