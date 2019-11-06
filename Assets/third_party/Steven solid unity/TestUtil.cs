using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;

public static class TestUtil
{
  public class TestScene
  {
    string sceneName;

    List<GameObject> rootObjs = null;

    bool loaded = false;

    public TestScene(string sceneName)
    {
      this.sceneName = sceneName;
    }

    // Usage: "yield return scene.LoadAndWait();"
    // TODO: Ideally, this would be a static method we could 'await' on,
    // and the class returned would be LoadedTestScene.
    public IEnumerator LoadAndWait()
    {
      UnityEngine.SceneManagement.SceneManager.LoadScene(this.sceneName);

      // Takes one more frame to actually load.
      yield return null;

      loaded = true;
    }

    string GetLogPrefix()
    {
      return "In test scene '" + this.sceneName + "': ";
    }

    // This will assert-fail if not found.
    public GameObject FindRootObject(string objName)
    {
      Debug.Assert(loaded);

      if (rootObjs == null)
      {
        rootObjs = new List<GameObject>();
        // TODO can we check that we're the active scene? Or store a ref to 'scene'?
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(rootObjs);
      }

      GameObject obj = rootObjs.Find(o => o.name == objName);
      Assert.IsNotNull(obj, GetLogPrefix() + "Could not find root test object named '" + objName + "'.");
      return obj;
    }

    // This will assert-fail if the object isn't found OR if it doesn't have the expected component.
    public TComponent FindRootComponent<TComponent>(string objName)
    {
      GameObject obj = this.FindRootObject(objName);
      TComponent component = obj.GetComponent<TComponent>();
      Assert.IsNotNull(component, GetLogPrefix() + "Root object '" + objName + "' did not have expected component: "
        + typeof(TComponent).Name);
      return component;
    }

  }
}