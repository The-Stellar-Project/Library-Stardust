namespace Stardust.Client {
	public class StardustClient {
		public static void Run(in Dictionary<string, string> args) {
			Console.WriteLine(value: "client detected");
			foreach (var pair in args) Console.WriteLine(value: $"key: {pair.Key} ; value: {pair.Value}");
		}
	}
}