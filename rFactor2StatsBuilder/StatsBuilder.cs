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
          Utils.ReportError($"failed to process vehicle {vehFileFull}.");
          continue;
        }

        var vehEntryStr = $"{vehEntry.VehID},{vehEntry.Version},{vehEntry.HdvID}";
        Utils.WriteLine($"VEH entry created: \"{vehEntryStr}\"", ConsoleColor.Magenta);

        string hdvFileFull = null;
        var hdvEntry = StatsBuilder.ProcessHdvFile(hdvFile, vehDirFull, vehDir, vehEntry.HdvID, vehFileFull, out hdvFileFull);
        if (hdvEntry == null || string.IsNullOrWhiteSpace(hdvEntry.TireBrand))
        {
          Utils.ReportError($"failed to process .hdv file {hdvFileFull ?? ""} for vehicle {vehFileFull}.");
          continue;
        }

        var hdvEntryStr = $"{hdvEntry.HdvID},{hdvEntry.Version},{hdvEntry.StopGo},{hdvEntry.StopGoSimultaneous},{hdvEntry.Preparation},{hdvEntry.DRSCapable},{hdvEntry.VehicleWidth},{hdvEntry.BrakeResponseCurveFrontLeft},{hdvEntry.BrakeResponseCurveFrontRight},{hdvEntry.BrakeResponseCurveRearLeft},{hdvEntry.BrakeResponseCurveRearRight},{hdvEntry.TbcIDPrefix}";
        Utils.WriteLine($"HDV entry matched: \"{hdvEntryStr}\"", ConsoleColor.Magenta);

        string tbcFileFull = null;
        var tbcEntries = StatsBuilder.ProcessTbcFile(hdvEntry.TireBrand, vehDirFull, vehDir, hdvEntry.TbcIDPrefix, vehFileFull, out tbcFileFull);
        if (tbcEntries == null || tbcEntries.Count == 0)
        {
          Utils.ReportError($"failed to process .tbc file {tbcFileFull ?? ""} for vehicle {vehFileFull}.");
          continue;
        }

        bool failed = false;
        foreach (var e in tbcEntries)
        {
          var tbcEntryStr = $"{e.TbcID},{e.Version},{e.WetWeather},{e.FrontTgmID},{e.RearTgmID}";
          Utils.WriteLine($"TBC entry matched: \"{tbcEntryStr}\"", ConsoleColor.Magenta);

          string tgmFileFull = null;
          var tgmEntry = StatsBuilder.ProcessTgmFile(e.FrontTGM, vehDirFull, e.FrontTgmID, vehFileFull, out tgmFileFull);
          if (tgmEntry == null)
          {
            Utils.ReportError($"failed to process .tgm file {tgmFileFull ?? ""} for vehicle {vehFileFull}.");
            failed = true;
            break;
          }

          var tgmEntryStr = $"{tgmEntry.TgmID},{tgmEntry.Version},{tgmEntry.StaticCurve}";
          Utils.WriteLine($"TGM entry matched: \"{tgmEntryStr}\"", ConsoleColor.Magenta);
        }

        if (failed) continue;

        //if (tbcEntries == null || string.IsNullOrWhiteSpace(tbcEntries.TireBrand))
        //{
        //          Utils.ReportError($"failed to process hdv file {hdvFileFull ?? ""} for vehicle {vehFileFull}.", ConsoleColor.Red);
        //        continue;
        //    }


      }
    }

    private static VehEntry ProcessVehFile(string vehFileFull, string vehDir, out string hdvFile)
    {
      hdvFile = "";

      Utils.WriteLine($"\nProcessing .veh: {vehFileFull}", ConsoleColor.Cyan);

      var vehFileReader = new KindOfSortOfIniFile(vehFileFull);

      Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>> section = null;
      List<Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>>> sectionList = null;
      if (!vehFileReader.sectionsToKeysToValuesMap.TryGetValue("", out sectionList))
      {
        Utils.ReportError($"global section not found in file {vehFileFull}.");
        return null;
      }

      // Pick the first section.
      section = sectionList[0];

      string descr;
      if (!StatsBuilder.GetFirstSectionValue(vehFileFull, section, "Description", out descr, false /*optional*/))
        return null;

      string cat;
      if (!StatsBuilder.GetFirstSectionValue(vehFileFull, section, "Category", out cat, false /*optional*/))
        return null;

      var catExtracted = StatsBuilder.ExtractCategory(cat);
      if (string.IsNullOrWhiteSpace(catExtracted))
      {
        Utils.ReportError($"Failed to parse categroy out of {cat} in {vehFileFull}.");
        return null;
      }

      var descrExtracted = StatsBuilder.UnquoteString(descr);
      if (string.IsNullOrWhiteSpace(descrExtracted))
      {
        Utils.ReportError($"Failed to parse description out of {descr} in {vehFileFull}.");
        return null;
      }

      string hdvFileFull;
      if (!StatsBuilder.GetFirstSectionValue(vehFileFull, section, "HDVehicle", out hdvFileFull, false /*optional*/))
        return null;

      var lastSlash = hdvFileFull.LastIndexOf('\\');
      hdvFile = hdvFileFull;
      if (lastSlash != -1)
        hdvFile = hdvFileFull.Substring(lastSlash + 1);

      if (string.IsNullOrWhiteSpace(hdvFile))
      {
        Utils.ReportError($"Failed to parse .hdv file name out of {hdvFileFull} in {vehFileFull}.");
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
        Utils.ReportError($"failed to locate {hdvFile} for vehicle {vehFileFull}.");
        return null;
      }
      else if (hdvFiles.Length > 1)
        Utils.ReportWarning($"hdv file {hdvFile} is ambigous for vehicle {vehFileFull}.  Will use the first one: {hdvFiles[0]}.");

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
        Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>> section = null;
        List<Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>>> sectionList = null;
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("GENERAL", out sectionList))
        {
          Utils.ReportError($"[GENERAL] section not found in file {hdvFileFull}.");
          break;
        }

        // Pick the first section.
        section = sectionList[0];

        // TODO: tireBrand might depend on upgrade selected.
        string tireBrand = null;
        if (!StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "TireBrand", out tireBrand, false /*optional*/))
          break;

        tireBrand = tireBrand.ToLowerInvariant();

        // Some mods add .tbc, chop it off.
        if (tireBrand.ToUpperInvariant().EndsWith(".TBC"))
          tireBrand = tireBrand.Substring(0, tireBrand.Length - 4);

        //////////////////////////////////////////
        // [PITMENU] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("PITMENU", out sectionList))
        {
          Utils.ReportError($"[PITMENU] section not found in file {hdvFileFull}.");
          break;
        }

        // Pick the first section.
        section = sectionList[0];

        string stopGo = null;
        StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "StopGo", out stopGo, true /*optional*/, "1" /*default*/);

        string stopGoSimultaneous = null;
        StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "StopGoSimultaneous", out stopGoSimultaneous, true /*optional*/, "0" /*default*/);

        string preparation = null;
        if (!StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "Preparation", out preparation, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(preparation, out preparation))
          break;

        //////////////////////////////////////////
        // [REARWING] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("REARWING", out sectionList))
        {
          Utils.ReportError($"[REARWING] section not found in file {hdvFileFull}.");
          break;
        }

        // Pick the first section.
        section = sectionList[0];

        string DRSCapable = null;
        if (StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "FlapDrag", out DRSCapable, true /*optional*/)
          && StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "FlapLift", out DRSCapable, true /*optional*/)
          && StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "FlapTimes", out DRSCapable, true /*optional*/)
          && StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "FlapRules", out DRSCapable, true /*optional*/))
          DRSCapable = "1";
        else
          DRSCapable = "0";

        //////////////////////////////////////////
        // [BODYAERO] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("BODYAERO", out sectionList))
        {
          Utils.ReportError($"[BODYAERO] section not found in file {hdvFileFull}.");
          break;
        }

        // Pick the first section.
        section = sectionList[0];

        string vehicleWidth = null;
        StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "VehicleWidth", out vehicleWidth, true /*optional*/, "-1" /*defaultValue*/);

        //////////////////////////////////////////
        // [FRONTLEFT] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("FRONTLEFT", out sectionList))
        {
          Utils.ReportError($"[FRONTLEFT] section not found in file {hdvFileFull}.");
          break;
        }

        // Pick the first section.
        section = sectionList[0];

        string frontLeftBrakeCurve = null;
        if (!StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "BrakeResponseCurve", out frontLeftBrakeCurve, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(frontLeftBrakeCurve, out frontLeftBrakeCurve))
          break;

        //////////////////////////////////////////
        // [FRONTRIGHT] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("FRONTRIGHT", out sectionList))
        {
          Utils.ReportError($"[FRONTRIGHT] section not found in file {hdvFileFull}.");
          break;
        }

        // Pick the first section.
        section = sectionList[0];

        string frontRightBrakeCurve = null;
        if (!StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "BrakeResponseCurve", out frontRightBrakeCurve, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(frontRightBrakeCurve, out frontRightBrakeCurve))
          break;

        //////////////////////////////////////////
        // [REARLEFT] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("REARLEFT", out sectionList))
        {
          Utils.ReportError($"[REARLEFT] section not found in file {hdvFileFull}.");
          break;
        }

        // Pick the first section.
        section = sectionList[0];

        string rearLeftBrakeCurve = null;
        if (!StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "BrakeResponseCurve", out rearLeftBrakeCurve, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(rearLeftBrakeCurve, out rearLeftBrakeCurve))
          break;

        //////////////////////////////////////////
        // [REARRIGHT] section.
        //////////////////////////////////////////
        if (!hdvFileReader.sectionsToKeysToValuesMap.TryGetValue("REARRIGHT", out sectionList))
        {
          Utils.ReportError($"[REARRIGHT] section not found in file {hdvFileFull}.");
          break;
        }

        // Pick the first section.
        section = sectionList[0];

        string rearRightBrakeCurve = null;
        if (!StatsBuilder.GetFirstSectionValue(hdvFileFull, section, "BrakeResponseCurve", out rearRightBrakeCurve, false /*optional*/))
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
          BrakeResponseCurveFrontLeft = frontLeftBrakeCurve.Replace(" ", ""),
          BrakeResponseCurveFrontRight = frontRightBrakeCurve.Replace(" ", ""),
          BrakeResponseCurveRearLeft = rearLeftBrakeCurve.Replace(" ", ""),
          BrakeResponseCurveRearRight = rearRightBrakeCurve.Replace(" ", ""),
          TbcIDPrefix = $"tbc@@{vehDir}@@{tireBrand}@@".ToLowerInvariant(),
          TireBrand = tireBrand
        };
      }
      while (false);

      StatsBuilder.hdvResolvedMap.Add(hdvFileFull, hdvEntry);

      return hdvEntry;
    }

    private static Dictionary<string, List<TbcEntry>> tbcResolvedMap = new Dictionary<string, List<TbcEntry>>();

    private static List<TbcEntry> ProcessTbcFile(string tireBrand, string vehDirFull, string vehDir, string tbcIDPrefix, string vehFileFull, out string tbcFileFull)
    {
      tbcFileFull = null;
      var tbcFile = tireBrand + ".tbc";
      var tbcFiles = Directory.GetFiles(vehDirFull, tbcFile, SearchOption.AllDirectories);
      if (tbcFiles == null || tbcFiles.Length == 0)
      {
        Utils.ReportError($"failed to locate {tbcFile} for vehicle {vehFileFull}.");
        return null;
      }
      else if (tbcFiles.Length > 1)
        Utils.ReportWarning($"tbc file {tbcFile} is ambigous for vehicle {vehFileFull}.  Will use the first one: {tbcFiles[0]}.");

      tbcFileFull = tbcFiles[0];
      List<TbcEntry> tbcEntries = null;
      List<TbcEntry> tbcEntriesIntermediate = null;
      if (StatsBuilder.tbcResolvedMap.TryGetValue(tbcFileFull, out tbcEntries))
        return tbcEntries;

      bool failed = false;
      do
      {
        /*
         * Extracted stuff:
         * [COMPOUND]
         * Name="Soft"
         * WetWeather=1/0
         * FRONT: (subsection)
         * TGM="tgm file name"
         * REAR: (subsection)
         * TGM="tgm file name"
         */
        Utils.WriteLine($"\nProcessing .tbc: {tbcFileFull}", ConsoleColor.Cyan);

        var tbcFileReader = new KindOfSortOfIniFile(tbcFileFull);

        //Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>> section = null;
        List<Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>>> sectionList = null;
        if (!tbcFileReader.sectionsToKeysToValuesMap.TryGetValue("COMPOUND", out sectionList))
        {
          Utils.ReportError($"[COMPOUND] section not found in file {tbcFileFull}.");
          break;
        }

        var ver = new DirectoryInfo(tbcFileFull).Parent.Name;
        tbcEntriesIntermediate = new List<TbcEntry>();
        foreach (var s in sectionList)
        {
          string compoundName = null;
          if (!StatsBuilder.GetFirstSectionValue(tbcFileFull, s, "Name", out compoundName, false /*optional*/))
          {
            failed = true;
            break;
          }

          string isWetCompound = null;
          StatsBuilder.GetFirstSectionValue(tbcFileFull, s, "WetWeather", out isWetCompound, true /*optional*/, "0" /*defaultValue*/);

          List<KindOfSortOfIniFile.ValueInSubSection> values = null;
          if (!StatsBuilder.GetSectionValues(tbcFileFull, s, "TGM", out values, false /*optional*/))
          {
            failed = true;
            break;
          }

          if (values.Count != 2)
          {
            Utils.ReportError($"Unexpected number of TGM entires in {tbcFileFull}");
            failed = true;
            break;
          }

          string frontTgm = null;
          string rearTgm = null;
          foreach (var v in values)
          {
            // Some mods add .tgm, chop it off.
            var value = StatsBuilder.UnquoteString(v.Value);
            if (value.ToUpperInvariant().EndsWith(".TGM"))
              value = value.Substring(0, value.Length - 4);

            if (v.SubSection == "FRONT:")
            {
              frontTgm = value;
              continue;
            }
            else if (v.SubSection == "REAR:")
            {
              rearTgm = value;
              continue;
            }
            else if (v.SubSection == "ALL:")
            {
              frontTgm = rearTgm = value;
              continue;
            }

            failed = true;
            break;
          }

          if (frontTgm == null || rearTgm == null)
          {
            Utils.ReportError($"Couldn't figure out Front/Rear TGM entires in {tbcFileFull}");
            failed = true;
            break;
          }

          // All collected.  Form an entry.
          tbcEntriesIntermediate.Add(new TbcEntry()
            {
              TbcID = $"{tbcIDPrefix}@@{compoundName}".ToLowerInvariant(),
              Version = ver,
              WetWeather = isWetCompound,
              FrontTgmID = $"tgm@@{vehDir}@@{frontTgm}@@".ToLowerInvariant(),
              RearTgmID = $"tgm@@{vehDir}@@{rearTgm}@@".ToLowerInvariant(),

              // Internal (not part of entries).
              FrontTGM = frontTgm,
              RearTGM = rearTgm 
            }
          );
        }

        if (failed) break;

      } while (false);

      // Only use created entries if all of them succeeded.
      if (!failed)
      {
        Debug.Assert(tbcEntriesIntermediate != null);
        tbcEntries = tbcEntriesIntermediate;
      }

      StatsBuilder.tbcResolvedMap.Add(tbcFileFull, tbcEntries);

      return tbcEntries;
    }

    private static Dictionary<string, TgmEntry> tgmResolvedMap = new Dictionary<string, TgmEntry>();

    private static TgmEntry ProcessTgmFile(string tgmFile, string vehDirFull, string tgmID, string vehFileFull, out string tgmFileFull)
    {
      tgmFileFull = null;
      tgmFile = tgmFile + ".tgm";
      var tgmFiles = Directory.GetFiles(vehDirFull, tgmFile, SearchOption.AllDirectories);
      if (tgmFiles == null || tgmFiles.Length == 0)
      {
        Utils.ReportError($"failed to locate {tgmFile} for vehicle {vehFileFull}.");
        return null;
      }
      else if (tgmFiles.Length > 1)
        Utils.ReportWarning($".tgm file {tgmFile} is ambigous for vehicle {vehFileFull}.  Will use the first one: {tgmFiles[0]}.");

      tgmFileFull = tgmFiles[0];
      TgmEntry tgmEntry = null;
      if (StatsBuilder.tgmResolvedMap.TryGetValue(tgmFileFull, out tgmEntry))
        return tgmEntry;

      do
      {
        /*
         * Extracted stuff:
         * [Realtime]
         * StaticCurve=
         */
        Utils.WriteLine($"\nProcessing .tgm: {tgmFileFull}", ConsoleColor.Cyan);

        var tgmFileReader = new KindOfSortOfIniFile(tgmFileFull);

        List<Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>>> sectionList = null;
        if (!tgmFileReader.sectionsToKeysToValuesMap.TryGetValue("REALTIME", out sectionList))
        {
          Utils.ReportError($"[REALTIME] section not found in file {tgmFileFull}.");
          break;
        }

        var section = sectionList[0];

        var ver = new DirectoryInfo(tgmFileFull).Parent.Name;
        string staticCurve = null;
        if (!StatsBuilder.GetFirstSectionValue(tgmFileFull, section, "StaticCurve", out staticCurve, false /*optional*/))
          break;

        if (!StatsBuilder.RemoveParens(staticCurve, out staticCurve))
        {
          Utils.ReportError($"failed to remove parens on StaticCurve {staticCurve} file {tgmFileFull}");
          break;
        }

        tgmEntry = new TgmEntry()
        {
          TgmID = tgmID,
          Version = ver,
          StaticCurve = staticCurve.Replace(" ", "")
        };

      } while (false);

      StatsBuilder.tgmResolvedMap.Add(tgmFileFull, tgmEntry);

      return tgmEntry;
    }

    private static bool RemoveParens(string str, out string strOut)
    {
      if (!string.IsNullOrWhiteSpace(str) && str.StartsWith("(") && str.EndsWith(")"))
      {
        strOut = str.Substring(1, str.Length - 2);
        return true;
      }
      strOut = str;
      return false;
    }

    private static bool GetFirstSectionValue(string vehFileFull, Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>> section, string key, out string value, bool optional, string defaultValue = null)
    {
      List<KindOfSortOfIniFile.ValueInSubSection> thisKeyValues = null;
      if (!section.TryGetValue(key.ToUpperInvariant(), out thisKeyValues))
      {
        if (!optional)
          Utils.ReportError($"'{key}' key value not found in file {vehFileFull}.");

        value = defaultValue;
        return false;
      }

      Debug.Assert(thisKeyValues.Count > 0);
      value = thisKeyValues[0].Value;

      return true;
    }

    private static bool GetSectionValues(string vehFileFull, Dictionary<string, List<KindOfSortOfIniFile.ValueInSubSection>> section, string key, out List<KindOfSortOfIniFile.ValueInSubSection> values, bool optional)
    {
      if (!section.TryGetValue(key.ToUpperInvariant(), out values))
      {
        if (!optional)
          Utils.ReportError($"'{key}' key values not found in file {vehFileFull}.");

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
