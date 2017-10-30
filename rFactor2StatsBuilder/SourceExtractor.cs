/*

Author: The Iron Wolf (vleonavicius@hotmail.com)
Website: thecrewchief.org
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rFactor2StatsBuilder
{
  static class SourceExtractor
  {
    internal static void Extract()
    {
      foreach (var vehDirFull in Directory.GetDirectories(rFactor2StatsBuilder.VehRoot))
      {
        var vehDir = new DirectoryInfo(vehDirFull).Name;

        SourceExtractor.ProcessSingleVehicle(vehDirFull, vehDir);
      }
    }

    private static void ProcessSingleVehicle(string vehFullPath, string vehDir)
    {
      Utils.WriteLine($"\nProcessing vehicle: {vehDir}", ConsoleColor.Green);

      var vehOutPath = $"{rFactor2StatsBuilder.OutRoot}\\{vehDir}";
      try
      {
        Directory.CreateDirectory(vehOutPath);
      }
      catch (IOException ex)
      {
        Utils.ReportException($"Failed to create directory: {vehOutPath} ", ex);
        throw ex;
      }

      if (Directory.GetDirectories(vehFullPath).Length > 1)
        Utils.WriteLine($"Warning: {vehDir} contains more than one version folders.", ConsoleColor.Yellow);

      foreach (var masFileFull in Directory.GetFiles(vehFullPath, "*.mas", SearchOption.AllDirectories))
      {
        Utils.WriteLine($"\nProcessing .mas: {masFileFull}", ConsoleColor.Cyan);

        var psi = new ProcessStartInfo(rFactor2StatsBuilder.ModMgr, $"*.veh *.tbc *.hdv -x{masFileFull} -o{vehOutPath}");
        var process = new Process() { StartInfo = psi };

        process.Start();

        // Not calling for wait is faster, but produce weird console output.  So for now, keep things synchronous.
        process.WaitForExit();

        // ModMgr does not return error code on failure.  We could possibly parse the console output somehow,
        // but for now, simply let resolution of things fail during stats build phase.
      }
    }

  }
}
