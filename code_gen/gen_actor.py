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

# TODO: we can probably get rid of need_update and put that info in the 'downstreams' list.
# TODO: get rid of need_persisted_var? Only false for tags and read-only.

##### IMPORTANT!!! If you modify this, please make sure you bump up the version number in VoosActor.PersistedState.

actor_fields = [
    {'name': 'displayName', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': "The human-readable display name for the actor. Useful for edit mode inspecting and debugging."},
    {'name': 'description', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': "The user-customizable descripion of the actor."},
    {'name': 'tint', 'cs_type': 'Color', 'need_persisted_var': True, 
        'need_component_var': False, 'need_update': True, 'comment': "tint"},
    {'name': 'transformParent', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': True, 'need_update': True, 'update_needs_old_value': True, 'comment': "The name of the actor that is the transform-parent of this actor. Used for attach yourself to another, like when you get picked up."},
    {'name': 'position', 'cs_type': 'Vector3', 'need_persisted_var': True,
        'need_component_var': False, 'need_update': False, 'comment': "The world-space position of the actor origin, which by default is the bottom-center of the object, ie. its position on the floor. However, this can be changed with setRenderableOffset."},
    {'name': 'localPosition', 'cs_type': 'Vector3', 'need_persisted_var': False,
        'need_component_var': False, 'need_update': False, 'comment': "The local position (position relative to parent's coordinate system)."},
    {'name': 'rotation', 'cs_type': 'Quaternion', 'need_persisted_var': True,
        'need_component_var': False, 'need_update': False, 'comment': "The world-space rotation of the actor. This should be used to compute directions, like the forward facing direction of the actor, and if it does not correspond to the renderable model's forward (like where the head is facing for a lion model), you should fix it using the Rotate Tool or setRenderableRotation."},
    {'name': 'localRotation', 'cs_type': 'Quaternion', 'need_persisted_var': False,
        'need_component_var': False, 'need_update': False, 'comment': "The local rotation (rotation relative to parent's coordinate system)."},
    {'name': 'localScale', 'cs_type': 'Vector3', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': False, 'need_update': False, 'comment': "The local scale of the actor."},
    {'name': 'renderableOffset', 'cs_type': 'Vector3', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': True, 'need_update': True, 'comment': "The local position/offset of the rendered model (and collider) relative to the actor's origin."},
    {'name': 'renderableRotation', 'cs_type': 'Quaternion', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': True, 'need_update': True, 'comment': "The local rotation of the rendered model (and collider) relative to the actor's rotation."},
    {'name': 'commentText', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': True, 'need_update': False, 'comment': "The text shown on comment signs created using the Comment Tool. Does not matter for non-sign actors."},
    {'name': 'spawnPosition', 'cs_type': 'Vector3', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': True, 'need_update': False, 'comment': "The position the actor will be reset to upon ResetGame (F6). When you move it using the Move Tool, it also affects this, but scripted motion or physics does not."},
    {'name': 'spawnRotation', 'cs_type': 'Quaternion', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': True, 'need_update': False, 'comment': "Like spawnPosition, but for rotation."},
    {'name': 'preferOffstage', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': True, 'need_update': False, 'default_cs_value': 'false', 'comment': "If true, this object is in the virtual off-stage area, not on the actual scene."},
    {'name': 'isSolid', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
        'need_component_var': True, 'need_update': False, 'default_cs_value': 'true', 'comment': "If true, other objects will collide and not go thruogh it. If false, it will still be visible, but other objects can go through it like a ghost."},
    {'name': 'enablePhysics', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': 'Enable physics!'},
    {'name': 'enableGravity', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': 'Enable gravity! Only works if enablePhysics is also true.'},
    {'name': 'bounciness', 'cs_type': 'float', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'drag', 'cs_type': 'float', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'angularDrag', 'cs_type': 'float', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'mass', 'cs_type': 'float', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'freezeRotations', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'freezeX', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'freezeY', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'freezeZ', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'enableAiming', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': 'GET RID OF THIS!'},
    {'name': 'hideInPlayMode', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': "This actor's model will not be rendered in play mode (but still visible in edit). Good for hiding things, like picked-up items."},
    {'name': 'keepUpright', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': 'Keeps the object standing upright, but still responding to physics otherwise.'},
    {'name': 'useDesiredVelocity', 'cs_type': 'bool', 'need_persisted_var': False,
     'need_component_var': True, 'need_update': False, 'comment': 'Makes the object obey the "desiredVelocity" value, like a motor.'},
    {'name': 'ignoreVerticalDesiredVelocity', 'cs_type': 'bool', 'need_persisted_var': False,
     'need_component_var': True, 'need_update': False, 'comment': 'Ignore the vertical Y component of desiredVelocity. Use this if your character only moves on the ground, and does not fly, for example.'},
    {'name': 'desiredVelocity', 'cs_type': 'Vector3', 'need_persisted_var': False,
     'need_component_var': True, 'need_update': False, 'comment': 'The velocity the actor should move with. Make sure "useDesiredVelocity" is true for this to work.'},
    {'name': 'isPlayerControllable', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': True, 'comment': 'Internal - do not use for now.'},
    {'name': 'debugString', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': "Internal - do not use for now"},
    {'name': 'worldRenderBoundsSize', 'cs_type': 'Vector3', 'read_only': True, 'need_persisted_var': False,
     'need_component_var': False, 'comment': "Axis-aligned current world bounds"},
    {'name': 'worldRenderBoundsCenter', 'cs_type': 'Vector3', 'read_only': True, 'need_persisted_var': False,
     'need_component_var': False, 'comment': "Axis-aligned current world bounds"},
    {'name': 'cloneParent', 'cs_type': 'string', 'read_only': True, 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ""},
    {'name': 'joinedTags', 'cs_type': 'string', 'need_persisted_var': False, 'networking' : 'reliable',
     'need_component_var': False, 'need_update': False, 'comment': "Internal - do not use for now"},
    {'name': 'velocity', 'cs_type': 'Vector3', 'need_persisted_var': True,
     'need_component_var': False, 'need_update': False, 'comment': 'The direct rigidbody velocity parameter. Not valid if physics is not enabled.'},
    {'name': 'angularVelocity', 'cs_type': 'Vector3', 'need_persisted_var': True,
     'need_component_var': False, 'need_update': False, 'comment': 'The direct rigidbody angular velocity parameter. Not valid if physics is not enabled.'},
    {'name': 'cameraActor', 'cs_type': 'string', 'need_persisted_var': True,
     'need_component_var': True, 'need_update': False, 'comment': "Internal - do not use for now"},
    {'name': 'spawnTransformParent', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable',
      'need_component_var': True, 'need_update': False, 'comment': "The name of the actor that is the transform-parent of this actor upon reset."},
    {'name': 'wasClonedByScript', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
      'need_component_var': True, 'need_update': False, 'default_cs_value': 'false', 'comment': "If true, this object was created/cloned by script. So it will be treated differently by various systems, such as getting auto-destroyed upon ResetGame."},
    {'name': 'loopingAnimation', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': "The clip name of the currently looping animation"},
    {'name': 'controllingVirtualPlayerId', 'cs_type': 'string', 'need_persisted_var': False, 'networking': 'reliable',
     'need_component_var': True, 'need_update': True, 'comment': "Internal - do not use for now"},
    {'name': 'cameraSettingsJson', 'cs_type': 'string', 'need_persisted_var': True,
     'need_component_var': True, 'need_update': True, 'comment': "Internal - do not use for now"},
    {'name': 'lightSettingsJson', 'cs_type': 'string', 'need_persisted_var': True, 'networking': 'reliable',
     'need_component_var': True, 'need_update': True, 'comment': "Internal - do not use for now"},
    {'name': 'pfxId', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable', 'need_component_var': True, 'need_update': True, 'comment': ""},
    {'name': 'sfxId', 'cs_type': 'string', 'need_persisted_var': True, 'networking' : 'reliable', 'need_component_var': True, 'need_update': True, 'comment': ""},
    {'name': 'useConcaveCollider', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': ''},
    {'name': 'isGrounded', 'cs_type': 'bool', 'read_only': True, 'need_persisted_var': False,
     'need_component_var': False, 'comment': ""},
    {'name': 'speculativeColDet', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': 'Enable continuous, speculative collision detection. This is expensive, but will help with fast objects going through walls. Note this only works if the object has primitive colliders!'},
    {'name': 'useStickyDesiredVelocity', 'cs_type': 'bool', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': 'If true, then stickyDesiredVelocity should be enforced.'},
    {'name': 'stickyDesiredVelocity', 'cs_type': 'Vector3', 'need_persisted_var': True, 'networking' : 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': 'Only relevant is useStickDesiredVelocity is true. Unlike normal desiredVelocity, this is persisted and networked. For things like kinematic projectiles (laser bolts) that just fly in one direction, this is all you need and it allows us to predict on remote clients.'},
     {'name': 'stickyForce', 'cs_type': 'Vector3', 'need_persisted_var': True, 'networking': 'reliable',
     'need_component_var': True, 'need_update': False, 'comment': 'A constant force applied to the object''s center of mass. Only valid if physics is enabled.'}
]

runtime_actor_fields = [
    {'name': 'isSprinting', 'cs_type': 'bool',
        'comment': '(Only valid if isPlayerControllable is true) True if the player is holding the sprint key (usually shift)'},
    {'name': 'worldSpaceThrottle', 'cs_type': 'Vector3',
        'comment': '(Only valid if isPlayerControllable is true) The player input throttle transformed into world space, according to the camera.'},
    {'name': 'inputThrottle', 'cs_type': 'Vector3',
        'comment': '(Only valid if isPlayerControllable is true) The raw player input throttle, so X is horizontal (A/D) and Y is vertical (W/S)'},
    {'name': 'aimDirection', 'cs_type': 'Vector3',
        'comment': '(Only valid if isPlayerControllable is true) For aiming ray casts, use this direction.'},
    {'name': 'aimOrigin', 'cs_type': 'Vector3',
        'comment': '(Only valid if isPlayerControllable is true) For aiming ray casts, use this origin.'},
    {'name': 'lastAimHitPoint', 'cs_type': 'Vector3', 'comment': 'TODO'},
    {'name': 'aimingAtName', 'cs_type': 'string', 'comment': 'TODO'},
    {'name': 'lookAxes', 'cs_type': 'Vector3',
        'comment': '(Only valid if isPlayerControllable is true) Look axes.'},
]

downstreams = [
    {'function' : 'UpdateGameObjectName', 'deps':['displayName']},
    {'function' : 'UpdateCommentText', 'deps':['commentText']},
    {'function' : 'UpdateRenderableHiddenState', 'deps':['hideInPlayMode']},
    {'function' : 'UpdateRigidbodyComponent', 'deps':['enablePhysics', 'enableGravity', 'keepUpright', 'mass', 'drag', 'angularDrag', 'freezeRotations', 'freezeX', 'freezeY', 'freezeZ', 'speculativeColDet']},
    {'function' : 'UpdatePhysicsMaterial', 'deps':['bounciness']},
    {'function' : 'UpdateTriggerGhost', 'deps':['enablePhysics', 'isSolid']},
    {'function' : 'UpdateCollisionStayTracker', 'deps':['isPlayerControllable']},
    {'function' : 'OnEffectivelyOffstageChanged', 'deps':['preferOffstage', 'transformParent']},
    {'function' : 'UpdateAnimation', 'deps':['loopingAnimation']},
    {'function' : 'MaybeCorrectRotation', 'deps':['rotation', 'keepUpright']},
    {'function' : 'UpdateColliders', 'deps':['useConcaveCollider', 'enablePhysics', 'isSolid']},
]

# BELOW IS NO LONGER CONFIG DATA!

for field in actor_fields:
    if 'networking' not in field:
        print 'NOTE: no networking spec set for ' + field['name']

bool_actor_fields = [
    field for field in actor_fields if field['cs_type'] == 'bool']
float_actor_fields = [
    field for field in actor_fields if field['cs_type'] == 'float']
color_actor_fields = [
    field for field in actor_fields if field['cs_type'] == 'Color']
vector3_actor_fields = [
    field for field in actor_fields if field['cs_type'] == 'Vector3']
quat_actor_fields = [
    field for field in actor_fields if field['cs_type'] == 'Quaternion']
string_actor_fields = [
    field for field in actor_fields if field['cs_type'] == 'string']

downstreams_by_field = {f['name'] : [] for f in actor_fields}
for entry in downstreams:
    for dep in entry['deps']:
        if dep not in downstreams_by_field:
            raise Exception('Downstream info for function ' + entry['function'] + ' has invalid dependency field: ' + dep)
        downstreams_by_field[dep].append(entry['function'])

def is_read_only(field):
    return 'read_only' in field and field['read_only'] == True


def writables_only(fields):
    for field in fields:
        if not is_read_only(field):
            yield field


def ACTOR_PERSISTED_FIELDS_CSHARP_DECLS(srcf, prefix):
    for field in actor_fields:
        if not field['need_persisted_var']:
            continue
        emit_csharp(srcf, 'public ' + get_csharp_type(field) +
                    ' ' + field['name'] + ';', prefix)


def ACTOR_FIELDS_CONSTRUCTOR_JAVASCRIPT(srcf, prefix):
    for field in runtime_actor_fields:
        if field['cs_type'] == 'Vector3':
            emit_javascript(srcf, 'this.' +
                            field['name'] + ' = new THREE.Vector3();', prefix)
        elif field['cs_type'] == 'Quaternion':
            # Pretty important we don't use THREE.Quaternion here, since it
            # stores its components as _x, _y, etc. And actually we just
            # shouldn't rely on THREE.Vector3 for this either. Bleh.
            emit_javascript(
                srcf, 'this.voosField = { x: 0, y: 0, z: 0, w: 1 };', prefix, field)


def ACTOR_COMPONENT_CSHARP(srcf, prefix):
    for field in actor_fields:
        if field['need_component_var']:
            emit_csharp_field_getters_setters(srcf, prefix, field,
            downstreams_by_field[field['name']])


def ACTOR_PERSISTED_FIELDS_SERIALIZE(srcf, prefix):
    for field in actor_fields:
        if not field['need_persisted_var']:
            continue
        emit_csharp(srcf, "rv.{0} = actor.Get{1}();".format(
            field['name'], cap_first(field['name']), field['cs_type']), prefix)


def ACTOR_PERSISTED_FIELDS_DESERIALIZE(srcf, prefix):
    for field in actor_fields:
        if not field['need_persisted_var']:
            continue
        emit_csharp(srcf, "Set{1}(serialized.{0});".format(
            field['name'], cap_first(field['name']), field['cs_type']), prefix)


def OTHER_ACTOR_CLASS_JAVASCRIPT(srcf, prefix):
    # "Runtime fields" are async and serialized still, because they're only done
    # for player actors, so there are few of them. Might as well convert to sync
    # later as well though.
    for field in runtime_actor_fields:
        if field['cs_type'] == 'Vector3':
            emit_vector3_getter_js(srcf, prefix, field)
        elif field['cs_type'] == 'Quaternion':
            emit_quaternion_getter_js(srcf, prefix, field)
        else:
            emit_actor_js_getter(srcf, prefix, field)

    for id, field in enumerate(bool_actor_fields):
        emit_javascript(srcf, """
/**
 * Getter for: VoosComment
 * @returns {VoosJSType}
 */
getVoosField() {
  return getActorBoolean(this.actor_.tempId_, """+str(id)+""");
}""", prefix, field)

    for id, field in enumerate(float_actor_fields):
        emit_javascript(srcf, """
/**
 * Getter for: VoosComment
 * @returns {VoosJSType}
 */
getVoosField() {
  return getActorFloat(this.actor_.tempId_, """+str(id)+""");
}""", prefix, field)

    for id, field in enumerate(vector3_actor_fields):
        emit_javascript(srcf, """
/**
 * Getter for: VoosComment
 * @param {THREE.Vector3=} existing
 * @returns {THREE.Vector3}
 */
getVoosField(existing = null) {
  if (!existing) {
    existing = new THREE.Vector3();
  }
  assertVector3(existing, 'getVoosField argument');
  getActorVector3(this.actor_.tempId_, """+str(id)+""", existing);
  return existing;
}""", prefix, field)

    for id, field in enumerate(quat_actor_fields):
        emit_javascript(srcf, """
/**
 * Getter for: VoosComment
 * @param {THREE.Quaternion=} existing
 * @returns {THREE.Quaternion}
 */
getVoosField(existing = null) {
  if (!existing) {
    existing = new THREE.Quaternion();
  }
  assertQuaternion(existing, 'getVoosField argument');
  getActorQuaternion(this.actor_.tempId_, """+str(id)+""", existing);
  return existing;
}""", prefix, field)

    for id, field in enumerate(string_actor_fields):
        emit_javascript(srcf, """
/**
 * Getter for: VoosComment
 * @returns {VoosJSType}
 */
getVoosField() {
  return getActorString(this.actor_.tempId_, """+str(id)+""");
}""", prefix, field)

    for id, field in enumerate(color_actor_fields):
        emit_javascript(srcf, """
/**
 * Getter for: VoosComment
 * @param {THREE.Color=} existing
 * @returns {THREE.Color}
 */
getVoosField(existing = null) {
  if (!existing) {
    existing = new THREE.Color();
  }
  assertColor(existing, 'getVoosField argument');
  const o = getActorColor(this.actor_.tempId_, """+str(id)+""");
  existing.r = o.r;
  existing.g = o.g;
  existing.b = o.b;
  existing.a = o.a;
  return existing;
}""", prefix, field)

def HANDLING_ACTOR_CLASS_JAVASCRIPT(srcf, prefix):
    for id, field in enumerate(writables_only(bool_actor_fields)):
        assert_func = assert_by_cstype[field['cs_type']]
        # Setter
        emit_javascript(srcf, """
/**
 * Setter for: VoosComment
 * @param {VoosJSType} newVoosField
 */
setVoosField(newVoosField) {
  """ + assert_func + """(newVoosField, 'setVoosField argument');
  setActorBoolean(this.actor_.tempId_, """+str(id)+""", newVoosField);
}""", prefix, field)

    for id, field in enumerate(writables_only(float_actor_fields)):
        assert_func = assert_by_cstype[field['cs_type']]
        # Setter
        emit_javascript(srcf, """
/**
 * Setter for: VoosComment
 * @param {VoosJSType} newVoosField
 */
setVoosField(newVoosField) {
  """ + assert_func + """(newVoosField, 'setVoosField argument');
  setActorFloat(this.actor_.tempId_, """+str(id)+""", newVoosField);
}""", prefix, field)

    for id, field in enumerate(writables_only(vector3_actor_fields)):
        emit_csharp(srcf, """
/**
 * Setter for: VoosComment
 * @param {THREE.Vector3} newVoosField
 */
setVoosField(newVoosField) {
  assertVector3(newVoosField, 'setVoosField argument');
  const v = newVoosField;
  setActorVector3(this.actor_.tempId_, """+str(id)+""", v.x, v.y, v.z);
}""", prefix, field)

    for id, field in enumerate(writables_only(quat_actor_fields)):
        emit_csharp(srcf, """
/**
 * Setter for: VoosComment
 * @param {THREE.Quaternion} newVoosField
 */
setVoosField(newVoosField) {
  assertQuaternion(newVoosField, 'setVoosField argument');
  const q = newVoosField;
  setActorQuaternion(this.actor_.tempId_, """+str(id)+""", q.x, q.y, q.z, q.w);
}""", prefix, field)

    for id, field in enumerate(writables_only(string_actor_fields)):
        assert_func = assert_by_cstype[field['cs_type']]
        # Setter
        emit_javascript(srcf, """
/**
 * Setter for: VoosComment
 * @param {VoosJSType} newVoosField
 */
setVoosField(newVoosField) {
  """ + assert_func + """(newVoosField, 'setVoosField argument');
  setActorString(this.actor_.tempId_, """+str(id)+""", newVoosField);
}""", prefix, field)

    for id, field in enumerate(writables_only(color_actor_fields)):
        assert_func = assert_by_cstype[field['cs_type']]
        emit_javascript(srcf, """
/**
 * Setter for: VoosComment
 * @param {VoosJSType} newVoosField
 */
setVoosField(newVoosField) {
  """ + assert_func + """(newVoosField, 'setVoosField argument');
  setActorColor(this.actor_.tempId_, """+str(id)+""", newVoosField);
}""", prefix, field)


def RUNTIME_STATE_CSHARP_DECLS(srcf, prefix):
    for field in runtime_actor_fields:
        emit_csharp(srcf, 'public VoosCSType voosField;', prefix, field)


def ACTOR_RUNTIME_FIELDS_MERGE_JSON_JAVASCRIPT(srcf, prefix):
    emit_field_merge_js(srcf, prefix, runtime_actor_fields, 'runtimeSrc')


def emit_getter_cases(srcf, prefix, fields):
    for id, field in enumerate(fields):
        emit_csharp(
            srcf, 'case '+str(id)+': return this.GetVoosField();', prefix, field)


def emit_setter_cases(srcf, prefix, fields):
    for id, field in enumerate(writables_only(fields)):
        emit_csharp(
            srcf, 'case '+str(id)+': this.SetVoosField(newValue); return;', prefix, field)


def CS_ACTOR_GET_BOOLEAN_FIELD_SWITCH(srcf, prefix):
    emit_getter_cases(srcf, prefix, bool_actor_fields)


def CS_ACTOR_SET_BOOLEAN_FIELD_SWITCH(srcf, prefix):
    emit_setter_cases(srcf, prefix, bool_actor_fields)


def CS_ACTOR_GET_FLOAT_FIELD_SWITCH(srcf, prefix):
    emit_getter_cases(srcf, prefix, float_actor_fields)


def CS_ACTOR_SET_FLOAT_FIELD_SWITCH(srcf, prefix):
    emit_setter_cases(srcf, prefix, float_actor_fields)


def CS_ACTOR_GET_VECTOR3_FIELD_SWITCH(srcf, prefix):
    emit_getter_cases(srcf, prefix, vector3_actor_fields)


def CS_ACTOR_SET_VECTOR3_FIELD_SWITCH(srcf, prefix):
    emit_setter_cases(srcf, prefix, vector3_actor_fields)


def CS_ACTOR_GET_STRING_FIELD_SWITCH(srcf, prefix):
    emit_getter_cases(srcf, prefix, string_actor_fields)


def CS_ACTOR_SET_STRING_FIELD_SWITCH(srcf, prefix):
    emit_setter_cases(srcf, prefix, string_actor_fields)


def CS_ACTOR_GET_QUATERNION_FIELD_SWITCH(srcf, prefix):
    emit_getter_cases(srcf, prefix, quat_actor_fields)


def CS_ACTOR_SET_QUATERNION_FIELD_SWITCH(srcf, prefix):
    emit_setter_cases(srcf, prefix, quat_actor_fields)


def CS_ACTOR_GET_COLOR_FIELD_SWITCH(srcf, prefix):
    emit_getter_cases(srcf, prefix, color_actor_fields)


def CS_ACTOR_SET_COLOR_FIELD_SWITCH(srcf, prefix):
    emit_setter_cases(srcf, prefix, color_actor_fields)


def LEGACY_ACTOR_ACCESSORS(srcf, prefix):
    for id, field in enumerate(writables_only(bool_actor_fields)):
        assert_func = assert_by_cstype[field['cs_type']]
        # Setter
        emit_javascript(srcf, """
set voosField(newVoosField) {
  """ + assert_func + """(newVoosField, 'setVoosField argument');
  setActorBoolean(this.tempId_, """+str(id)+""", newVoosField);
}""", prefix, field)

    for id, field in enumerate(writables_only(float_actor_fields)):
        assert_func = assert_by_cstype[field['cs_type']]
        # Setter
        emit_javascript(srcf, """
set voosField(newVoosField) {
  """ + assert_func + """(newVoosField, 'setVoosField argument');
  setActorFloat(this.tempId_, """+str(id)+""", newVoosField);
}""", prefix, field)

    for id, field in enumerate(writables_only(vector3_actor_fields)):
        emit_csharp(srcf, """
set voosField(newVoosField) {
  assertVector3(newVoosField, 'setVoosField argument');
  const v = newVoosField;
  setActorVector3(this.tempId_, """+str(id)+""", v.x, v.y, v.z);
}""", prefix, field)

    for id, field in enumerate(writables_only(quat_actor_fields)):
        emit_csharp(srcf, """
set voosField(newVoosField) {
  assertQuaternion(newVoosField, 'setVoosField argument');
  const q = newVoosField;
  setActorQuaternion(this.tempId_, """+str(id)+""", q.x, q.y, q.z, q.w);
}""", prefix, field)

    for id, field in enumerate(writables_only(string_actor_fields)):
        assert_func = assert_by_cstype[field['cs_type']]
        # Setter
        emit_javascript(srcf, """
set voosField(newVoosField) {
  """ + assert_func + """(newVoosField, 'setVoosField argument');
  setActorString(this.tempId_, """+str(id)+""", newVoosField);
}""", prefix, field)

    for id, field in enumerate(bool_actor_fields):
        emit_javascript(srcf, """
get voosField() {
  return getActorBoolean(this.tempId_, """+str(id)+""");
}""", prefix, field)

    for id, field in enumerate(float_actor_fields):
        emit_javascript(srcf, """
get voosField() {
  return getActorFloat(this.tempId_, """+str(id)+""");
}""", prefix, field)

    for id, field in enumerate(vector3_actor_fields):
        emit_javascript(srcf, """
get voosField() {
  const existing = new THREE.Vector3();
  getActorVector3(this.tempId_, """+str(id)+""", existing);
  // Legacy hack..
  existing.copy = (v) => {
    existing.x = v.x;
    existing.y = v.y;
    existing.z = v.z;
    this.voosField = existing;
  };
  existing.set = (x, y, z) => {
    existing.x = x;
    existing.y = y;
    existing.z = z;
    this.voosField = existing;
  };
  return existing;
}""", prefix, field)

    for id, field in enumerate(quat_actor_fields):
        emit_javascript(srcf, """
get voosField() {
  const existing = new THREE.Quaternion();
  getActorQuaternion(this.tempId_, """+str(id)+""", existing);
  return existing;
}""", prefix, field)

    for id, field in enumerate(string_actor_fields):
        emit_javascript(srcf, """
get voosField() {
  return getActorString(this.tempId_, """+str(id)+""");
}""", prefix, field)

reliable_stream_fields = [f for f in actor_fields if 'networking' in f and f['networking'] == 'reliable']
def ACTOR_RELIABLE_STREAM_WRITE(srcf, prefix):
    for field in reliable_stream_fields:
        if field['cs_type'] == 'string':
            emit_csharp(srcf, "stream.SendNext(Util.EmptyIfNull(actor.GetVoosField()));", prefix, field)
        else:
            emit_csharp(srcf, "stream.SendNext(actor.GetVoosField());", prefix, field)

def ACTOR_RELIABLE_STREAM_READ(srcf, prefix):
    for field in reliable_stream_fields:
        emit_csharp(srcf, "actor.SetVoosField((VoosCSType)stream.ReceiveNext());", prefix, field)

cstype2writesuffix = {
    'string' : 'Utf16',
    'bool' : 'VoosBoolean',
    'float' : '',
    'Vector3': 'VoosVector3',
    'Color': 'Color',
    'Quaternion': ''
}

cstype2readsuffix = {
    'string' : 'Utf16',
    'bool' : 'VoosBoolean',
    'float' : 'Single',
    'Vector3': 'VoosVector3',
    'Color': 'Color',
    'Quaternion': 'Quaternion'
}

def ACTOR_PERSISTED_FIELDS_BINARY_SERIALIZE(srcf, prefix):
    for field in actor_fields:
        if not field['need_persisted_var']: continue
        suf = cstype2writesuffix[field['cs_type']]
        emit_csharp(srcf, 'writer.Write' + suf + '(voosField);', prefix, field)


def ACTOR_PERSISTED_FIELDS_BINARY_DESERIALIZE(srcf, prefix):
    for field in actor_fields:
        if not field['need_persisted_var']: continue
        suf = cstype2readsuffix[field['cs_type']]
        emit_csharp(srcf, 'this.voosField = reader.Read' + suf + '();', prefix, field)

regions_to_generators = {
    'ACTOR_PERSISTED_FIELDS_CSHARP_DECLS': ACTOR_PERSISTED_FIELDS_CSHARP_DECLS,
    'ACTOR_FIELDS_CONSTRUCTOR_JAVASCRIPT': ACTOR_FIELDS_CONSTRUCTOR_JAVASCRIPT,
    'ACTOR_COMPONENT_CSHARP': ACTOR_COMPONENT_CSHARP,
    'ACTOR_PERSISTED_FIELDS_SERIALIZE': ACTOR_PERSISTED_FIELDS_SERIALIZE,
    'ACTOR_PERSISTED_FIELDS_DESERIALIZE': ACTOR_PERSISTED_FIELDS_DESERIALIZE,
    'OTHER_ACTOR_CLASS_JAVASCRIPT': OTHER_ACTOR_CLASS_JAVASCRIPT,
    'HANDLING_ACTOR_CLASS_JAVASCRIPT': HANDLING_ACTOR_CLASS_JAVASCRIPT,
    'RUNTIME_STATE_CSHARP_DECLS': RUNTIME_STATE_CSHARP_DECLS,
    'ACTOR_RUNTIME_FIELDS_MERGE_JSON_JAVASCRIPT': ACTOR_RUNTIME_FIELDS_MERGE_JSON_JAVASCRIPT,
    'CS_ACTOR_GET_BOOLEAN_FIELD_SWITCH': CS_ACTOR_GET_BOOLEAN_FIELD_SWITCH,
    'CS_ACTOR_SET_BOOLEAN_FIELD_SWITCH': CS_ACTOR_SET_BOOLEAN_FIELD_SWITCH,
    'CS_ACTOR_GET_FLOAT_FIELD_SWITCH': CS_ACTOR_GET_FLOAT_FIELD_SWITCH,
    'CS_ACTOR_SET_FLOAT_FIELD_SWITCH': CS_ACTOR_SET_FLOAT_FIELD_SWITCH,
    'CS_ACTOR_GET_VECTOR3_FIELD_SWITCH': CS_ACTOR_GET_VECTOR3_FIELD_SWITCH,
    'CS_ACTOR_SET_VECTOR3_FIELD_SWITCH': CS_ACTOR_SET_VECTOR3_FIELD_SWITCH,
    'CS_ACTOR_GET_QUATERNION_FIELD_SWITCH': CS_ACTOR_GET_QUATERNION_FIELD_SWITCH,
    'CS_ACTOR_SET_QUATERNION_FIELD_SWITCH': CS_ACTOR_SET_QUATERNION_FIELD_SWITCH,
    'CS_ACTOR_SET_STRING_FIELD_SWITCH': CS_ACTOR_SET_STRING_FIELD_SWITCH,
    'CS_ACTOR_GET_STRING_FIELD_SWITCH': CS_ACTOR_GET_STRING_FIELD_SWITCH,
    'CS_ACTOR_SET_COLOR_FIELD_SWITCH': CS_ACTOR_SET_COLOR_FIELD_SWITCH,
    'CS_ACTOR_GET_COLOR_FIELD_SWITCH': CS_ACTOR_GET_COLOR_FIELD_SWITCH,
    'LEGACY_ACTOR_ACCESSORS': LEGACY_ACTOR_ACCESSORS,
    'ACTOR_RELIABLE_STREAM_WRITE': ACTOR_RELIABLE_STREAM_WRITE,
    'ACTOR_RELIABLE_STREAM_READ': ACTOR_RELIABLE_STREAM_READ,
    'ACTOR_PERSISTED_FIELDS_BINARY_SERIALIZE': ACTOR_PERSISTED_FIELDS_BINARY_SERIALIZE,
    'ACTOR_PERSISTED_FIELDS_BINARY_DESERIALIZE': ACTOR_PERSISTED_FIELDS_BINARY_DESERIALIZE
}

this_file_dir = os.path.dirname(os.path.realpath(__file__))
paths_to_process = [
    os.path.join(this_file_dir, '..', 'Assets',
                 'Scripts', 'Voos', 'VoosActor.cs'),
    os.path.join(this_file_dir, '..', 'Assets',
                 'Scripts', 'Voos', 'VoosActorAccessors.cs'),
    os.path.join(this_file_dir, '..', 'Assets',
                'Scripts', 'Voos', 'ActorNetworking.cs'),
    os.path.join(this_file_dir, '..', 'Assets',
                 'Scripts', 'Behaviors', 'JavaScript', 'ModuleBehaviorsActor.js.txt'),
    os.path.join(this_file_dir, '..', 'Assets',
                 'Scripts', 'Behaviors', 'JavaScript', 'HandlingActor.js.txt'),
]

for path in paths_to_process:
    do_code_gen(path, regions_to_generators)
