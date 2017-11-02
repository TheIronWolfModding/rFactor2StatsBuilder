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
    internal Dictionary<string, Dictionary<string, string>> sectionsToKeysToValuesMap = new Dictionary<string, Dictionary<string, string>>();
    internal int numErrors = 0;

    internal KindOfSortOfIniFile(string fileFull)
    {
      var lines = File.ReadAllLines(fileFull);
      var lineCounter = 0;

      var currentSection = new Dictionary<string, string>();

      // Unnamed section.
      this.sectionsToKeysToValuesMap.Add("", currentSection);

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

          var sectionName = l.Substring(1, closingBkgIdx);

          // Ok, this is new section.  Create a new section:
          currentSection = new Dictionary<string, string>();
          this.sectionsToKeysToValuesMap.Add(sectionName, currentSection);

          continue;
        }
        else  // Key=value.
        {
          // TODO: deal with 
          // Sponsor = "civic_btcc_1EXTRA7.dds" SponsorTexStage = 3
          l = this.SanitizeKeyValuePair(l);
          var keyValue = l.Split('=');
          if (keyValue.Length == 0 || keyValue.Length > 2)
          {
            this.ReportWarning($"ignoring unrecognized key value pair statement {l}.  Authoring error really, please fix.", fileFull, lineCounter);
            continue;
          }

          if (currentSection.ContainsKey(keyValue[0]))
          {
            this.ReportWarning($"already encountered {keyValue[0]}", fileFull, lineCounter);
            continue;
          }

          currentSection.Add(keyValue[0], keyValue[1]);
          continue;
        }
      }
    }

    private string SanitizeKeyValuePair(string keyValue)
    {
      var commentIdx = keyValue.IndexOf("//");
      if (commentIdx != -1)
        return keyValue.Substring(0, commentIdx).TrimEnd();

      return keyValue;
    }

    private void ReportError(string msg, string fileFull, int line)
    {
      Utils.WriteLine($"Error: {msg}.  File: {fileFull} Line: {line}", ConsoleColor.Red);
      ++this.numErrors;
    }

    private void ReportWarning(string msg, string fileFull, int line)
    {
      Utils.WriteLine($"Warning: {msg}.  File: {fileFull} Line: {line}", ConsoleColor.Yellow);
    }

  }
}
