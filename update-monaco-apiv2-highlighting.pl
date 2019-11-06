#!/usr/bin/perl
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

$DEFS_FILE = "BrowserAssets/monaco-editor-0.14.3/min/vs/basic-languages/javascript/javascript.js";
$JS_KEYWORDS = '"break","case","catch","class","continue","const","constructor","debugger","default","delete","do","else","export","extends","false","finally","for","from","function","get","if","import","in","instanceof","let","new","null","return","set","super","switch","symbol","this","throw","true","try","typeof","undefined","var","void","while","with","yield","async","await","of"';

$jsdef = qx(cat $DEFS_FILE);
$? eq 0 or die "Failed to read Monaco js defs file from $DEFS_FILE";

@words = $JS_KEYWORDS =~ /"(\w+)"/g;

@js_files = split(/\s+/,qx(find Assets/Scripts/Behaviors/JavaScript/apiv2 -name '*.js.txt'));

for $js_file (@js_files) {
  $contents = qx(cat $js_file);
  $? eq 0 or die "Failed to read JS file $js_file.";
  next unless $contents =~ /VISIBLE_TO_MONACO/;
  print STDERR "Processing JS file $js_file...\n";

  $contents =~ s/^\/\/\s+DOC_ONLY:\s+//mg;
  
  @functions = ($contents =~ /^function (\w+)\(/mg);
  print "FUNCTION: $_\n" for @functions;
  push @words, $_ for @functions;

  @globs = ($contents =~ /^var (\w+)/mg);
  print "GLOB: $_\n" for @globs;
  push @words, $_ for @globs;
}

@words = sort @words;

@words_quoted = ();
push @words_quoted, "\"$_\"" for @words;
$list = join(",", @words_quoted);

$jsdef =~ s/tokenPostfix:\s*"\.js"\s*,\s*keywords:\s*\[[^]]+\]/tokenPostfix:".js",keywords:[$list]/ or die "Failed to match keywords: portion of Monaco js defs.";

open OUT, ">$DEFS_FILE" or die "Failed to write $DEFS_FILE";
print OUT $jsdef;
close OUT;

print STDERR "Done.\n";

