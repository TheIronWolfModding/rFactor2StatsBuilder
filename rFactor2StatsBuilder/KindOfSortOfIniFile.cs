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
  class KindOfSortOfIniFile
  {
    // Maps section name to multiple section key-value maps.  Keys can be duplicated, so values are a list too.
    //                  section name -> section -> key -> values.
    internal Dictionary<string, List<Dictionary<string, List<string>>>> sectionsToKeysToValuesMap = new Dictionary<string, List<Dictionary<string, List<string>>>>();
    internal string currentSectionNameUpper = null;

    internal KindOfSortOfIniFile(string fileFull)
    {
      var lines = File.ReadAllLines(fileFull);
      var lineCounter = 0;

      var currentSection = new Dictionary<string, List<string>>();
      var currentSectionList = new List<Dictionary<string, List<string>>>();
      currentSectionList.Add(currentSection);

      // Unnamed section.
      this.sectionsToKeysToValuesMap.Add("", currentSectionList);

      foreach (var line in lines)
      {
        ++lineCounter;
        var l = line.Trim();
        if (string.IsNullOrWhiteSpace(l))  // Empty lines
          continue;
        else if (l.StartsWith("/"))  // Comment
          continue;
        else if (l.StartsWith("["))  // New section
        {
          var closingBkgIdx = l.IndexOf("]");
          if (closingBkgIdx == -1)
          {
            this.ReportError("closing ']' not found", fileFull, lineCounter);
            continue;
          }
          else if (closingBkgIdx == 1)
          {
            this.ReportError("empty section found", fileFull, lineCounter);
            continue;
          }

          var sectionName = l.Substring(1, closingBkgIdx - 1);

          // Ok, this is new section.  Create a new section:
          currentSection = new Dictionary<string, List<string>>();
          this.currentSectionNameUpper = sectionName.ToUpperInvariant();

          // Some sections may appear multiple times (COMPOUND).  So keep a list.
          if (this.sectionsToKeysToValuesMap.ContainsKey(currentSectionNameUpper))
            currentSectionList = this.sectionsToKeysToValuesMap[currentSectionNameUpper];
          else
          {
            // New unique section.
            currentSectionList = new List<Dictionary<string, List<string>>>();
            this.sectionsToKeysToValuesMap.Add(currentSectionNameUpper, currentSectionList);
          }

          // Add new section instance to the new or existing list.
          currentSectionList.Add(currentSection);

          continue;
        }
        else  // Key=value.
        {
          if (this.currentSectionNameUpper == "SLIPCURVE")
            continue;

          l = this.SanitizeKeyValuePair(l);
          if (l == "FRONT:" || l == "REAR:")
            continue;

          if (!l.Contains("="))
          {
            this.ReportWarning($"ignoring unrecognized key value pair statement \"{l}\".", fileFull, lineCounter);
            continue;
          }

          var keyValue = l.Split('=');

          // Drop Special keys (not needed yet, need special parsing).
          if (keyValue[0].EndsWith("Special"))
            continue;

          if (keyValue.Length == 0 || keyValue.Length > 2)
          {
            this.ReportWarning($"ignoring unrecognized key value pair statement \"{l}\".", fileFull, lineCounter);
            continue;
          }

          var keyTrimmed = keyValue[0].Trim();
          var keyUpper = keyTrimmed.ToUpperInvariant();
          var valueTrimmed = keyValue[1];
          List<string> thisKeyValues = null;
          if (!currentSection.TryGetValue(keyUpper, out thisKeyValues))
          {
            thisKeyValues = new List<string>();
            currentSection.Add(keyUpper, thisKeyValues);
          }

          thisKeyValues.Add(valueTrimmed);

          continue;
        }
      }
    }

    private string SanitizeKeyValuePair(string keyValue)
    {
      var commentIdx = keyValue.IndexOf("//");
      if (commentIdx != -1)
        return keyValue.Substring(0, commentIdx).TrimEnd();

      return keyValue.Trim();
    }

    private void ReportError(string msg, string fileFull, int line)
    {
      Utils.ReportError($"{msg}.  File: {fileFull} Line: {line}");
    }

    private void ReportWarning(string msg, string fileFull, int line)
    {
      Utils.ReportWarning($"{msg}.  File: {fileFull} Line: {line}");
    }

  }
}
