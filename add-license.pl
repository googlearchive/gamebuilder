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

$GOOGLE_LICENSE_REGEX = qr/\n[#* ]*Copyright 2019 Google LLC/;
$OTHER_LICENSE_REGEX = qr([Cc]opyright);

$LOG_INFO = "[\x1B[1;30mINFO\x1B[0m]";
$LOG_ERROR = "[\x1B[1;31mERROR\x1B[0m]";
$LOG_WARN = "[\x1B[1;33mWARNING\x1B[0m]";

$TEMPLATE_C = <<'END_TEMPLATE_C';
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

END_TEMPLATE_C

$TEMPLATE_SH = <<'END_TEMPLATE_SH';
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

END_TEMPLATE_SH

$TEMPLATE_HTML = <<'END_TEMPLATE_HTML';
<!--
  Copyright 2019 Google LLC

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

    https://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
-->

END_TEMPLATE_HTML

if (0 == @ARGV) {
  print STDERR "Syntax: add-license.pl [file1 [file2 [file3 ... ] ] ]\n";
  exit 1;
}
while ($file = shift) {
  my $error = add_license($file);
  if ($error) {
    print STDERR "$LOG_ERROR $file: $error\n";
  }
}

sub add_license {
  my $file = shift;

  # Find out what license format to use.
  my $template = "";

  $file =~ /\.cs$/ and $template = $TEMPLATE_C;
  $file =~ /\.js\.txt$/ and $template = $TEMPLATE_C;
  $file =~ /\.js$/ and $template = $TEMPLATE_C;
  $file =~ /\.c$/ and $template = $TEMPLATE_C;
  $file =~ /\.cpp$/ and $template = $TEMPLATE_C;
  $file =~ /\.h$/ and $template = $TEMPLATE_C;
  $file =~ /\.css$/ and $template = $TEMPLATE_C;
  $file =~ /\.shader$/ and $template = $TEMPLATE_C;

  $file =~ /\.sh$/ and $template = $TEMPLATE_SH;
  $file =~ /\.pl$/ and $template = $TEMPLATE_SH;
  $file =~ /\.py$/ and $template = $TEMPLATE_SH;

  $file =~ /\.html$/ and $template = $TEMPLATE_HTML;

  if ($template eq "") {
    return "Error: not a recognized file type: $file\n";
  }

  # Read full contents of file.
  open my $fh, $file or return "Failed to read $file";
  my $contents = do { local $/; <$fh> };
  close $fh;

  # Is it a DOS file or a Unix file?
  my $line_ending = $contents =~ /\r\n/ ? "\r\n" : "\n";

  # Does it already contain the copyright notice? (must be at the start
  # of the file).
  if (substr($contents, 0, 100) =~ /$GOOGLE_LICENSE_REGEX/s) {
    print STDERR "$LOG_INFO $file: already has a Google LLC copyright notice.\n";
    return "";  # Not an error
  }

  # Does the file already have some other copyright?
  if ($contents =~ $OTHER_LICENSE_REGEX) {
    return "File has a (NON-GOOGLE) copyright notice??";
  }

  # We must preserve the BOM and the shell/HTML declaration line at the
  # beginning of the file when inserting the copyright notice.
  my $header = "";
  my $body = $contents;
  if ($contents =~ /^(\xEF\xBB\xBF)?([#<]![^\n\r]*\r?\n)?(.*)$/s) {
    $header = "$1$2";
    $body = $3;
  }

  # Add copyright notice to top of body, keeping line endings.
  my $notice = $template;
  $notice =~ s/\r//g;
  $notice =~ s/\n/$line_ending/g;
  $body = "$notice$body";

  open OUT, ">$file" or return "Failed to write file.";
  print OUT "$header$body";
  close OUT;

  print STDERR "$LOG_INFO $file: successfully added copyright notice.\n";

  return "";
}

