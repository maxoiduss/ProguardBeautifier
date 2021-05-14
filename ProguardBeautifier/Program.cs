using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SSO = System.StringSplitOptions;

namespace ProguardBeautifier
{
	using StringAndStringsPair = KeyValuePair<string, List<string>>;

	public static class Consts
	{
		public const char Start = '-';
		public const char End = ';';
		public const char Comms = '#';
		public const string ErrorMessage = "The file is corrupted or structure is wrong.";
	}

	class MainClass
	{
		public static void Main()
		{
			Console.WriteLine(" Enter full file path with extension ");
			ReadFile(Console.ReadLine(), out IEnumerable<string> file);
			Beatufier.BeatufyProguard(file);
			Console.WriteLine(" DONE ");
		}

		public static void ReadFile(string filePath, out IEnumerable<string> file)
		{
			file = new List<string>();
			try
			{
				using StreamReader sr = new(filePath);
				string line = null;

				while ((line = sr.ReadLine()) != null)
				{
					(file as List<string>).Add(line);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ERROR ---->>>>> {ex.Message}");
				ReadFile(filePath, out file);
			}

			Console.WriteLine("\n");
		}
	}

	public static class Beatufier
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

		public static void BeatufyProguard(IEnumerable<string> proguardFile)
		{
			List<string> file = new(proguardFile);
			List<StringAndStringsPair> Segments = new();
			int i = 0;

			for (; i < file.Count; ++i)
			{
				if (string.IsNullOrEmpty(file[i])) continue;

				if (file[i].Trim()[0] == Consts.Comms)
				{	// comments
					populateSegmentsOnlyByString();
				}
				else if (file[i].Contains('{'))
				{
					file.ShiftStringToNextLineAfter(file[i].IndexOf('{'), i);
					populateSegments();
				}
				else if (file[i].Trim()[0] == Consts.Start)
				{	// ordinary proguard rule string
					populateSegmentsOnlyByString(checkForChar: Consts.Start);
				}
			}
			// save full headers for the future
			Dictionary<string, string> headers = new();
			Segments.ForEach(seg =>
			{
				try { headers.Add(seg.Key.RemoveWhiteSpaces(), seg.Key); }
				catch (Exception) { }
			});
			List<StringAndStringsPair> segments =
				Segments.Select(s => new StringAndStringsPair(s.Key.RemoveWhiteSpaces(), s.Value))
						.ToList();

			// finally create the dictionary with no duplicates
			Dictionary<string, List<string>> result =
				segments.GetUnitedIdenticalSegments()
						.ToDictionary(keySelector: pair => headers[pair.Key], elementSelector: pair => pair.Value);

			void populateSegmentsOnlyByString(char? checkForChar = null)
			{
				if (checkForChar != null)
				{
					if (file[i].Count(c => c == checkForChar) > 1)
					{
						(i++).PrintProblemLineDetailsAndExitIfNeeded();
					}
				}
				Segments.Add(new StringAndStringsPair(file[i].TrimStart(' '.AsArray()), new List<string>()));
			}

			void populateSegments()
			{
				var start = i;
				var state = 0;
				var segment = new List<string>();

				try
				{
					do // we use state-machine in that decrease state on { and increase on }
					{
						var opened = file[i].Count(c => c == '{');
						var closed = file[i].Count(c => c == '}');
						state -= opened;
						state += closed;

						if (state > 0)
						{
							throw new OverflowException($"You have odd close bracket or another problem at {i} line.");
						}

						if (opened > 0 && closed > 0)
						{
							if ((state < 0 && file[i].LastIndexOf('{') > file[i].LastIndexOf('}')) ||
								(state == 0 && file[i].IndexOf('{') > file[i].IndexOf('}')))
							{	// here we get the point where new segment begins, set state 0 and exit loop
								var segmentStart = file[i].GetClosedStateCharIndex(state) + 1;
								file.ShiftStringToNextLineAfter(segmentStart, i);

								state = 0;
							}
						}

						if (state < 0) ++i;
					}
					while (state != 0);

					// here we can fullfill segment contents
					var startLetter = '{';
					var startStringParts = file[start].Split(startLetter.AsArray(), SSO.RemoveEmptyEntries).ToList();
					var header = startStringParts.First().RemoveMultipleSpacesInARow();
					startStringParts.RemoveAt(0); // removing header, after that we can populate segment
					segment.Add(string.Join(startLetter.ToString(), startStringParts).RemoveMultipleSpacesInARow());

					if (start < i - 1)
					{
						segment.AddRange(file.GetRange(start + 1, i - start - 1).Select(seg => seg.Trim()));
					}
					if (start < i) // the last line in segment
					{
						segment.Add(file[i].Substring(0, file[i].LastIndexOf('}')));
					}
					Segments.Add(new StringAndStringsPair(header, segment.RemoveEmptyStrings().GetSegmentContent()));
				}
				catch (Exception ex)
				{
					Console.WriteLine($" {Consts.ErrorMessage}\nStacktrace: {ex.StackTrace}");
					start.PrintProblemLineDetailsAndExitIfNeeded(true);
				}
			}
		}
	}
}
