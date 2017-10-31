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

      Utils.WriteLine($"\nExtracting sources for vehicles in: {rFactor2StatsBuilder.VehRoot}", ConsoleColor.Green);
      foreach (var vehDirFull in Directory.GetDirectories(rFactor2StatsBuilder.VehRoot))
      {
        var vehDir = new DirectoryInfo(vehDirFull).Name;

        SourceExtractor.ProcessSingleVehicle(vehDirFull, vehDir);
      }
    }

    private static void ProcessSingleVehicle(string vehDirFull, string vehDir)
    {
      Utils.WriteLine($"\nProcessing vehicle: {vehDir}", ConsoleColor.Green);

      foreach (var versionDirFull in Directory.GetDirectories(vehDirFull))
      {
        var verDir = new DirectoryInfo(versionDirFull).Name;
        var vehOutDirFull = $"{rFactor2StatsBuilder.OutRoot}\\{vehDir}\\{verDir}";
        try
        {
          Directory.CreateDirectory(vehOutDirFull);
        }
        catch (IOException ex)
        {
          Utils.ReportException($"Failed to create directory: {vehOutDirFull} ", ex);
          throw ex;
        }

        foreach (var masFileFull in Directory.GetFiles(versionDirFull, "*.mas", SearchOption.AllDirectories))
        {
          Utils.WriteLine($"\nProcessing .mas: {masFileFull}", ConsoleColor.Cyan);

          var psi = new ProcessStartInfo(rFactor2StatsBuilder.ModMgr, $"*.veh *.tbc *.hdv -x\"{masFileFull}\" -o\"{vehOutDirFull}\"");
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
}
