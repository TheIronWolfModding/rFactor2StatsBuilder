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
  static class StatsBuilder
  {
    internal static void Build()
    {
      Utils.WriteLine($"\nBuilding stats from sources in: {rFactor2StatsBuilder.OutRoot}", ConsoleColor.Green);
      foreach (var vehDirFull in Directory.GetDirectories(rFactor2StatsBuilder.OutRoot))
      {
        var vehDir = new DirectoryInfo(vehDirFull).Name;

        StatsBuilder.ProcessSingleVehicle(vehDirFull, vehDir);
      }
    }

    private static void ProcessSingleVehicle(string vehDirFull, string vehDir)
    {
      Utils.WriteLine($"\nProcessing vehicle: {vehDir}", ConsoleColor.Green);
      foreach (var vehFileFull in Directory.GetFiles(vehDirFull, "*.veh", SearchOption.AllDirectories))
      {
        Utils.WriteLine($"\nProcessing .veh: {vehFileFull}", ConsoleColor.Cyan);

        var vehFileReader = new KindOfSortOfIniFile(vehFileFull);

        Dictionary<string, string> section;
        if (!vehFileReader.sectionsToKeysToValuesMap.TryGetValue("", out section))
        {
          Utils.WriteLine($"Error: global section not found in file {vehFileFull}.", ConsoleColor.Red);
          continue;
        }

        string descr;
        if (!section.TryGetValue("Description", out descr))
        {
          Utils.WriteLine($"Error: 'Description' key value not found in file {vehFileFull}.", ConsoleColor.Red);
          continue;
        }

        string cat;
        if (!section.TryGetValue("Category", out cat))
        {
          Utils.WriteLine($"Error: 'Category' key value not found in file {vehFileFull}.", ConsoleColor.Red);
          continue;
        }

        var catExtracted = StatsBuilder.ExtractCategory(cat);
        if (string.IsNullOrWhiteSpace(catExtracted))
        {
          Utils.WriteLine($"Error: Failed to parse categroy out of {cat} in {vehFileFull}.", ConsoleColor.Red);
          continue;
        }

        var descrExtracted = StatsBuilder.UnquoteString(descr);
        if (string.IsNullOrWhiteSpace(descrExtracted))
        {
          Utils.WriteLine($"Error: Failed to parse description out of {descr} in {vehFileFull}.", ConsoleColor.Red);
          continue;
        }

        var vehId = $"{descrExtracted}@@{catExtracted}".ToLowerInvariant();
        Utils.WriteLine(vehId);

        //      var descr = vehFileReader.Read("Description", "GENERAL");


      }

    }

    private static string UnquoteString(string str)
    {
      if (!str.StartsWith("\"") || !str.EndsWith("\"") || str.Length < 3)
        return null;

      str = str.Substring(1, str.Length - 2);
      return str.Trim();
    }

    private static string ExtractCategory(string categoryRaw)
    {
      if (categoryRaw.Length < 3)
        return null;

      var lastComma = categoryRaw.LastIndexOf(',');
      if (lastComma != -1)
      {
        var retCat = categoryRaw.Substring(lastComma + 1, categoryRaw.Length - lastComma - 2);
        retCat = retCat.Trim();
        return retCat;
      }
      else if (categoryRaw.StartsWith("\"") && categoryRaw.EndsWith("\""))
      {
        var retCat = categoryRaw.Substring(1, categoryRaw.Length - 2);
        retCat = retCat.Trim();
        return retCat;
      }

      return null;
    }
  }
}
