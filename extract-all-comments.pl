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

@files = split(/\n/, qx(git ls-files));
chomp for @files;

%EXTENSIONS = (
  c => "C_STYLE",
  cpp => "C_STYLE",
  cs => "C_STYLE",
  css => "C_STYLE",
  cxx => "C_STYLE",
  h => "C_STYLE",
  hpp => "C_STYLE",
  html => "HTML_STYLE",
  js => "C_STYLE",
  json => "C_STYLE",
  md => "SHELL_STYLE",
  pl => "SHELL_STYLE",
  py => "SHELL_STYLE",
  sh => "SHELL_STYLE",
  shader => "C_STYLE",
  ts => "C_STYLE",
  txt => "C_STYLE",  # Because of .js.txt
);


for $file (@files) {
  $file =~ /^third_party\// and next;
  $file =~ /\/third_party\// and next;
  $file =~ /\/TextMesh Pro\// and next;

  # Get file extension.
  $ext = "";
  $file =~ /\.(\w+)$/ and $ext = $1;

  # Skip files if extension is not in set above.
  $style = $EXTENSIONS{$ext};
  next unless $style;

  # Get file contents.
  open my $fh, $file or die "Failed to read $file\n";
  my $contents = do { local $/; <$fh> };
  close $fh;

  my @comments = ();

  if ($style eq "C_STYLE") {
    while ($contents =~ /(\/\*.*?\*\/)/sg) {
      $comment = $1;
      next if $comment =~ /Copyright \d\d\d\d Google LLC/;
      next if $comment =~ /\/*ASEBEGIN/;
      push @comments, $comment;
    }
    while ($contents =~ /[^:](\/\/.*$)/mg) {
      next if $comment =~ /\/\/ GENERATED/;
      $comment = $1;
      push @comments, $comment;
    }
  } elsif ($style eq "SHELL_STYLE") {
    while ($contents =~ /(#.*$)/mg) {
      $comment = $1;
      push @comments, $comment;
    }
  } elsif ($style eq "HTML_STYLE") {
    while ($contents =~ /(<!-.*?->)/sg) {
      $comment = $1;
      push @comments, $comment;
    }
  } else {
    die "Unknown comment style $style\n";
  }

  next unless scalar(@comments) > 0;
  
  print "\n$file:\n";

  for $item (@comments) {
    $item =~ s/\r//g;
    $item =~ s/\n/\n   /g;
    print "   $item\n";
  }
}

