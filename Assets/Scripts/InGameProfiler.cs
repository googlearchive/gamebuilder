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
using System.Collections.Generic;
using SD = System.Diagnostics;
using ST = System.Text;

public class InGameProfiler : MonoBehaviour
{
  static InGameProfiler latest;

  class Node
  {
    public string label;
    public Node parent = null;
    public GameBuilder.CircularBuffer<long> frameTotalTickSamples = new GameBuilder.CircularBuffer<long>(60);
    public List<Node> children = new List<Node>();
    public int currentFrame = -1;
    public long currentFrameTotalTicks = 0;
    public int currentFrameNumCalls = 0;
    public int lastFrameCalls = 0;
    public long currentCallTicks0 = -1;

    // GUI state
    public bool expanded = true;
    public bool highlighted = false;

    public Node(string label, Node parent = null)
    {
      this.label = label;
      this.parent = parent;
    }

    public void UnHighlightSubtree()
    {
      this.highlighted = false;
      foreach (var kid in children)
      {
        kid.UnHighlightSubtree();
      }
    }
  }

  Node root = new Node("ROOT");
  Node current;
  SD.Stopwatch watch = new SD.Stopwatch();

  void Awake()
  {
    latest = this;
    current = root;
  }

  public void Begin(string sectionLabel)
  {
    if (current == root)
    {
      watch.Restart();
    }

    Debug.Assert(current != null);
    Node beginNode = null;
    for (int i = 0; i < current.children.Count; i++)
    {
      if (current.children[i].label == sectionLabel)
      {
        beginNode = current.children[i];
        break;
      }
    }
    if (beginNode == null)
    {
      beginNode = new Node(sectionLabel, current);
      current.children.Add(beginNode);
    }

    Debug.Assert(beginNode.currentCallTicks0 == -1);
    beginNode.currentCallTicks0 = watch.ElapsedTicks;
    current = beginNode;
    UnityEngine.Profiling.Profiler.BeginSample(sectionLabel);

  }

  public void End()
  {
#if UNITY_EDITOR
    Debug.Assert(current != null);
    Debug.Assert(current != root);
    Debug.Assert(current.currentCallTicks0 != -1);
#endif
    UnityEngine.Profiling.Profiler.EndSample();

    // If this is a new frame, consider the last frame "done" and add it.
    if (current.currentFrame != Time.frameCount)
    {
      current.frameTotalTickSamples.Add(current.currentFrameTotalTicks);
      current.currentFrame = Time.frameCount;
      current.currentFrameTotalTicks = 0;
      current.lastFrameCalls = current.currentFrameNumCalls;
      current.currentFrameNumCalls = 0;
    }

    // Add the elapsed ticks to the current total
    long t0 = current.currentCallTicks0;
    current.currentCallTicks0 = -1;
    long t1 = watch.ElapsedTicks;
    current.currentFrameTotalTicks += t1 - t0;
    current.currentFrameNumCalls++;


    current = current.parent;
  }

  ST.StringBuilder sharedBuilder = new ST.StringBuilder();

  void DrawNode(Node node, int depth = 0)
  {
    if (node != root && Time.frameCount - node.currentFrame > 5)
    {
      // This hasn't hit for a while. Draw it greyed out, without recursing.
      GUILayout.Label($"<color=#888888>{node.label}</color>");
      return;
    }

    long totalTicks = 0;
    foreach (long sample in node.frameTotalTickSamples)
    {
      totalTicks += sample;
    }
    float totalMilliseconds = totalTicks * 1000f / SD.Stopwatch.Frequency;
    float avgMs = totalMilliseconds / node.frameTotalTickSamples.Count;

    sharedBuilder.Clear();
    if (node.highlighted)
    {
      sharedBuilder.Append("<color=#000088>");
    }
    for (int i = 0; i < depth; i++)
    {
      sharedBuilder.Append("    ");
    }
    sharedBuilder.Append(node.label);
    sharedBuilder.Append(" :: ");
    sharedBuilder.AppendFormat("{0:0.00} ms", avgMs);
    sharedBuilder.AppendFormat(" ({0} calls)", node.lastFrameCalls);
    if (node.highlighted)
    {
      sharedBuilder.Append("</color>");
    }

    if (!node.expanded && node.children.Count > 0)
    {
      sharedBuilder.Append(" (children hidden)");
    }

    GUILayout.BeginHorizontal();
    bool newExpanded = GUILayout.Toggle(node.expanded, sharedBuilder.ToString());
    bool clicked = newExpanded != node.expanded;
    node.expanded = newExpanded;
    if (clicked)
    {
      // Unhighlight everyone else
      root.UnHighlightSubtree();

      // Highlight us and our ancestors.
      node.highlighted = true;
      var curr = node;
      while (curr != null)
      {
        curr.highlighted = true;
        curr = curr.parent;
      }

    }
    GUILayout.EndHorizontal();

    if (node.expanded)
    {
      for (int i = 0; i < node.children.Count; i++)
      {
        DrawNode(node.children[i], depth + 1);
      }
    }
  }

  void OnGUI()
  {
    int height = 10000;
    GUILayout.BeginArea(new Rect(10, 50, Screen.width, height));
    DrawNode(root);
    GUILayout.EndArea();
  }

  // For "using" pattern
  class SectionDisposable : System.IDisposable
  {
    readonly InGameProfiler instance;

    public SectionDisposable(string label, InGameProfiler instance)
    {
      this.instance = instance ?? InGameProfiler.latest;
      if (this.instance == null) return;
      this.instance.Begin(label);
    }

    public void Dispose()
    {
      if (this.instance == null) return;
      this.instance.End();
    }
  }

  public static System.IDisposable Section(string label, InGameProfiler instance = null)
  {
    return new SectionDisposable(label, instance);
  }

  public static void BeginSection(string label)
  {
    latest.Begin(label);
  }

  public static void EndSection()
  {
    latest.End();
  }

}