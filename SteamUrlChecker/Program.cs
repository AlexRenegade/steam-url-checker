using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;

namespace SteamUrlChecker
{
	class Program
	{
		static async Task Main()
		{
			var strings = await File.ReadAllLinesAsync("strings.txt");
			var smallerLists = SplitList(strings.ToList(), strings.Length / 30);
			var tasks = new List<Task<Result>>();

			foreach (var smallerList in smallerLists)
			{
				foreach (var s in smallerList)
				{
					tasks.Add(CheckString(s));
				}
			}

			await Task.WhenAll(tasks);

			Console.WriteLine("\nChecking complete!");
			Console.WriteLine($"{tasks.Count(task => task.Result.Unclaimed)} out of {tasks.Count} were unclaimed.");
			await File.WriteAllLinesAsync("unclaimed.txt",
				tasks.Where(task => task.Result.Unclaimed).Select(task => task.Result.InputString).ToList());
			await File.WriteAllLinesAsync("claimed.txt",
				tasks.Where(task => !task.Result.Unclaimed).Select(task => task.Result.InputString).ToList());

			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
		}

		private static async Task<Result> CheckString(string s)
		{
			var client =
				new RestClient(
					$"https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/?key={SteamCredentials.ApiKey}&vanityurl={s}");
			var request = new RestRequest(Method.GET);
			var response = await client.ExecuteAsync(request);

			if (response.Content.Contains("No match"))
			{
				Console.WriteLine(s + " :: Unclaimed");
				return new Result(s, true);
			}

			Console.WriteLine(s + " :: Claimed");
			return new Result(s, false);
		}

		private static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
		{
			for (var i = 0; i < locations.Count; i += nSize)
			{
				yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
			}
		}
	}

	public class Result
	{
		public string InputString { get; }
		public bool Unclaimed { get; }

		public Result(string inputString, bool unclaimed)
		{
			InputString = inputString;
			Unclaimed = unclaimed;
		}
	}
}
