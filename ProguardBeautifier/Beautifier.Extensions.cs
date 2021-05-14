using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SSO = System.StringSplitOptions;

namespace ProguardBeautifier
{
	using StringAndStringsPair = KeyValuePair<string, List<string>>;

	public static partial class Beautifier
	{
		#region @private extensions

		static void PrintProblemLineDetailsAndExitIfNeeded(this int lineIndex, bool needToExit = false, int exitCode = 1)
		{
			Console.WriteLine($"Problem started at {lineIndex} string. Take a look.");

			if (needToExit) Environment.Exit(exitCode);
		}

		static int GetClosedStateCharIndex(this string str, int state)
		{
			var index = 0;
			str.ToCharArray().ToList().ForEach(c =>
			{
				if (c == '{') { --state; }
				if (c == '}') { ++state; }
				if (state == 0) { return; }

				++index;
			});

			return index;
		}

		static List<string> GetSegmentContent(this IEnumerable<string> source)
		{
			return string.Join(" ", source)
							.RemoveMultipleSpacesInARow()
							.Split(Consts.End.AsArray(), SSO.RemoveEmptyEntries)
							.Select(str => str + Consts.End)
							.ToList();
		}

		static Dictionary<string, List<string>> GetUnitedIdenticalSegments(this List<StringAndStringsPair> segments)
		{
			if (!segments.Any()) return null;

			var unitedSegments = new Dictionary<string, List<string>>();
			foreach (var segment in segments)
			{
				try { unitedSegments.Add(segment.Key, segment.Value); }
				catch (ArgumentException)
				{
					unitedSegments[segment.Key].Union(segment.Value);
				}
			}
			return unitedSegments;
		}
		#endregion @private extensions

		public static void ShiftStringToNextLineAfter(this List<string> file, int indexInString, int currentLineIndex)
		{
			var nextStr = file[currentLineIndex].Substring(indexInString + 1);
			var thisStr = file[currentLineIndex].Remove(indexInString + 1, file[currentLineIndex].Length - indexInString - 1);

			file[currentLineIndex] = thisStr;
			file.Insert(currentLineIndex + 1, nextStr);
		}

		public static IEnumerable<string> RemoveEmptyStrings(this IEnumerable<string> strs)
		{
			return strs.Where(s => !string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s))
					   .ToList();
		}

		public static string RemoveMultipleSpacesInARow(this string str)
		{
			var strs = str.Split(' '.AsArray(), SSO.RemoveEmptyEntries)
						  .Where(s => !string.IsNullOrWhiteSpace(s));
			return string.Join(" ", strs);
		}

		public static string RemoveWhiteSpaces(this string input) => Regex.Replace(input, @"\s+", "");

		public static char[] AsArray(this char c) => new char[] { c };
	}
}
