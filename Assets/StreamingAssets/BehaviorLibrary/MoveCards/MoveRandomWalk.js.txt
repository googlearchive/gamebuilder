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

// Random walk

export const PROPS = [
  propDecimal("Speed", 1),
  propDecimal("MinTurn", 10),
  propDecimal("MaxTurn", 180),
  propDecimal("MinDistBetweenTurns", 1),
  propDecimal("MaxDistBetweenTurns", 5),
  propDecimal("TurnSpeed", 90), //degrees per second
  propBoolean("MoveWhileTurning", true),
  propBoolean("LimitRange", false),
  propDecimal("MaxRange", 10, { requires: [requireTrue("LimitRange")] }),
]

//a better way to do this?
export function onResetGame() {
  card.walkDist = 0;
  card.walkDistTarget = 0;
  card.turnAmount = 0
  card.turnAmountTarget = 0;
  card.returningHome = false;
  newTargets();
}

export function onActiveTick() {
  if (card.returningHome) {
    returnHomeUpdate();
    return;
  }
  // checks if it reach dist then turn, if neither, just move a bit
  if (reachedTargetDist()) {
    if (reachedTargetTurn()) {
      newTargets();
    } else {
      turnUpdate();
    }
  } else {
    card.walkDist += props.Speed * deltaTime();
    moveForward(props.Speed);
  }

  if (props.LimitRange && props.MaxRange < getDistanceTo(getSpawnPos())) {
    // Wandered too far! Time to go home.
    card.returningHome = true;
  }
}

function turnUpdate() {
  //if speed is 0, just snap to target turn, get new targets, and return
  if (props.TurnSpeed == 0) {
    turn(card.turnAmountTarget);
    newTargets();
    return;
  }

  const turnDelta = degToRad(props.TurnSpeed * deltaTime())
    * Math.sign(card.turnAmountTarget - card.turnAmount);
  card.turnAmount += turnDelta
  turn(turnDelta);

  if (props.MoveWhileTurning) {
    moveForward(props.Speed);
  }
}

// grabs a new distance to walk and turn
function newTargets() {
  card.walkDist = 0;
  card.walkDistTarget = THREE.Math.randFloat(
    props.MinDistBetweenTurns, props.MaxDistBetweenTurns)

  card.turnAmount = 0;
  let randomDegrees = THREE.Math.randFloat(
    props.MinTurn, props.MaxTurn);

  //make it randomly be clockwise versus counterclockwise
  randomDegrees = randomDegrees * (Math.random() > .5 ? -1 : 1);

  card.turnAmountTarget = degToRad(randomDegrees);
}

// have we reached that distance?
function reachedTargetDist() {
  return (card.walkDist || 0) >= (card.walkDistTarget || 0);
}

// have we reached the target turn amount?
function reachedTargetTurn() {
  return Math.abs((card.turnAmountTarget || 0) - (card.turnAmount || 0)) < .01;
}

function returnHomeUpdate() {
  const forward = getForward();
  const toHome = vec3sub(getSpawnPos(), getPos());
  const angleToHomeDegrees = radToDeg(forward.angleTo(toHome));
  lookToward(getSpawnPos(), props.TurnSpeed > 0 ? degToRad(props.TurnSpeed) : 20, true);
  if (vec3dot(getForward(), toHome) > 0 && angleToHomeDegrees < 30) {
    moveForward(props.Speed);
  }
  if (getDistanceTo(getSpawnPos()) < props.MaxRange / 2) {
    card.returningHome = false;
  }
}

export function getCardStatus() {
  return {
    description: `Wanders around randomly with speed <color=green>${props.Speed.toFixed(1)}</color>`
  }
}
