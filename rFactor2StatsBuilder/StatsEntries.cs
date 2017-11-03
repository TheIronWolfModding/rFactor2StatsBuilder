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
    internal string StaticCurve { get; set; }
    internal string[] TbcIDs { get; set; }
  }
}
