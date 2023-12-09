using Stardust.Client;
using Stardust.Server;

namespace Stardust {
	public class Stardust {
		private static readonly KeyValuePair<string, string[]>[]
			AllowedClientArgs = { new(key: "-test", value: new[] { "1234" }) },
			AllowedServerArgs = { new(key: "-test", value: new[] { "4321" }) };

		public static void Main(params string[] args) {
			var pairs = args.Where(predicate: (_, index) => (index + 1) < args.Length)
							.Select(selector: (arg, index) =>
										new KeyValuePair<string, string>(key: arg, value: args[index + 1]))
							.ToDictionary(keySelector: kvp => kvp.Key, elementSelector: kvp => kvp.Value);

			if (pairs[key: "-env"].Equals(value: "server"))
				StardustServer.Run(args: pairs
										 .Where(predicate: pair => AllowedServerArgs.Any(predicate: pairS =>
													pair.Key.Equals(value: pairS.Key) &&
													pairS.Value.Any(predicate: value => pair.Value
																		.Equals(value: value))))
										 .ToDictionary());
			else
				StardustClient.Run(args: pairs
										 .Where(predicate: pair => AllowedClientArgs.Any(predicate: pairS =>
													pair.Key.Equals(value: pairS.Key) &&
													pairS.Value.Any(predicate: value => pair.Value
																		.Equals(value: value))))
										 .ToDictionary());
		}
	}
}