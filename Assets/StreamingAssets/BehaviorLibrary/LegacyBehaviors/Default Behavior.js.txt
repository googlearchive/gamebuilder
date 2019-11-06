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

// Kickable<size=70%>\nAllows this actor to be affected by velocity requests from other actors, when they use the 'kick' function.

// WARNING: This built-in script is necessary for things to work as expected.
// Don't modify or delete this script! -- Unless you have a good reason
// to do so, of course :-)

// Respond to the AddVelocityChange message (which is sent by the push() functions).
export function onAddVelocityChange(msg) {
  const dv = msg.velocityChange;
  if (!isKinematic()) {
    // If I am not kinematic, use addVelocity, since my physics behavior is based on
    // normal physics, so I'm subject to forces, collisions, etc.
    addVelocity(dv);
  }
  else if (msg.affectKinematic === true) {
    const actor = ApiV2Context.instance.getActor();
    const prevDv = actor.getStickyDesiredVelocity();
    const newDv = vec3add(prevDv, dv);
    actor.setStickyDesiredVelocity(newDv);
    actor.setUseStickyDesiredVelocity(true);
  }
}

export function onChangeVar(msg) {
  if (msg.op === undefined || msg.value === undefined || msg.name === undefined) {
    logError("ChangeVar message needs an 'op' and 'value' and 'name' fields.");
    return;
  }
  const curValueAsNum = +(getVar(msg.name) || 0);
  const operandAsNum = +(msg.value || 0);
  switch (msg.op) {
    case "SET":
      setVar(msg.name, msg.value);
      return;
    case "ADD":
      setVar(msg.name, curValueAsNum + operandAsNum);
      return;
    case "SUBTRACT":
      setVar(msg.name, curValueAsNum - operandAsNum);
      return;
    case "MULTIPLY":
      setVar(msg.name, curValueAsNum * operandAsNum);
      return;
    default:
      logError("Invalid operation for ChangeVar: " + msg.op);
  }
  if (msg.clampMin !== undefined && msg.clampMax !== undefined) {
    setVar(msg.name, clamp(+getVar(msg.name), msg.clampMin, msg.clampMax));
  }
}

export function onResetGame() {
  ApiV2Context.instance.getActor().setUseStickyDesiredVelocity(false);
}

export function onPoliteRequest(msg) {
  const verb = msg.verb;
  const args = msg.args;
  assertString(verb, 'PoliteRequest message needs a verb, was given ' + verb);
  assert(Array.isArray(args), 'PoliteRequest needs arguments as an Array. Was given ' + args);

  switch (verb) {
    // BEGIN_GAME_BUILDER_CODE_GEN REMOTE_API_HANDLER_CASES_JAVASCRIPT
    case 'setPos': setPos.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setYaw': setYaw.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setPitch': setPitch.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setRoll': setRoll.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setYawPitchRoll': setYawPitchRoll.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'turn': turn.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'applyQuaternion': applyQuaternion.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'applyQuaternionSelf': applyQuaternionSelf.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setRot': setRot.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'resetRot': resetRot.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'lookAt': lookAt.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'lookDir': lookDir.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setScaleUniform': setScaleUniform.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setScale': setScale.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'attachToParent': attachToParent.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'detachFromParent': detachFromParent.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setVar': setVar.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'deleteVar': deleteVar.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setDisplayName': setDisplayName.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setCommentText': setCommentText.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setSolid': setSolid.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setKinematic': setKinematic.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'enableGravity': enableGravity.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'enableKeepUpright': enableKeepUpright.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setBounciness': setBounciness.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setMass': setMass.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setDrag': setDrag.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setAngularDrag': setAngularDrag.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setPhysicsPreset': setPhysicsPreset.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'addVelocity': addVelocity.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setCameraActor': setCameraActor.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setIsPlayerControllable': setIsPlayerControllable.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setControllingPlayer': setControllingPlayer.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setBodyPos': setBodyPos.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setBodyRot': setBodyRot.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setTintColor': setTintColor.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'setTintHex': setTintHex.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'show': show.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'hide': hide.apply(null, decodeUndefineds(args)); break;    // GENERATED
    case 'destroySelf': destroySelf.apply(null, decodeUndefineds(args)); break;    // GENERATED
    // END_GAME_BUILDER_CODE_GEN
    default:
      throw new Error("Invalid polite request verb: " + verb);
  }
}