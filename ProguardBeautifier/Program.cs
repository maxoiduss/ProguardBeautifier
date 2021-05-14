using System;
using System.Collections.Generic;

namespace ProguardBeautifier
{
	public static class Consts
	{
		public const char Start = '-';
		public const char End = ';';
		public const char Comms = '#';
		public const string ErrorMessage = "The file is corrupted or structure is wrong.";
	}

	public partial class MainClass
	{
		public static void Main()
		{
			Console.WriteLine(" Enter full file path with extension ");
			ReadFile(Console.ReadLine(), out IEnumerable<string> file);
			Beautifier.BeautifyProguard(file);
			Console.WriteLine(" DONE ");
		}
	}
}
