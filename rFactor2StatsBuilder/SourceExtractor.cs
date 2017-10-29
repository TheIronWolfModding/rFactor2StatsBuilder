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
      Utils.WriteLine($"Processing vehicle: {vehDir}", ConsoleColor.Green);

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

      foreach (var masFileFull in Directory.GetFiles(vehFullPath, "*.mas", SearchOption.AllDirectories))
      {
        Utils.WriteLine($"Processing .mas: {masFileFull}", ConsoleColor.Cyan);


      }
    }

  }
}
