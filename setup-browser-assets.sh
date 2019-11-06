#!/bin/bash
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

mkdir -p BrowserAssets/gamebuilder
cp -afv third_party/codemirror-5.38.0 third_party/jshint-2.9.5 third_party/monaco-editor-0.14.3 BrowserAssets
cp -afv third_party/es5/es5.d.ts.js third_party/js-editor/js-editor.html BrowserAssets/gamebuilder

