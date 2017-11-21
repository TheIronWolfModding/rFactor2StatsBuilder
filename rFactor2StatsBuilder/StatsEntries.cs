/*

Author: The Iron Wolf (vleonavicius@hotmail.com)
Website: thecrewchief.org
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rFactor2StatsBuilder
{
  internal class VehEntry
  {
    internal string VehID { get; set; }
    internal string Version { get; set; }
    internal string HdvID { get; set; }
  }

  internal class HdvEntry
  {
    internal string HdvID { get; set; }
    internal string Version { get; set; }
    internal string StopGo { get; set; }
    internal string StopGoSimultaneous { get; set; }
    internal string Preparation { get; set; }
    internal string DRSCapable { get; set; }
    internal string VehicleWidth { get; set; }
    internal string BrakeResponseCurveFrontLeft { get; set; }
    internal string BrakeResponseCurveFrontRight { get; set; }
    internal string BrakeResponseCurveRearLeft { get; set; }
    internal string BrakeResponseCurveRearRight { get; set; }
    internal string TbcIDPrefix { get; set; }

    // Internal (not part of entries).
    internal string TireBrand { get; set; }
  }

  internal class TbcEntry
  {
    internal string TbcID { get; set; }
    internal string Version { get; set; }
    internal string WetWeather { get; set; }
    internal string FrontTgmID { get; set; }
    internal string RearTgmID { get; set; }

    // Internal (not part of entries).
    internal string FrontTGM { get; set; }
    internal string RearTGM { get; set; }
  }

  internal class TgmEntry
  {
    internal string TgmID { get; set; }
    internal string Version { get; set; }
    internal string StaticCurve { get; set; }
  }
}
