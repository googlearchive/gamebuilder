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

echo Make sure Unity is closed!
echo --

pushd Assets
rm -rf PhotonChatApi
popd

pushd Assets/Photon\ Unity\ Networking
rm -rf Demos
NEWTONSOFT_COPY=Editor/PhotonNetwork/Newtonsoft.Json.dll
if [ -f "$NEWTONSOFT_COPY" ]; then rm $NEWTONSOFT_COPY; fi;
popd

if [[ "$OSTYPE" == "darwin"* ]]; then
  # Remove CRs (PUN comes in with DOS line endings - breaks our patches)
  perl -i -pe 's/\r//' Assets/Photon\ Unity\ Networking/Editor/PhotonNetwork/Views/PhotonAnimatorViewEditor.cs
  perl -i -pe 's/\r//' Assets/Photon\ Unity\ Networking/Plugins/PhotonNetwork/CustomTypes.cs
  perl -i -pe 's/\r//' Assets/Photon\ Unity\ Networking/Plugins/PhotonNetwork/PhotonNetwork.cs
  perl -i -pe 's/\r//' Assets/Photon\ Unity\ Networking/Plugins/PhotonNetwork/Views/PhotonAnimatorView.cs
  perl -i -pe 's/\r//' Assets/Photon\ Unity\ Networking/Plugins/PhotonNetwork/Views/PhotonTransformView.cs
  patch -p0 < photon.osx.patch
  patch -p0 < photon_transform_view.osx.patch
else
  patch -p0 < photon.patch
  patch -p0 < photon_transform_view.patch
fi

# Force Unity to reimport. Otherwise, Unity tends to cache incorrect versions of PhotonView prefabs, such as Actor and UserBody.

echo Deleting import cache..please wait..
rm -rf Library
rm -rf obj

echo All done! Re-open Unity to proceed.
