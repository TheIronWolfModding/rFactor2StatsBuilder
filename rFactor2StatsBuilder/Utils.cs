/*

Author: The Iron Wolf (vleonavicius@hotmail.com)
Website: thecrewchief.org
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rFactor2StatsBuilder
{
  static class Utils
  {
    internal static void ReportException(string msg, Exception ex)
    {
      Utils.WriteLine($"{msg} Exception: {ex.GetType().ToString()} at: {ex.StackTrace} {(ex.InnerException != null ? $"inner: {ex.InnerException.StackTrace}" : "")} ", ConsoleColor.Red);
    }

    internal static void ReportError(string msg)
    {
      var errMsg = $"Error: {msg}";
      Utils.WriteLine(errMsg, ConsoleColor.Red);

      rFactor2StatsBuilder.errors.Add(errMsg);
    }

    internal static void ReportWarning(string msg)
    {
      var warnMsg = $"Warning: {msg}";
      Utils.WriteLine(warnMsg, ConsoleColor.Yellow);

      rFactor2StatsBuilder.warnings.Add(warnMsg);
    }

    internal static void WriteLine(string str, ConsoleColor color = ConsoleColor.White)
    {
      var oldColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.WriteLine(str);
      Console.ForegroundColor = oldColor;
    }
  }
}
