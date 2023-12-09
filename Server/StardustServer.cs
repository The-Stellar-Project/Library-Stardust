namespace Stardust.Server {
	public class StardustServer {
		public static void Run(in Dictionary<string, string> args) { // hi
			Console.WriteLine(value: "server detected");
			foreach (var pair in args) Console.WriteLine(value: $"key: {pair.Key} ; value: {pair.Value}");
		}
	}
}