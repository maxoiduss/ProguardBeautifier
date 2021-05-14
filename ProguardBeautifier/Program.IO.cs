using System;
using System.IO;
using System.Collections.Generic;

namespace ProguardBeautifier
{
	public partial class MainClass
	{
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
}
