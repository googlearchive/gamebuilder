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

## Syntax:
##   bench-diff from-benchmark.json to-benchmark.json
##
## Prints human-readable stats diffs between two
## benchmark files.

$FORCE = 0;

if ($ARGV[0] eq "--force") {
  shift;
  $FORCE = 1;
}

scalar(@ARGV) != 2 and do {
  system "grep '^##' \"$0\" | cut -c4-80";
  exit 1;
};

$from_text = qx(cat "$ARGV[0]");
$to_text = qx(cat "$ARGV[1]");

$from_sett = get_settings($from_text);
$to_sett = get_settings($to_text);

if ($from_sett ne $to_sett) {
  print "FROM file settings are different from TO file settings.\n\n";
  print "FROM file settings:\n$from_sett\n\nTO file settings:\n$to_sett\n\n";
  print "This makes the diff meaningless.\n";
  if ($FORCE) {
    print "\n!!! Proceeding anyway, since --force was specified.\n";
  } else {
    print "If you want to proceed, use --force.\n";
    print "*** Aborted.\n";
    exit 1;
  }
}

@INTERESTING_FIELDS = qw(avgFrameMs avgVoosUpdateMs loadToVoos);

@voos_files = ();
while ($from_text =~ /voosFile": "([^"]+)/g) {
  $voos_file = $1;
  print "VOOS FILE: $voos_file\n";
  for $field_name (@INTERESTING_FIELDS) {
    $oldValue = get_field($from_text, $voos_file, $field_name);
    $newValue = get_field($to_text, $voos_file, $field_name);
    printf "  %20s: %7.2f -> %7.2f (%s%s%7.2f\x1B[0m)\n", 
      $field_name,
      $oldValue, $newValue,
      ($newValue > $oldValue ? "\x1B[1;31m" : "\x1B[1;32m"),
      ($newValue > $oldValue ? "+" : "-"),
      abs($newValue - $oldValue);
  }
  print "\n";
}

sub get_field {
  my ($text, $voos_file, $field_name) = @_;
  $text =~ /"voosFile": "$voos_file"[^}]*"$field_name": ([0-9.]+),/s;
  return $1 * 1;
}

sub get_settings {
  my $text = shift;
  $text =~ /("host":.*)"results":/s or die "Failed to find settings in json file.\n";
  my $sett = $1;
  $sett =~ s/^\s+//mg;
  return $sett;
}

