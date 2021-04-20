using System;
using System.IO;
using ConfigParser;

namespace ParserTest {
	class Program {
		public static readonly string CONFIG_PATH = Path.Combine("..", "..", "..",
			"config.cfg");
		public static readonly string CONFIG2_PATH = Path.Combine("..", "..", "..",
			"config2.cfg");
		public static readonly string INVALID_CONFIG_PATH = Path.Combine("..", "..", "..",
			"invalid.cfg");
		
		static void Main(string[] args) {
			if (File.Exists(CONFIG2_PATH)) File.Delete(CONFIG2_PATH);
			File.Copy(CONFIG_PATH, CONFIG2_PATH);

			FileStream file = File.Open(CONFIG2_PATH, FileMode.OpenOrCreate);
			Config config = new Config(file);
			try { config.Load(); }
			catch (FormatException ex) { Console.WriteLine(ex.ToString()); }
			finally { file.Position = 0; }

			Console.WriteLine($"{file.Name}:");
			Console.WriteLine(new StreamReader(file).ReadToEnd());
			Console.WriteLine("==================================");

			config["pc_health"] = " 25 ";
			config["zombie_health"] = "35";
			config["offline"] = "false #dd";
			config["pc_mana"] = null;
			config.Delete("pc_health");
			config.Delete("pc_mana");
			Console.WriteLine("Config:");
			foreach((string setting, string value) in config) {
				Console.WriteLine($"{setting} => '{value}'");
			}
			Console.WriteLine("==================================");

			config.Save();
			file.Position = 0;
			Console.WriteLine($"{file.Name}:");
			Console.WriteLine(new StreamReader(file).ReadToEnd());
			Console.WriteLine("==================================");
		}

		void example() {
			Config cfg = new Config(File.Open("config.cfg", FileMode.OpenOrCreate));
			cfg.Load();

			if (cfg["github"] == "true") {
				string link = cfg["link"];
				//Do something with the link
			}

			cfg["github"] = "false";
			cfg["link"] = "";
			cfg["colour"] = "Red";

			cfg.Delete("pet");

			cfg.Save();
		}
	}
}
