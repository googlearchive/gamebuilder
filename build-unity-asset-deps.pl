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


$OUTPUT_FILE = "unity-asset-deps.txt";

open OUT, ">$OUTPUT_FILE" or die "Failed to write $OUTPUT_FILE\n";

@asset_files = qx(find Assets -name '*.prefab' -o -name '*.unity' -o -name '*.cs');
chomp for @asset_files;

# Contents of all files
%file_contents = {};
# Map from GUID to asset file name
%guid_to_file = {};

# Load all files into memory.
$done = 0;
$count = scalar(@asset_files);
for $asset_file (@asset_files) {
  open my $fh, $asset_file or die "Can't open $asset_file\n";
  $file_contents{$asset_file} = do { local $/; <$fh> };
  close $fh;
  open my $fh, "$asset_file.meta" or die "Can't open $asset_file.meta\n";
  $meta_contents = do { local $/; <$fh> };
  close $fh;
  $meta_contents =~ /guid: (\w+)/ or die "File $asset_file has no GUID.\n";
  $guid_to_file{$1} = $asset_file;
  $done++;
  $done % 10 == 0 and print STDERR "Loading files $done / $count...   \r";
}
print STDERR "\nDone loading files.\n";

# Output dependencies for all files
$done = 0;
$count = scalar(@asset_files);
%already_printed = {};
%had_dependency = {};
for $asset_file (@asset_files) {
  $done++;
  print STDERR "Analyzing deps $done / $count....         \r";

  $asset_file =~ /\.cs$/ and next;  # .cs files don't depend on anything

  $contents = $file_contents{$asset_file};
  while ($contents =~ /guid: (\w+)/sg) {
    $file_dep = $guid_to_file{$1};
    if ($file_dep) {
      # Abbreviate: 
      (my $p_asset_file = $asset_file) =~ s/^Assets\///;
      (my $p_file_dep = $file_dep) =~ s/^Assets\///;

      $line = "$p_asset_file: $p_file_dep\n";
      print OUT $line unless $already_printed{$line};
      $already_printed{$line} = 1;
      $had_dependency{$file_dep} = 1;
    }
  }
}

for $asset_file (@asset_files) {
  if ($asset_file !~ /third_party/
      && $asset_file !~ /Assets\/GameAssets\/Resources\/BuiltinAssets/
      && $asset_file !~ /Assets\/GameAssets\/Prefabs\/Environment/
      && $asset_file !~ /Assets\/Scripts\/Tests/
      && $asset_file !~ /unity$/m
      && !$had_dependency{$asset_file}) {
    print OUT "no deps: $asset_file\n";
  }
}

print STDERR "\nDone analyzing dependencies.\n";

close OUT;
print "Wrote $OUTPUT_FILE\n";

