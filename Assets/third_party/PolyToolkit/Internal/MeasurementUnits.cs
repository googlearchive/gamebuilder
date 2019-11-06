// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace PolyToolkitInternal {

/// <summary>
/// Measurement units.
/// </summary>
public enum LengthUnit {
  METERS,
  DECIMETERS,
  CENTIMETERS,
  MILLIMETERS,
  FEET,
  INCHES,
}

/// <summary>
/// Represents a length that's composed of an amount and a unit (for example "3 feet").
/// </summary>
[Serializable]
public struct LengthWithUnit {
  public float amount;
  public LengthUnit unit;

  public LengthWithUnit(float amount, LengthUnit unit) {
    this.amount = amount;
    this.unit = unit;
  }

  public static LengthWithUnit FromMeters(float meters, LengthUnit unit) {
    return new LengthWithUnit(meters / MeasurementUnits.ToMeters(unit), unit);
  }

  public float ToMeters() {
    return amount * MeasurementUnits.ToMeters(unit);
  }

  public override string ToString() {
    return string.Format("{0} {1}", amount, unit);
  }
}

public static class MeasurementUnits {
  /// <summary>
  /// Converts the given unit of length to meters.
  /// </summary>
  /// <param name="unit">The unit of length to convert.</param>
  /// <returns>The corresponding length in meters.</returns>
  public static float ToMeters(LengthUnit unit) {
    switch (unit) {
      case LengthUnit.METERS:
        return 1.0f;
      case LengthUnit.DECIMETERS:
        return 0.1f;
      case LengthUnit.CENTIMETERS:
        return 0.01f;
      case LengthUnit.MILLIMETERS:
        return 0.001f;
      case LengthUnit.FEET:
        return 0.3048f;
      case LengthUnit.INCHES:
        return 0.0254f;
      default:
        throw new Exception("Invalid length unit: " + unit);
    }
  }
}

}
