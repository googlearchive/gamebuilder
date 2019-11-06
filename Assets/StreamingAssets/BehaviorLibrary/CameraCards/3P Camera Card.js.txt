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

export const PROPS = [
  propDecimal("HeadOffsetX", 0),
  propDecimal("HeadOffsetY", 2),
  propDecimal("HeadOffsetZ", 0),
  propDecimal("CamOffsetHoriz", 1),
  propDecimal("CamDistance", 5),
  propBoolean("IsPitchLocked", false),
  propDecimal("LockedPitch", -20),
  propDecimal("FieldOfView", 60),
]

const SPHERE_CAST_RADIUS_NORMAL = 0.3;
const SPHERE_CAST_RADIUS_LOOKING_UP = 0.55;

export function onCameraTick(msg) {
  if (!exists(msg.target)) return;
  const target = msg.target;
  reparentIfNeeded(target);

  // If yaw is unset (null), initialize with the target's yaw.
  if (card.yaw === null) {
    card.yaw = getYaw(target) || 0;
  }

  // Change yaw/pitch in response to user input.
  card.yaw += getLookAxes(target).x;
  card.pitch = clamp(props.IsPitchLocked ? degToRad(props.LockedPitch) : card.pitch + getLookAxes(target).y,
    degToRad(-80), degToRad(80));
  setYawPitchRoll(card.yaw, -card.pitch, 0);

  setPos(computeCameraPos(target));
  setCameraSettings({
    cursorActive: false,
    aimOrigin: getPos(),
    aimDir: getForward(),
    fov: props.FieldOfView || 60,
  });
}

function computeCameraPos(target) {
  const headPos = selfToWorldPos(vec3(props.HeadOffsetX, props.HeadOffsetY, props.HeadOffsetZ), target);

  // Figure out what radius to use for the sphere cast. When the player
  // is looking up, we use a bigger radius to prevent the camera from
  // clipping the ground.
  const sphereCastRadius = getForward().y > 0 ? SPHERE_CAST_RADIUS_LOOKING_UP :
    SPHERE_CAST_RADIUS_NORMAL;

  // Find the ideal camera position.
  const idealCamPos = vec3add(vec3add(headPos, getRight(props.CamOffsetHoriz)),
    getBackward(props.CamDistance));

  // If from that position I can see the player's head directly
  // with no terrain in the way, that's a good position for the camera.
  let hit = castAdvanced(
    idealCamPos,
    vec3sub(headPos, idealCamPos),
    getDistanceBetween(idealCamPos, headPos),
    sphereCastRadius, CastMode.CLOSEST,
    /* includeActors */ false,
    /* includeSelf */ false,
    /* includeTerrain */ true);
  if (!hit || hit.actor === target) {
    return idealCamPos;
  }

  // If we got here, it means a piece of terrain is in the way between
  // the player's head and the ideal camera position, so we have to do
  // things the hard way: first raycast to the side until we hit something,
  // then raycast back to see how far we can go with the camera in each
  // direction.
  let pos = headPos;
  if (Math.abs(props.CamOffsetHoriz) > 0.01) {
    const sign = props.CamOffsetHoriz > 0 ? 1 : -1;
    hit = castAdvanced(
      headPos,
      getRight(sign),
      Math.abs(props.CamOffsetHoriz),
      sphereCastRadius, CastMode.CLOSEST,
      /* includeActors */ false,
      /* includeSelf */ false,
      /* includeTerrain */ true);
    const allowedDist = hit ? hit.distance - 0.1 : props.CamOffsetHoriz;
    pos = vec3add(pos, getRight(sign * allowedDist));
  }
  hit = castAdvanced(
    pos,
    getBackward(),
    props.CamDistance,
    sphereCastRadius,
    CastMode.CLOSEST,
    /* includeActors */ false,
    /* includeSelf */ false,
    /* includeTerrain */ true);
  const allowedDist = hit ? hit.distance - 0.1 : props.CamDistance;
  pos = vec3add(pos, getBackward(allowedDist));
  return pos;
}

export function onInit() {
  card.yaw = null;  // null means "unset".
  card.pitch = 0;
}

export function reparentIfNeeded(target) {
  if ((target || null) === (getParent() || null)) return;
  if (exists(target)) {
    attachToParent(target);
  } else {
    detachFromParent();
  }
}