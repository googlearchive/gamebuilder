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

set -euxo pipefail

OUTDIR="$1"
stat $OUTDIR

echo Checking NPM installed
npm --version

echo Checking JSDoc is installed..
jsdoc --version

echo Installing/updating TUI template
npm i -D tui-jsdoc-template
TEMPLATE_PATH=$(npm root)/tui-jsdoc-template

JS_SRC=Assets/Scripts/Behaviors/JavaScript/moduleBehaviors.js.txt
cp $JS_SRC temp.js
jsdoc -c code_gen/jsdoc-config.json temp.js -d $OUTDIR -t $TEMPLATE_PATH
rm temp.js

echo All good!
