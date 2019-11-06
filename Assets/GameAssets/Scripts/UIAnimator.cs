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

public class UIAnimator : MonoBehaviour {

    public GameObject avatar;
    Animator avatarAnimator;
    
    // Use this for initialization
	void Start () {

        avatarAnimator = avatar.GetComponent<Animator>();
        
	}
	
	// Update is called once per frame
	void Update () {
		
        if (avatarAnimator.GetBool("IsDamaged") == true)
        {
            gameObject.GetComponent<Animator>().SetTrigger("IsDamaged");
        }


        if (avatarAnimator.GetBool("IsDead") == true)
        {
            gameObject.GetComponent<Animator>().SetTrigger("IsDead");
        }
    }
}