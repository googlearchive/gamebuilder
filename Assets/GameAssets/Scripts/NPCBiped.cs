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

public class NPCBiped : MonoBehaviour
{
  const float IdealWalkSpeed = 1f;
  const float IdealRunSpeed = 6f;

  [SerializeField] IBipedDriver driver;
  [SerializeField] bool ignoreLook = false;
  [SerializeField] float inputDampTime = 0.15f;

  // Full run animation is for 5m/s.
  // Full value is 1.
  [SerializeField] float throttleToInputScale = 1f / 5f;

  Animator animator;
  HeadLookController lookController;

  float heightFromGround;

  Vector3 dampedForward = Vector3.forward;
  Vector3 prevDampedThrottle = Vector3.zero;

  ////////////////////////////////////////////////////////////////////////////

  private void Awake()
  {
    animator = GetComponent<Animator>();
    lookController = GetComponent<HeadLookController>();

    if (driver == null)
    {
      SetDriver(GetComponentInParent<IBipedDriver>());
    }
  }

  public void SetDriver(IBipedDriver newDriver)
  {
    if (newDriver == driver) return;
    if (driver != null)
    {
      driver.onJump -= OnJump;
    }
    driver = newDriver;
    if (driver != null)
    {
      driver.onJump += OnJump;
    }
  }

  void OnEnable()
  {
  }

  void OnDisable()
  {
    SetDriver(null);
  }

  Vector3 DampDirection(Vector3 current, Vector3 target)
  {
    Quaternion fullRot = Quaternion.FromToRotation(current, target);
    // TODO make this dependent on deltaTime.
    Quaternion partialRot = Quaternion.Slerp(Quaternion.identity, fullRot, LerpForHalfLife(0.15f));
    return partialRot * current;
  }

  Vector3 DampHeading(Vector3 current, Vector3 target)
  {
    Debug.Assert(current.y == 0);
    Debug.Assert(target.y == 0);

    float degs = Vector2.SignedAngle(current.GetXZ(), target.GetXZ());
    // Debug.DrawRay(transform.position, prevMoveForward.AsXZVec(), Color.red, 0f, false);
    // Debug.DrawRay(transform.position, targetMove2.AsXZVec(), Color.green, 0f, false);
    // Debug.Log($"degs {degs}");
    float frameDegs = degs * LerpForHalfLife(0.14f);
    Quaternion xzRot = Quaternion.AngleAxis(-frameDegs, Vector3.up);
    return xzRot * current;
  }

  void DrawOffset(Vector3 offset, Color color)
  {
    Debug.DrawLine(transform.position, transform.position + offset, color, 0f, false);
  }

  ////////////////////////////////////////////////////////////////////////////

  // This is something I made up..essentially, if you just do a normal lerp
  // for throttle, it's kinda weird when you switch between backwards and
  // forwards and you're at an angle (so imagine with WASD controls, you have
  // W+A held, then you suddenly just have S held). When that kind of switch
  // happens, this lerp will first go through zero, effectively stopping the
  // legs animation for a bit, then continuing in the other direction.
  Vector3 ThroughZeroLerp(Vector3 a, Vector3 b, Vector3 forward, float t)
  {
    if (a.magnitude < 1e-4 || b.magnitude < 1e-4)
    {
      return Vector3.Lerp(a, b, t);
    }
    else if (Mathf.Sign(Vector3.Dot(forward, a)) == Mathf.Sign(Vector3.Dot(forward, b)))
    {
      // Both forward or both backward
      // Normal lerp
      return Vector3.Lerp(a, b, t);
    }
    else
    {
      // Go through zero first...
      float ma = a.magnitude;
      float mb = b.magnitude;
      float zt = ma / (ma + mb);
      if (t < zt)
      {
        return (1f - t / zt) * a;
      }
      else
      {
        return ((t - zt) / (1 - zt)) * b;
      }
    }
  }

  float LerpForHalfLife(float halfLife)
  {
    return 1f - Mathf.Pow(0.5f, Time.deltaTime / halfLife);
  }

  void Update()
  {
    if (driver == null || !driver.IsValid())
    {
      lookController.enabled = false;
      return;
    }

    // Damp any change to our root heading.
    dampedForward = DampHeading(dampedForward, driver.GetLookDirection().GetForwardHeading());
    transform.rotation = Quaternion.LookRotation(dampedForward, Vector3.up);
    Debug.Assert(Mathf.Abs(transform.forward.y) < 1e-4);

    // Damp throttle to avoid pops. See the comment for ThroughZeroLerp for
    // details.
    prevDampedThrottle = ThroughZeroLerp(prevDampedThrottle, driver.GetMoveThrottle(), transform.forward, LerpForHalfLife(0.1f));
    //TEMP
    prevDampedThrottle = driver.GetMoveThrottle();

    // This code gives the throttle *relative* to the look
    // direction. So if inputX is positive, then we're strafing right by
    // some amount. If we're just moving forward, inputX should be 0, and
    // inputY should be positive. If inputX is 0 and inputY is negative,
    // we're moving straight backward.
    Vector3 relThrottle = transform.InverseTransformDirection(prevDampedThrottle.normalized) * prevDampedThrottle.magnitude;
    float speed = relThrottle.magnitude;
    float blendMagnitude = 1f;
    if (speed < 4.0f)
    {
      Debug.Assert(speed < IdealRunSpeed);
      // Walking.
      blendMagnitude = 0.5f;
      animator.SetFloat("LocomotionSpeedScale", speed / IdealWalkSpeed);
    }
    else
    {
      // Running
      blendMagnitude = 1f;
      animator.SetFloat("LocomotionSpeedScale", speed / IdealRunSpeed);
    }
    Vector2 blendCoord = new Vector2(relThrottle.x, relThrottle.z).normalized * blendMagnitude;
    animator.SetFloat("InputX", blendCoord.x, inputDampTime, Time.deltaTime);
    animator.SetFloat("InputY", blendCoord.y, inputDampTime, Time.deltaTime);

    // calculate the distance to the ground (which is on it's own layer) and pass that info to the animator so we know how high up we are
    int layer_mask = LayerMask.GetMask("Ground");

    RaycastHit hit = new RaycastHit();
    if (Physics.Raycast(transform.position, -Vector3.up, out hit, layer_mask))
    {
      heightFromGround = hit.distance;
    }

    animator.SetFloat("HeightFromGround", heightFromGround);
    animator.SetBool("IsGrounded", driver.IsGrounded());

    if (!ignoreLook)
    {
      lookController.enabled = true;
      lookController.lookDirWorld = driver.GetLookDirection();
    }
    else
    {
      lookController.enabled = false;
    }

    DrawOffset(driver.GetLookDirection().GetForwardHeading(), Color.red);
    DrawOffset(driver.GetMoveThrottle(), Color.yellow);
    DrawOffset(prevDampedThrottle, Color.blue);
    DrawOffset(transform.forward, Color.green);
  }

  ////////////////////////////////////////////////////////////////////////////

  // if we're jumping, set the animator trigger
  void OnJump()
  {
    animator.SetTrigger("IsJumping");
  }

  ////////////////////////////////////////////////////////////////////////////
}
