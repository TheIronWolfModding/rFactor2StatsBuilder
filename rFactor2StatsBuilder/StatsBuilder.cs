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
        string hdvFile = null;
        var vehEntry = StatsBuilder.ProcessVehFile(vehFileFull, vehDir, out hdvFile);
        if (vehEntry == null || string.IsNullOrWhiteSpace(hdvFile))
        {
          Utils.WriteLine($"Error: failed to process vehicle {vehFileFull}.", ConsoleColor.Red);
          continue;
        }

        var vehEntryStr = $"{vehEntry.VehID},{vehEntry.Version},{vehEntry.HdvID}";
        Utils.WriteLine($"VEH entry created: \"{vehEntryStr}\"", ConsoleColor.Magenta);

        string hdvFileFull = null;
        var hdvEntry = StatsBuilder.ProcessHdvFile(hdvFile, vehDirFull, vehDir, vehEntry.HdvID, vehFileFull, out hdvFileFull);
        if (hdvEntry == null || string.IsNullOrWhiteSpace(hdvEntry.TireBrand))
        {
          Utils.WriteLine($"Error: failed to process hdv file {hdvFileFull ?? ""} for vehicle {vehFileFull}.", ConsoleColor.Red);
          continue;
        }

        var hdvEntryStr = $"{hdvEntry.HdvID},{hdvEntry.Version},{hdvEntry.StopGo},{hdvEntry.StopGoSimultaneous},{hdvEntry.Preparation},{hdvEntry.DRSCapable},{hdvEntry.VehicleWidth},{hdvEntry.BrakeResponseCurveFrontLeft},{hdvEntry.BrakeResponseCurveFrontRight},{hdvEntry.BrakeResponseCurveRearLeft},{hdvEntry.BrakeResponseCurveRearRight},{hdvEntry.TbcIDPrefix}";
        Utils.WriteLine($"HDV entry matched: \"{hdvEntryStr}\"", ConsoleColor.Magenta);

        string tbcFileFull = null;
        var tbcEntries = StatsBuilder.ProcessTbcFile(hdvEntry.TireBrand, vehDirFull, vehDir, hdvEntry.TbcIDPrefix, vehFileFull, out tbcFileFull);
        //if (tbcEntries == null || string.IsNullOrWhiteSpace(tbcEntries.TireBrand))
        //{
//          Utils.WriteLine($"Error: failed to process hdv file {hdvFileFull ?? ""} for vehicle {vehFileFull}.", ConsoleColor.Red);
  //        continue;
    //    }


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
      if (!StatsBuilder.GetSectionValue(vehFileFull, section, "Description", out descr, false /*optional*/))
        return null;

      string cat;
      if (!StatsBuilder.GetSectionValue(vehFileFull, section, "Category", out cat, false /*optional*/))
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
      if (!StatsBuilder.GetSectionValue(vehFileFull, section, "HDVehicle", out hdvFileFull, false /*optional*/))
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
      var hdvID = $"hdv@@{vehDir}@@{hdvFile}".ToLowerInvariant();

      return new VehEntry() { VehID = vehID, Version = ver, HdvID = hdvID };
    }

    private static Dictionary<string, HdvEntry> hdvResolvedMap = new Dictionary<string, HdvEntry>();

    private static HdvEntry ProcessHdvFile(string hdvFile, string vehDirFull, string vehDir, string hdvId, string vehFileFull, out string hdvFileFull)
    {
      hdvFileFull = null;
      var hdvFiles = Directory.GetFiles(vehDirFull, hdvFile, SearchOption.AllDirectories);
      if (hdvFiles == null || hdvFiles.Length == 0)
      {
        Utils.WriteLine($"Error: failed to locate {hdvFile} for vehicle {vehFileFull}.", ConsoleColor.Red);
        return null;
      }
      else if (hdvFiles.Length > 1)
        Utils.WriteLine($"Warning: hdv file {hdvFile} is ambigous for vehicle {vehFileFull}.  Will use the first one: {hdvFiles[0]}. ", ConsoleColor.Yellow);

      hdvFileFull = hdvFiles[0];
      HdvEntry hdvEntry = null;
      if (StatsBuilder.hdvResolvedMap.TryGetValue(hdvFileFull, out hdvEntry))
        return hdvEntry;

      do
      {
        /*
         * Extracted stuff:
         * [GENERAL]
         * TireBrand=
         * 
         * [PITMENU]
         * StopGo=1                     // Whether stop/go pit menu item is available (highly recommended); default=1
         *    - Penalty announcement tweak.
         * 
         * StopGoSimultaneous=0         // Whether stop/go penalties can be served during a regular pit stop (time is added at end); default=0
         * Preparation=(150,24,0.5,4.5) // When crew gives up after request, crew prep time, delay multiplier for how much more time was needed to prep, max delay; default=(150,25,0.5,4.5)
         *
         *    Opportunities:
         *      There are bunch of options for voice command (menu items).
          *      Tire/Fuel pit time estimation
         *
         * [PLUGIN]
         *      Opportunities: sensors, what is available?
         *
         * [REARWING]
         * FlapDrag=(3.2e-4,0.94888)
         * FlapLift=(-0.01,0.90319)
         * FlapTimes=(0.04,0.08,0.04,0.3)
         * FlapRules=(-1,0.0081)
         * 
         * [BODYAERO]
         * VehicleWidth= exact width
         *    Opportunities: suggest adjusting BrakeDuctOpening, RadiatorOpening
         *    
         * [FRONTLEFT] // ZF SACHS Race Engineering Dampers FBT23 (front) and FBT24 (rear)
         * [FRONTRIGHT]
         * [REARLEFT]
         * [REARRIGHT]
         * BrakeResponseCurve=(-160,350,600,1590) // Cold temperature (where brake torque is half optimum), min temp for optimum brake torque, max temp for optimum brake torque, and overheated temperature (where brake torque is half optimum)
         */

        Utils.WriteLine($"\nProcessing .hdv: {hdvFileFull}", ConsoleColor.Cyan);

        var hdvFileReader = new KindOfSortOfIniFile(hdvFileFull);

        //////////////////////////////////////////
        // [GENERAL] section.
        //////////////////////////////////////////
        Dictionary<string, string> section;
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("GENERAL", out section))
        {
          Utils.WriteLine($"Error: [GENERAL] section not found in file {hdvFileFull}.", ConsoleColor.Red);
          break;
        }

        // TODO: tireBrand might depend on upgrade selected.
        string tireBrand = null;
        if (!StatsBuilder.GetSectionValue(hdvFileFull, section, "TireBrand", out tireBrand, false /*optional*/))
          break;

        tireBrand = tireBrand.ToLowerInvariant();

        // Some mods add .tbc, chop it off.
        if (tireBrand.EndsWith(".tbc"))
          tireBrand = tireBrand.Substring(0, tireBrand.Length - 4);

        //////////////////////////////////////////
        // [PITMENU] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("PITMENU", out section))
        {
          Utils.WriteLine($"Error: [PITMENU] section not found in file {hdvFileFull}.", ConsoleColor.Red);
          break;
        }

        var stopGo = "1";
        StatsBuilder.GetSectionValue(hdvFileFull, section, "StopGo", out stopGo, true /*optional*/);

        var stopGoSimultaneous = "0";
        StatsBuilder.GetSectionValue(hdvFileFull, section, "StopGoSimultaneous", out stopGoSimultaneous, true /*optional*/);

        string preparation = null;
        if (!StatsBuilder.GetSectionValue(hdvFileFull, section, "Preparation", out preparation, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(preparation, out preparation))
          break;

        //////////////////////////////////////////
        // [REARWING] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("REARWING", out section))
        {
          Utils.WriteLine($"Error: [REARWING] section not found in file {hdvFileFull}.", ConsoleColor.Red);
          break;
        }

        string DRSCapable = null;
        if (StatsBuilder.GetSectionValue(hdvFileFull, section, "FlapDrag", out DRSCapable, true /*optional*/)
          && StatsBuilder.GetSectionValue(hdvFileFull, section, "FlapLift", out DRSCapable, true /*optional*/)
          && StatsBuilder.GetSectionValue(hdvFileFull, section, "FlapTimes", out DRSCapable, true /*optional*/)
          && StatsBuilder.GetSectionValue(hdvFileFull, section, "FlapRules", out DRSCapable, true /*optional*/))
          DRSCapable = "1";
        else
          DRSCapable = "0";

        //////////////////////////////////////////
        // [BODYAERO] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("BODYAERO", out section))
        {
          Utils.WriteLine($"Error: [BODYAERO] section not found in file {hdvFileFull}.", ConsoleColor.Red);
          break;
        }

        // TODO:  Probably drop this as it isn't critical and kills TCR mod.
        string vehicleWidth = null;
        if (!StatsBuilder.GetSectionValue(hdvFileFull, section, "VehicleWidth", out vehicleWidth, false /*optional*/))
          break;

        //////////////////////////////////////////
        // [FRONTLEFT] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("FRONTLEFT", out section))
        {
          Utils.WriteLine($"Error: [FRONTLEFT] section not found in file {hdvFileFull}.", ConsoleColor.Red);
          break;
        }

        string frontLeftBrakeCurve = null;
        if (!StatsBuilder.GetSectionValue(hdvFileFull, section, "BrakeResponseCurve", out frontLeftBrakeCurve, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(frontLeftBrakeCurve, out frontLeftBrakeCurve))
          break;

        //////////////////////////////////////////
        // [FRONTRIGHT] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("FRONTRIGHT", out section))
        {
          Utils.WriteLine($"Error: [FRONTRIGHT] section not found in file {hdvFileFull}.", ConsoleColor.Red);
          break;
        }

        string frontRightBrakeCurve = null;
        if (!StatsBuilder.GetSectionValue(hdvFileFull, section, "BrakeResponseCurve", out frontRightBrakeCurve, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(frontRightBrakeCurve, out frontRightBrakeCurve))
          break;

        //////////////////////////////////////////
        // [REARLEFT] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("REARLEFT", out section))
        {
          Utils.WriteLine($"Error: [REARLEFT] section not found in file {hdvFileFull}.", ConsoleColor.Red);
          break;
        }

        string rearLeftBrakeCurve = null;
        if (!StatsBuilder.GetSectionValue(hdvFileFull, section, "BrakeResponseCurve", out rearLeftBrakeCurve, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(rearLeftBrakeCurve, out rearLeftBrakeCurve))
          break;

        //////////////////////////////////////////
        // [REARRIGHT] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("REARRIGHT", out section))
        {
          Utils.WriteLine($"Error: [REARRIGHT] section not found in file {hdvFileFull}.", ConsoleColor.Red);
          break;
        }

        string rearRightBrakeCurve = null;
        if (!StatsBuilder.GetSectionValue(hdvFileFull, section, "BrakeResponseCurve", out rearRightBrakeCurve, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(rearRightBrakeCurve, out rearRightBrakeCurve))
          break;

        var ver = new DirectoryInfo(hdvFileFull).Parent.Name;

        hdvEntry = new HdvEntry()
        {
          HdvID = hdvId,
          Version = ver,
          StopGo = stopGo,
          StopGoSimultaneous = stopGoSimultaneous,
          Preparation = preparation,
          DRSCapable = DRSCapable,
          VehicleWidth = vehicleWidth,
          BrakeResponseCurveFrontLeft = frontLeftBrakeCurve,
          BrakeResponseCurveFrontRight = frontRightBrakeCurve,
          BrakeResponseCurveRearLeft = rearLeftBrakeCurve,
          BrakeResponseCurveRearRight = rearRightBrakeCurve,
          TbcIDPrefix = $"tbc@@{vehDir}@@{tireBrand}@@".ToLowerInvariant(),
          TireBrand = tireBrand
        };
      }
      while (false);

      StatsBuilder.hdvResolvedMap.Add(hdvFileFull, hdvEntry);

      return hdvEntry;
    }

    private static TbcEntry[] ProcessTbcFile(string tireBrand, string vehDirFull, string vehDir, string tbcIDPrefix, string vehFileFull, out string tbcFileFull)
    {
      tbcFileFull = null;
      var tbcFile = tireBrand + ".tbc";
      var tbcFiles = Directory.GetFiles(vehDirFull, tbcFile, SearchOption.AllDirectories);
      if (tbcFiles == null || tbcFiles.Length == 0)
      {
        Utils.WriteLine($"Error: failed to locate {tbcFile} for vehicle {vehFileFull}.", ConsoleColor.Red);
        return null;
      }
      else if (tbcFiles.Length > 1)
        Utils.WriteLine($"Warning: tbc file {tbcFile} is ambigous for vehicle {vehFileFull}.  Will use the first one: {tbcFiles[0]}. ", ConsoleColor.Yellow);

      tbcFileFull = tbcFiles[0];
      //HdvEntry hdvEntry = null;
      //if (StatsBuilder.hdvResolvedMap.TryGetValue(hdvFileFull, out hdvEntry))
      //return null;

      return null;
    }

    private static bool RemoveParens(string preparation, out string preparationOut)
    {
      if (!string.IsNullOrWhiteSpace(preparation) && preparation.StartsWith("(") && preparation.EndsWith(")"))
      {
        preparationOut = preparation.Substring(1, preparation.Length - 2);
        return true;
      }
      preparationOut = preparation;
      return false;
    }

    private static bool GetSectionValue(string vehFileFull, Dictionary<string, string> section, string key, out string value, bool optional)
    {
      if (!section.TryGetValue(key.ToUpperInvariant(), out value))
      {
        if (!optional)
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
