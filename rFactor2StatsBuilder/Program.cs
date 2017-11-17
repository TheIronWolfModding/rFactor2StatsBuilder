/*

Author: The Iron Wolf (vleonavicius@hotmail.com)
Website: thecrewchief.org
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rFactor2StatsBuilder
{
  class rFactor2StatsBuilder
  {
    internal static string OutRoot { get; private set; } = @"c:\temp\masOut";
    internal static string VehRoot { get; private set; } = @"c:\temp\Vehicles";
    internal static string ModMgr { get; private set; } = @"C:\Games\Steam\SteamApps\common\rFactor 2\Bin32\ModMgr.exe";

    internal static List<string> warnings = new List<string>();
    internal static List<string> errors = new List<string>();

    static void Main(string[] args)
    {
      //SourceExtractor.Extract();
      StatsBuilder.Build();

      Utils.WriteLine("Errors:", ConsoleColor.Red);
      foreach (var e in rFactor2StatsBuilder.errors)
        Utils.WriteLine(e, ConsoleColor.Red);

      Utils.WriteLine("Warnings:", ConsoleColor.Yellow);
      foreach (var w in rFactor2StatsBuilder.warnings)
        Utils.WriteLine(w, ConsoleColor.Yellow);
    }
  }
}
