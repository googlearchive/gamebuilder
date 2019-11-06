# Copyright 2019 Google LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#   https://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import os
from gen_util import *

remote_api_functions = [
    { 'name': 'setPos', 'args': 'worldPos' },
    { 'name': 'setYaw', 'args': 'yawRadians' },
    { 'name': 'setPitch', 'args': 'pitchRadians' },
    { 'name': 'setRoll', 'args': 'rollRadians' },
    { 'name': 'setYawPitchRoll', 'args': 'yawRadians, pitchRadians, rollRadians' },
    { 'name': 'turn', 'args': 'radians, axis' },
    { 'name': 'applyQuaternion', 'args': 'quat' },
    { 'name': 'applyQuaternionSelf', 'args': 'quat' },
    { 'name': 'setRot', 'args': 'rot' },
    { 'name': 'resetRot', 'args': '' },
    { 'name': 'lookAt', 'args': 'targetActorOrPoint, yawOnly' },
    { 'name': 'lookDir', 'args': 'direction, yawOnly' },
    { 'name': 'setScaleUniform', 'args': 'scale' },
    { 'name': 'setScale', 'args': 'scale' },
    { 'name': 'attachToParent', 'args': 'newParent' },
    { 'name': 'detachFromParent', 'args': '' },
    { 'name': 'setVar', 'args': 'name, value' },
    { 'name': 'deleteVar', 'args': 'name' },
    { 'name': 'setDisplayName', 'args': 'name' },
    { 'name': 'setCommentText', 'args': 'newText' },
    { 'name': 'setSolid', 'args': 'isSolid' },
    { 'name': 'setKinematic', 'args': 'isKinematic' },
    { 'name': 'enableGravity', 'args': 'enableGravity' },
    { 'name': 'enableKeepUpright', 'args': 'keepUpright' },
    { 'name': 'setBounciness', 'args': 'bounciness' },
    { 'name': 'setMass', 'args': 'mass' },
    { 'name': 'setDrag', 'args': 'drag' },
    { 'name': 'setAngularDrag', 'args': 'drag' },
    { 'name': 'setPhysicsPreset', 'args': 'preset' },
    { 'name': 'addVelocity', 'args': 'velocity' },
    { 'name': 'setCameraActor', 'args': 'cameraActor' },
    { 'name': 'setIsPlayerControllable', 'args': 'value' },
    { 'name': 'setControllingPlayer', 'args': 'playerId' },
    { 'name': 'setBodyPos', 'args': 'pos' },
    { 'name': 'setBodyRot', 'args': 'rot' },
    { 'name': 'setTintColor', 'args': 'color' },
    { 'name': 'setTintHex', 'args': 'colorHex' },
    { 'name': 'show', 'args': 'visible' },
    { 'name': 'hide', 'args': '' },
    { 'name': 'destroySelf', 'args': '' },
]

def REMOTE_API_FUNCTIONS_JAVASCRIPT(srcf, prefix):
    for func in remote_api_functions:
        emit_javascript(srcf, """

/**
 * Politely requests that the given actor call {@link """+func['name']+"""} on itself.
 *
 * <p>This has to be a request because actors can't directly modify other
 * actors, so what this does is send a message to the other actor asking
 * it to call a given function on itself.</p>
 *
 * <p>This is asynchronous and could take a while to execute in a networked
 * game, so don't rely on the results being immediate.</p>
 *
 * <p>See the documentation for {@link """+func['name']+"""} for details
 * about the function itself.</p>
 *
 * @param {ActorRef} actor The actor to send the request to.
"""+gen_jsdoc_for_params(func['args'])+"""
 */
function """+func['name']+"""Please(actor"""+(", " if func['args'] != "" else "")+func['args']+""") {
  assert(exists(actor), '"""+func['name']+"""Please: actor does not exist: ' + actor);
  if (actor === myself()) {
    """+func['name']+"""("""+func['args']+""");
  } else {
    send(actor, 'PoliteRequest', { verb: '"""+func['name']+"""', args: encodeUndefineds(["""+func['args']+"""]) });
  }
}
""", prefix)

def REMOTE_API_HANDLER_CASES_JAVASCRIPT(srcf, prefix):
    for func in remote_api_functions:
        emit_javascript(srcf, "case '"+func['name']+"': "+func['name']+".apply(null, decodeUndefineds(args)); break;", prefix) 

regions_to_generators = {
    'REMOTE_API_FUNCTIONS_JAVASCRIPT': REMOTE_API_FUNCTIONS_JAVASCRIPT,
    'REMOTE_API_HANDLER_CASES_JAVASCRIPT': REMOTE_API_HANDLER_CASES_JAVASCRIPT
}

this_file_dir = os.path.dirname(os.path.realpath(__file__))
paths_to_process = [
    os.path.join(this_file_dir, '..', 'Assets',
                 'Scripts', 'Behaviors', 'JavaScript', 'apiv2', 'remote', 'remote.js.txt'),
    os.path.join(this_file_dir, '..', 'Assets',
                 'StreamingAssets', 'BehaviorLibrary', 'LegacyBehaviors', 'Default Behavior.js.txt'),
]

def gen_jsdoc_for_params(args):
    ret = ""
    for arg in args.split(','):
        ret = ret + " * @param " + arg.strip() + " (see original function)\n"
    return ret

for path in paths_to_process:
    do_code_gen(path, regions_to_generators)

