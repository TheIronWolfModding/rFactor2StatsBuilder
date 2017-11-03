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
        string hdvFile;
        var vehEntry = StatsBuilder.ProcessVehFile(vehFileFull, vehDir, out hdvFile);
        if (vehEntry == null || string.IsNullOrWhiteSpace(hdvFile))
        {
          Utils.WriteLine($"Error: failed to process vehicle {vehFileFull}.", ConsoleColor.Red);
          continue;
        }

        Utils.WriteLine($"VEH entry created: {vehEntry.VehID},{vehEntry.Version},{vehEntry.HdvID}", ConsoleColor.Magenta);

        var hdvEntry = StatsBuilder.ProcessHdvFile(hdvFile, vehDirFull, vehDir, vehEntry.Version, vehEntry.HdvID, vehFileFull);

      }
    }

    private static VehEntry ProcessVehFile(string vehFileFull, string vehDir, out string hdvFile)
    {
      hdvFile = "";

      Utils.WriteLine($"\nProcessing .veh: {vehFileFull}", ConsoleColor.Cyan);

      var vehFileReader = new KindOfSortOfIniFile(vehFileFull);

      Dictionary<string, string> section;
      if (!vehFileReader.sectionsToKeysToValuesMap.TryGetValue("", out section))
      {
        Utils.WriteLine($"Error: global section not found in file {vehFileFull}.", ConsoleColor.Red);
        return null;
      }

      string descr;
      if (!StatsBuilder.GetSectionValue(vehFileFull, section, "Description", out descr))
        return null;

      string cat;
      if (!StatsBuilder.GetSectionValue(vehFileFull, section, "Category", out cat))
        return null;

      var catExtracted = StatsBuilder.ExtractCategory(cat);
      if (string.IsNullOrWhiteSpace(catExtracted))
      {
        Utils.WriteLine($"Error: Failed to parse categroy out of {cat} in {vehFileFull}.", ConsoleColor.Red);
        return null;
      }

      var descrExtracted = StatsBuilder.UnquoteString(descr);
      if (string.IsNullOrWhiteSpace(descrExtracted))
      {
        Utils.WriteLine($"Error: Failed to parse description out of {descr} in {vehFileFull}.", ConsoleColor.Red);
        return null;
      }

      string hdvFileFull;
      if (!StatsBuilder.GetSectionValue(vehFileFull, section, "HDVehicle", out hdvFileFull))
        return null;

      var lastSlash = hdvFileFull.LastIndexOf('\\');
      hdvFile = hdvFileFull;
      if (lastSlash != -1)
        hdvFile = hdvFileFull.Substring(lastSlash + 1);

      if (string.IsNullOrWhiteSpace(hdvFile))
      {
        Utils.WriteLine($"Error: Failed to parse .hdv file name out of {hdvFileFull} in {vehFileFull}.", ConsoleColor.Red);
        return null;
      }

      // veh_1
      var vehID = $"{descrExtracted}@@{catExtracted}".ToLowerInvariant();

      // veh_2
      var ver = new DirectoryInfo(vehFileFull).Parent.Name;

      // veh_3
      var hdvID = $"hdv@@{vehDir}@@{ver}@@{hdvFile}".ToLowerInvariant();

      return new VehEntry() { VehID = vehID, Version = ver, HdvID = hdvID };
    }

    private static Dictionary<string, HdvEntry> hdvResolvedMap = new Dictionary<string, HdvEntry>();

    private static HdvEntry ProcessHdvFile(string hdvFile, string vehDirFull, string vehDir, string vehVer, string hdvId, string vehFileFull)
    {
      var hdvFiles = Directory.GetFiles(vehDirFull, hdvFile, SearchOption.AllDirectories);
      if (hdvFiles == null || hdvFiles.Length == 0)
      {
        Utils.WriteLine($"Error: failed to locate {hdvFile} for vehicle {vehFileFull}.", ConsoleColor.Red);
        return null;
      }
      else if (hdvFiles.Length > 1)
        Utils.WriteLine($"Warning: hdv file {hdvFile} is ambigous for vehicle {vehFileFull}.  Will use the first one: {hdvFiles[0]}. ", ConsoleColor.Yellow);

      var hdvFileFull = hdvFiles[0];
      HdvEntry hdvEntry = null;
      if (StatsBuilder.hdvResolvedMap.TryGetValue(hdvFileFull, out hdvEntry))
        return hdvEntry;

      Utils.WriteLine($"\nProcessing .hdv: {hdvFileFull}", ConsoleColor.Cyan);

      return null;
    }

    private static bool GetSectionValue(string vehFileFull, Dictionary<string, string> section, string key, out string value)
    {
      if (!section.TryGetValue(key, out value))
      {
        Utils.WriteLine($"Error: '{key}' key value not found in file {vehFileFull}.", ConsoleColor.Red);
        return false;
      }

      return true;
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
