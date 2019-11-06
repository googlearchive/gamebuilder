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

public class TestSearchManager : MonoBehaviour
{
  [SerializeField] Sprite testThumbnail;
  [SerializeField] GameObject testRenderable;
  [SerializeField] VoosEngine voosEngine;

  public void RequestRenderable(ActorableSearchResult _requestedResult, RenderableRequestEventHandler requestCallback, int index)
  {
    if (testRenderableRoutine != null) StopCoroutine(testRenderableRoutine);
    testRenderableRoutine = StartCoroutine(TestRenderableRoutine(_requestedResult, requestCallback, index));
  }

  public void Search(string searchstring, OnActorableSearchResult resultCallback)
  {
    Debug.Log("poly search " + searchstring);
    testResultsRoutine = StartCoroutine(TestResultsRoutine(resultCallback));
  }

  public void CancelSearch()
  {
    if (testResultsRoutine != null) StopCoroutine(testResultsRoutine);
  }


  Coroutine testRenderableRoutine;
  IEnumerator TestRenderableRoutine(ActorableSearchResult _requestedResult, RenderableRequestEventHandler requestCallback, int index)
  {
    // yield return new WaitForSeconds(Random.Range(.2f, .5f));
    yield return null;
    requestCallback(Instantiate(testRenderable));
  }

  //routine for testing asynchronous search results
  Coroutine testResultsRoutine;
  IEnumerator TestResultsRoutine(OnActorableSearchResult resultCallback)
  {
    int resultCount = Random.Range(2, 6);

    for (int i = 0; i < resultCount; i++)
    {
      yield return new WaitForSeconds(Random.Range(.2f, .5f));
      resultCallback(CreateTestResult());
    }
  }

  ActorableSearchResult CreateTestResult()
  {
    ActorableSearchResult testresult = new ActorableSearchResult();
    testresult.preferredRotation = Quaternion.identity;
    testresult.renderableReference.assetType = AssetType.Poly;
    testresult.name = "test name";
    testresult.renderableReference.uri = "test uri";
    // TODO
    testresult.thumbnail = testThumbnail.texture;

    return testresult;
  }
}
