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

stage_fields = [
    {'name': 'groundSizeX', 'cs_type': 'float', 'need_update': True,
     'comment': 'The X side-length of the ground plane in meters.'},
    {'name': 'groundSizeZ', 'cs_type': 'float', 'need_update': True,
     'comment': 'The Z side-length of the ground plane in meters.'},
    {'name': 'skyType', 'cs_type': 'SkyType', 'is_enum': True, 'need_update': True,
     'comment': 'The sky box type'},
    {'name': 'skyColor', 'cs_type': 'Color', 'need_update': True,
     'comment': 'The sky box tint'},
    {'name': 'groundType', 'cs_type': 'GroundType', 'is_enum': True, 'need_update': True,
     'comment': 'The ground texture type'},
    {'name': 'groundColor', 'cs_type': 'Color', 'need_update': True,
     'comment': 'The ground texture tint'},
    {'name': 'initialCameraMode', 'cs_type': 'CameraMode', 'is_enum': True, 'need_update': False,
     'comment': 'TEMP really should be independent for play vs. build mode'},
    {'name': 'isoCamRotationIndex', 'cs_type': 'int', 'need_update': False,
     'comment': 'TEMP really should be independent for play vs. build mode'},
    {'name': 'sceneLightingMode', 'cs_type': 'SceneLightingMode', 'is_enum': True, 'need_update': True,
     'comment': 'Scene light mode.'},
]


def STAGE_CSHARP(srcf, prefix):
    for field in stage_fields:
        emit_source_of_truth_decl(srcf, prefix, field)

        maybe_normalize = ''
        if field['cs_type'] == 'Quaternion':
            maybe_normalize = '\n  newVoosField = newVoosField.normalized;'

        no_change_condition = get_no_change_condition_templ(field)

        # Idempotent last-writer-wins pattern
        emit_csharp(srcf, """
public VoosCSType GetVoosField()
{
  return voosField;
}

bool SetVoosFieldLocal(VoosCSType newVoosField)
{""" + maybe_normalize + """
  if (""" + no_change_condition + """)
  {
    return false;
  }
  this.voosField = newVoosField;
  """ + ("UpdateVoosField();" if field['need_update'] else "") + """
  return true;
}

[PunRPC]
void SetVoosFieldRPC(VoosCSType newVoosField)
{
  SetVoosFieldLocal(newVoosField);
}

public void SetVoosField(VoosCSType newVoosField)
{
  if (SetVoosFieldLocal(newVoosField))
  {
    photonView.RPC("SetVoosFieldRPC", PhotonTargets.AllViaServer, newVoosField);
  }
}""", prefix, field)


def STAGE_PERSISTED_STRUCT_MEMBERS(srcf, prefix):
    for field in stage_fields:
        if is_enum_field(field):
            emit_csharp(srcf, "public string voosField;", prefix, field)
        else:
            emit_csharp(srcf, "public VoosCSType voosField;", prefix, field)


def STAGE_SAVE_ASSIGNMENTS(srcf, prefix):
    for field in stage_fields:
        if is_enum_field(field):
            emit_csharp(
                srcf, 'voosField = GetVoosField().ToString(),', prefix, field)
        else:
            emit_csharp(
                srcf, 'voosField = GetVoosField(),', prefix, field)


def STAGE_LOAD_PERSISTED(srcf, prefix):
    for field in stage_fields:
        # Important that we use Set**Local, to avoid triggering the RPCs. "Load" is used for new player init, so everyone else has the right values already.
        if is_enum_field(field):
            emit_csharp(
                srcf, 'SetVoosFieldLocal(state.voosField.IsNullOrEmpty() ? DefaultVoosField : Util.ParseEnum<VoosCSType>(state.voosField));', prefix, field)
        else:
            emit_csharp(
                srcf, 'SetVoosFieldLocal(state.voosField);', prefix, field)


def FORCE_UPDATE_ON_START(srcf, prefix):
    for field in stage_fields:
        if not field['need_update']: continue
        emit_csharp(srcf, 'UpdateVoosField();',
                    prefix, field)


regions_to_generators = {
    'STAGE_CSHARP': STAGE_CSHARP,
    'STAGE_PERSISTED_STRUCT_MEMBERS': STAGE_PERSISTED_STRUCT_MEMBERS,
    'STAGE_SAVE_ASSIGNMENTS': STAGE_SAVE_ASSIGNMENTS,
    'STAGE_LOAD_PERSISTED': STAGE_LOAD_PERSISTED,
    'FORCE_UPDATE_ON_START': FORCE_UPDATE_ON_START
}

this_file_dir = os.path.dirname(os.path.realpath(__file__))
paths_to_process = [
    os.path.join(this_file_dir, '..', 'Assets',
                 'Scripts', 'Core', 'GameBuilderStage.cs')
]

for path in paths_to_process:
    do_code_gen(path, regions_to_generators)
