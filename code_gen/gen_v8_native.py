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


accessor_types = ['Boolean', 'Vector3', 'Quaternion', 'Float']


def fill_accessor_template(template, cstype, etter):
    return multiple_replace(template, {
        'CSType': cstype,
        'Tetter': etter,
        'tet': 'get' if etter == 'Getter' else 'set'
    })


def ACTOR_ACCESSOR_DELEGATE_CACHES(srcf, prefix):
    for cstype in accessor_types:
        for etter in ['Getter', 'Setter']:
            emit_csharp(srcf, fill_accessor_template(
                'private static ActorCSTypeTetter lastCSTypeTetterCallback;', cstype, etter), prefix)


def ACTOR_ACCESSOR_DELEGATE_MAYBE_SETS(srcf, prefix):
    for cstype in accessor_types:
        for etter in ['Getter', 'Setter']:
            emit_csharp(srcf, fill_accessor_template("""
if (lastCSTypeTetterCallback != callbacks.tetActorCSType)
{
  SetActorCSTypeTetter(callbacks.tetActorCSType);
  lastCSTypeTetterCallback = callbacks.tetActorCSType;
}
""", cstype, etter), prefix)


regions_to_generators = {
    'ACTOR_ACCESSOR_DELEGATE_CACHES': ACTOR_ACCESSOR_DELEGATE_CACHES,
    'ACTOR_ACCESSOR_DELEGATE_MAYBE_SETS': ACTOR_ACCESSOR_DELEGATE_MAYBE_SETS
}

this_file_dir = os.path.dirname(os.path.realpath(__file__))
paths_to_process = [
    os.path.join(this_file_dir, '..', 'Assets',
                 'Scripts', 'Voos', 'V8InUnity', 'Native.cs')
]

for path in paths_to_process:
    do_code_gen(path, regions_to_generators)
