using System;
using System.Linq;
using System.Collections.Generic;
using SSO = System.StringSplitOptions;

namespace ProguardBeautifier
{
	using StringAndStringsPair = KeyValuePair<string, List<string>>;

	public static partial class Beautifier
	{
		public static Action<IEnumerable<string>> WriteFileAction { get; set; }

		public static void PrintProblemLineDetailsAndExitIfNeeded(int lineIndex, bool needToExit = false, int exitCode = 1)
		{
			Console.WriteLine($"Problem started at {lineIndex} string. Take a look.");

			if (needToExit) Environment.Exit(exitCode);
		}

		public static void BeautifyProguard(IEnumerable<string> proguardFile)
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

			// output creation
			List<string> output = result.SelectMany(segm =>
			{
				List<string> list = new();
				string Opened = '{'.ToString(), Closed = '}'.ToString();

				if (segm.Value.Count > 1)
				{
					list.Add(segm.Key);
					list.Add(Opened);
					list.AddRange(segm.Value.Select(s => $"{Consts.Tab}{s.Trim()}"));
					list.Add(Closed);
				}
				else if (segm.Value.Count == 1)
				{
					list.Add($"{segm.Key} {Opened} {segm.Value.First()} {Closed}");
				}
				else list.Add(segm.Key);

				return list;
			}).ToList();
			output.InsertEmptyStringsBeforeComments();

			// finally we can write output data to file
			WriteFileAction(output);

			void populateSegmentsOnlyByString(char? checkForChar = null)
			{
				if (checkForChar != null)
				{
					if (file[i].Count(c => c == checkForChar) > 1)
					{
						PrintProblemLineDetailsAndExitIfNeeded(i++);
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
					PrintProblemLineDetailsAndExitIfNeeded(start, true);
				}
			}
		}
	}
}
