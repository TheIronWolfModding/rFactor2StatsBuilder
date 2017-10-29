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

    static void Main(string[] args)
    {
      try
      {
        if (Directory.Exists(rFactor2StatsBuilder.OutRoot))
        {
          Console.WriteLine("Deleting old out root.");
          Directory.Delete(rFactor2StatsBuilder.OutRoot, true /*recursive*/);
        }
      }
      catch (Exception ex)
      {
        Utils.ReportException($"Failed to delete old out root {rFactor2StatsBuilder.OutRoot}", ex);
      }

      SourceExtractor.Extract();
    }
  }
}
