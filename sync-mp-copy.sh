#!/usr/bin/bash
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

set -e
set -o pipefail

DELETED=$(git ls-files -d)
if [ ! -z "$DELETED" ]
then
  echo "You have deleted some tracked files - we can't sync this yet. Press enter to exit."
  read; exit 1
fi

CURR_COMMIT=$(git rev-parse HEAD)

# Make sure we're in the project folder
stat Assets > /dev/null

# Make sure the copy exists
stat ../proto-copy > /dev/null

pushd ../proto-copy > /dev/null

  echo
  echo -- Resetting proto-copy..
  git reset --hard

  COPY_COMMIT=$(git rev-parse HEAD)
  if [ "$CURR_COMMIT" != "$COPY_COMMIT" ]; then
    echo
    echo -- Switching proto-copy to commit $CURR_COMMIT ..
    git fetch --all
    git checkout $CURR_COMMIT
  fi

  UNTRACKED_IN_COPY=$(git ls-files --others --exclude-standard)
  if [ ! -z "$UNTRACKED_IN_COPY" ]
  then
    echo
    echo -- Deleting untracked files in proto-copy..
    git ls-files --others --exclude-standard | xargs -I % rm -v %
  fi

popd > /dev/null

echo
echo -- Copying modified and untracked files..
git ls-files -m --others --exclude-standard | xargs -I % cp -v % ../proto-copy/%

echo
echo -- All done! Foreground Unity to re-compile.

echo
echo '-- Performing sanity check (diff -rqw on Assets/Scripts), but you can proceed'
diff -rqw Assets/Scripts ../proto-copy/Assets/Scripts
