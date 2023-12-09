using Stardust.Client.Render;

namespace Stardust.Client {
	public class StardustClient {
		public static bool Running = true;

		public static void Run(in Dictionary<string, string> args) {
			Console.WriteLine(value: "client detected");
			foreach (var pair in args) Console.WriteLine(value: $"key: {pair.Key} ; value: {pair.Value}");

			var window = new Window(title: "Stardust Example",
									width: 1600,
									height: 900,
									fullScreen: false,
									allowHighDpi: false);
			var renderContext = new RenderContext(window);

			window.Run();

			while (Running) ;
			// render context dispose here :D
			window.Dispose();
		}
	}
}