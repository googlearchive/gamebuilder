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

import re
import os

jsdoc_for_cstype = {
    'string': 'string',
    'bool': 'boolean',
    'Vector3': 'THREE.Vector3',
    'Quaternion': 'THREE.Quaternion',
    'Color': 'THREE.Color',
    'float': 'number',
    'int' : 'number',
}


def multiple_replace(text, adict):
    rx = re.compile('|'.join(map(re.escape, adict)))

    def one_xlat(match):
        return adict[match.group(0)]
    return rx.sub(one_xlat, text)


def to_jsdoc_type(field):
    if 'is_enum' in field and field['is_enum']:
        return 'number'
    else:
        return jsdoc_for_cstype[field['cs_type']]


def get_csharp_type(field):
    return field['cs_type']


def cap_first(s):
    return s[0].capitalize() + s[1:]


def split_lines(code):
    newline = '\n'
    if '\r' in code:
        newline = '\r\n'
    return code.split(newline)


def emit_csharp(srcf, code, line_prefix, field=None):
    if field is not None:
        code = fill_field_template(code, field)

    # Ignore first line if it's blank
    if code[0] == '\n':
        code = code[1:]

    for line in split_lines(code):
        if len(line) == 0:
            # Assume it's an intentional blank line.
            srcf.write('\n')
            continue

        # If only white-space, skip it. This is just convenient for formatting conditional code.
        if len(line.strip()) == 0:
            continue
        srcf.write(line_prefix)
        # I really like how this looks, but VSCode auto-format wrecks it :(
        # srcf.write('/**/   ')
        srcf.write(line)
        if len(line) > 8 and not ('*' in line):
            srcf.write('    // GENERATED')
        srcf.write('\n')


def emit_javascript(srcf, code, line_prefix, field=None):
    emit_csharp(srcf, code, line_prefix, field)


def fill_field_template(template, field):
    return multiple_replace(template, {
        'voosField': field['name'],
        'VoosField': cap_first(field['name']),
        'VoosCSType': field['cs_type'],
        'VoosJSType': to_jsdoc_type(field),
        'VoosComment': field['comment']
    })


def emit_field_merge_js(srcf, prefix, fields, src_var):
    for field in fields:
        if field['cs_type'] == 'Vector3':
            emit_javascript(srcf, 'this.' + field['name'] + '.copy(' + src_var + '.' + field['name'] +
                            ');', prefix)
        elif field['cs_type'] == 'Quaternion':
            emit_javascript(srcf, fill_field_template(
                'copyQuat(' + src_var + '.voosField, this.voosField);', field), prefix)
        else:
            emit_javascript(srcf, 'this.' + field['name'] + ' = ' + src_var + '.' + field['name'] +
                            ';', prefix)


def emit_vector3_getter_js(srcf, prefix, field):
    emit_csharp(srcf, """
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
  existing.copy(this.actor_.voosField);
  return existing;
}""", prefix, field)


def emit_quaternion_getter_js(srcf, prefix, field):
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
  copyQuat(this.actor_.voosField, existing);
  return existing;
}""", prefix, field)


def emit_quaternion_setter_js(srcf, prefix, field):
    # Setter
    emit_csharp(srcf, """
/**
 * Setter for: VoosComment
 * @param {THREE.Quaternion} newVoosField
 */
setVoosField(newVoosField) {
  assertQuaternion(newVoosField, 'setVoosField argument');
  copyQuat(newVoosField, this.actor_.voosField);
}""", prefix, field)


def emit_actor_js_getter(srcf, prefix, field):
    emit_javascript(srcf, """
/**
 * Getter for: VoosComment
 * @returns {VoosJSType}
 */
getVoosField() {
  return this.actor_.voosField;
}""", prefix, field)


assert_by_cstype = {
    'string': 'assertStringOrNull',
    'bool': 'assertBoolean',
    'Vector3': 'assertVector3',
    'Quaternion': 'assertQuaternion',
    'Color': 'assertColor',
    'float': 'assertNumber'
}


def emit_actor_js_setter(srcf, prefix, field):
    assert_func = assert_by_cstype[field['cs_type']]
    # Setter
    emit_csharp(srcf, """
/**
 * Setter for: VoosComment
 * @param {VoosJSType} newVoosField
 */
setVoosField(newVoosField) {
  """ + assert_func + """(newVoosField, 'setVoosField argument');
  this.actor_.voosField = newVoosField;
}""", prefix, field)


def emit_vector3_cs_serialize(srcf, prefix, field):
    emit_csharp(srcf, """
Vector3 tempVoosField = GetVoosField();
writer.Write(tempVoosField.x);
writer.Write(tempVoosField.y);
writer.Write(tempVoosField.z);
""", prefix, field)


def emit_vector3_cs_deserialize(srcf, prefix, field):
    emit_csharp(srcf, """
this.SetVoosField(new Vector3(
    reader.ReadSingle(),
    reader.ReadSingle(),
    reader.ReadSingle()
));
""", prefix, field)


def emit_quat_cs_serialize(srcf, prefix, field):
    emit_csharp(srcf, """
Quaternion tempVoosField = GetVoosField();
writer.Write(tempVoosField.x);
writer.Write(tempVoosField.y);
writer.Write(tempVoosField.z);
writer.Write(tempVoosField.w);
""", prefix, field)


def emit_quat_cs_deserialize(srcf, prefix, field):
    emit_csharp(srcf, """
this.SetVoosField(new Quaternion(
    reader.ReadSingle(),
    reader.ReadSingle(),
    reader.ReadSingle(),
    reader.ReadSingle()
));
""", prefix, field)


def do_code_gen(path, regions_to_generators):
    lines = []

    assert(os.path.exists(path))
    with open(path, 'r') as srcf:
        lines = [line for line in srcf]

    print(len(lines))
    # TODO uhhh write to a temp file, and only overwrite source if we're successful
    with open(path, 'w') as srcf:
        curr_region = None
        for line in lines:
            if curr_region is None:
                srcf.write(line)
                for marker in regions_to_generators:
                    if ('BEGIN_GAME_BUILDER_CODE_GEN ' + marker) in line:
                        prefix = line.split('/')[0]
                        regions_to_generators[marker](srcf, prefix)
                        curr_region = marker
                        break
            else:
                # In a region. Skip until END marker.
                if 'END_GAME_BUILDER_CODE_GEN' in line:
                    srcf.write(line)
                    curr_region = None


def emit_source_of_truth_decl(srcf, prefix, field):
    maybe_assignment = ''
    if 'default_cs_value' in field:
        maybe_assignment = ' = ' + field['default_cs_value']
    elif field['cs_type'] == 'Vector3':
        maybe_assignment = ' = Vector3.zero'
    elif field['cs_type'] == 'Quaternion':
        maybe_assignment = ' = Quaternion.identity'

    emit_csharp(srcf, "private VoosCSType voosField" +
                maybe_assignment + ";\n", prefix, field)


def get_no_change_condition_templ(field):
    no_change_condition = "voosField == newVoosField"
    if field['cs_type'] == 'Quaternion':
        no_change_condition = "voosField.ApproxEquals(newVoosField)"
    elif field['cs_type'] == 'Vector3':
        no_change_condition = "Vector3.Distance(voosField, newVoosField) < 1e-4f"
    elif field['cs_type'] == 'float':
        no_change_condition = "Mathf.Abs(voosField - newVoosField) < 1e-4f"
    return no_change_condition


def emit_csharp_field_getters_setters(srcf, prefix, field, downstream_calls=[]):
    emit_source_of_truth_decl(srcf, prefix, field)

    downstream_code = '\n  '.join([call + "();" for call in downstream_calls])

    update_needs_old_value = 'update_needs_old_value' in field and field['update_needs_old_value']

    if field['need_update']:
        update_code = "UpdateVoosField(oldVoosField);" if update_needs_old_value else "UpdateVoosField();"
    else:
        update_code = ""
    maybe_normalize = ''
    if field['cs_type'] == 'Quaternion':
        maybe_normalize = '\n  newVoosField = newVoosField.normalized;'

    no_change_condition = get_no_change_condition_templ(field)

    change_guard = ''
    if field['need_update'] or len(downstream_calls) > 0:
        change_guard = """
  if (""" + no_change_condition + """)
  {
    return;
  }
"""
    store_old = "var oldVoosField = voosField;" if update_needs_old_value else ""

    emit_csharp(srcf, """
public void SetVoosField(VoosCSType newVoosField)
{
  """ + maybe_normalize + """
  """ + change_guard + """
  """ + store_old + """
  voosField = newVoosField;
  """ + update_code + """
  """ + downstream_code + """
}

public VoosCSType GetVoosField()
{
  return voosField;
}
""", prefix, field)


def is_enum_field(field):
    return 'is_enum' in field and field['is_enum'] == True
