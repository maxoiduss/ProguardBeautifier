using System;
using System.Collections.Generic;
using String = System.String;

namespace ProguardBeautifier
{
	public static class Consts
	{
		public const char Start = '-';
		public const char End = ';';
		public const char Comms = '#';
		public const string ErrorMessage = "The file is corrupted or structure is wrong.";
		public const string FilenamePostfix = "-generated";
	}

	public partial class MainClass
	{
		public static void Main()
		{
			Console.WriteLine(" Enter full file path with extension ");
			String path = Console.ReadLine();
			ReadFile(path, out IEnumerable<string> file);
			Beautifier.WriteFileAction = (contents) => contents.WriteToFile(path);
			Beautifier.BeautifyProguard(file);
			Console.WriteLine(" DONE ");
		}
	}
}
