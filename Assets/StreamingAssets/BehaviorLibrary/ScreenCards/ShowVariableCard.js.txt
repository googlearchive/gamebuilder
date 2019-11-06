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
  propCardTargetActor('Target', {
    label: 'Whose variable?'
  }),
  propString('VarName', 'MyVar'),
  propNumber('X', 1000),
  propNumber('Y', 100),
  propString('Label', 'My var'),

  propBoolean('AutoSize', true),
  propNumber('Width', 40, { requires: requireFalse('AutoSize') }),
  propNumber('Height', 40, { requires: requireFalse('AutoSize') }),

  propColor('LabelColor', '#ffff00'),
  propNumber('LabelSize', 30),
  propEnum('LabelAlign', 'LEFT', ['LEFT', 'RIGHT', 'CENTER']),

  propColor('ValueColor', '#ffffff'),
  propNumber('ValueSize', 40),
  propEnum('ValueAlign', 'LEFT', ['LEFT', 'RIGHT', 'CENTER']),

  propBoolean('HasBackground', true),
  propColor('BackgroundColor', '#000020', { requires: requireTrue('HasBackground') }),
  propDecimal('Opacity', 0.7),
  propNumber('Padding', 10),
]

const SPACING_BETWEEN_LABEL_AND_VALUE = 5;

export function getCardStatus() {
  return {
    description: `Show variable <color=yellow>${props.VarName}</color> on screen at <color=green>(${props.X}, ${props.Y})`
  }
}

export function onDrawScreen() {
  const boxWidth = calcWidth();
  const boxHeight = calcHeight();
  if (props.HasBackground) {
    uiRect(props.X, props.Y, boxWidth, boxHeight, props.BackgroundColor, { opacity: props.Opacity });
  }
  drawAlignedText(
    props.X + props.Padding,
    props.Y + props.Padding,
    boxWidth - 2 * props.Padding,
    props.Label,
    props.LabelColor,
    props.LabelSize,
    props.LabelAlign);
  drawAlignedText(
    props.X + props.Padding,
    props.Y + props.Padding + SPACING_BETWEEN_LABEL_AND_VALUE + uiGetTextHeight(props.Label, props.LabelSize),
    boxWidth - 2 * props.Padding,
    getVarValue(),
    props.ValueColor,
    props.ValueSize,
    props.ValueAlign);
}

function calcWidth() {
  const val = getVarValue();
  if (props.AutoSize) {
    return 2 * props.Padding + Math.max(uiGetTextWidth(props.Label, props.LabelSize), uiGetTextWidth(val, props.ValueSize));
  } else {
    return props.Width;
  }
}

function calcHeight() {
  const val = getVarValue();
  if (props.AutoSize) {
    return uiGetTextHeight(props.Label, props.LabelSize) + uiGetTextHeight(val, props.ValueSize) + SPACING_BETWEEN_LABEL_AND_VALUE + 2 * props.Padding;
  } else {
    return props.Height;
  }
}

function getVarValue() {
  const actor = getCardTargetActor('Target');
  return exists(actor) ? ("" + getVar(props.VarName, actor)) : '?';
}

function drawAlignedText(x, y, width, text, color, textSize, align) {
  if (align === 'RIGHT') {
    x = x + width - uiGetTextWidth(text, textSize);
  } else if (align === 'CENTER') {
    x += 0.5 * (width - uiGetTextWidth(text, textSize));
  }
  uiText(x, y, text, color, { textSize: textSize });
}
