using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConfigParser {
	///<summary>Represents a config/settings file.</summary>
	public interface IConfig : IEnumerable<(string, string)>, IDisposable {
		///<summary>The value of a specified setting.</summary>
		///<param name="setting">The name of the setting.</param>
		///<returns>The value of the setting.</returns>
		string this[string setting] { get; set; }

		///<summary>The character that represents the start of a comment.</summary>
		char CommentChar { get; }
		///<summary>The character that represents the start of a value.</summary>
		char SeparatorChar { get; }

		///<summary>Delete a setting from the config file and object.</summary>
		///<param name="setting">The setting to delete.</param>
		void Delete(string setting);

		///<summary>Save the settings from the object to the config file.</summary>
		void Save();
		///<summary>Load the settings from the config file to the object.</summary>
		void Load();
	}

	///<inheritdoc cref="IConfig"/>
	///<remarks>Because Save() and Delete() use a StringBuilder as a buffer, files above 2GB
	/// will crash the program. Load *should* work fine though.</remarks>
	public class Config : IConfig {
		///<inheritdoc cref="CommentChar"/>
		protected char comment;
		///<inheritdoc cref="SeparatorChar"/>
		protected char separator;
		///<summary>The stream with the settings.</summary>
		protected Stream stream;
		///<summary>The settings in the config, keyed by their name.</summary>
		protected Dictionary<string, string> settings;
		///<summary>A function that checks wheter a char is legal as a settings name.</summary>
		protected Func<char, bool> charAccepted;

		///<inheritdoc cref="IConfig[string]"/>
		///<exception cref="KeyNotFoundException">The property is retrieved and key does
		/// not exist in the collection.</exception>
		///<exception cref="ArgumentException">Setting name is empty or null.</exception>
		///<exception cref="FormatException">Setting name is illegal(whitespace, comment,
		/// or non accepted characters)</exception>
		public virtual string this[string setting] {
			get {
				//Check validity
				if (string.IsNullOrEmpty(setting))
					throw new ArgumentException("Setting name is null or empty", "setting");
				foreach(char c in setting) {
					if (char.IsWhiteSpace(c)) throw new FormatException("Space in " +
						$"the middle of the setting name: '{setting}'");
					if (c == comment) throw new FormatException("Comment char" +
						$"('{comment}') in the middle of the setting name: '{setting}'");
					if (!charAccepted(c)) throw new FormatException("Illegal " +
						 $"char '{c}' in the setting name: '{setting}'");
				}
				return settings[setting];
			} set {
				//Check validity
				if (string.IsNullOrEmpty(setting))
					throw new ArgumentException("Setting name is null or empty", "setting");
				foreach (char c in setting) {
					if (char.IsWhiteSpace(c)) throw new FormatException("Space in " +
						$"the middle of the setting name: '{setting}'");
					if (c == comment) throw new FormatException("Comment char" +
						$"('{comment}') in the middle of the setting name: '{setting}'");
					if (!charAccepted(c)) throw new FormatException("Illegal " +
						 $"char '{c}' in the setting name: '{setting}'");
				}
				//Strip the value of comment and leading and trailing whitespace
				if (value is null) value = string.Empty;
				int start = 0, length = 0;
				while (start < value.Length && char.IsWhiteSpace(value[start])) start++;
				int spaces = 0;
				while (start + length < value.Length) {
					char cur = value[start + length];
					if (cur == comment) break;
					
					//Count spaces after the value
					if (char.IsWhiteSpace(cur)) spaces++;
					else spaces = 0;

					length++;
				}
				settings[setting] = value.Substring(start, length - spaces);
			}
		}

		public virtual char CommentChar => comment;
		public virtual char SeparatorChar => separator;

		///<summary>Initializes a new config with a stream, a setting/value separator, and 
		/// a comment char.</summary>
		///<param name="stream">The stream of the config.</param>
		///<param name="separator">The char that separates between name and value.</param>
		///<param name="comment">The char that represents the start of a comment.</param>
		///<param name="charAccepted">A function that checks whether a char is legal as a
		/// settings name. The default(null) is the <see cref="DefaultCharAccepted"/>.</param>
		///<exception cref="ArgumentNullException">Stream is null.</exception>
		public Config(Stream stream, char separator = '=', char comment = '#', 
				Func<char, bool> charAccepted = null) {
			if (stream == null) throw new ArgumentNullException("'stream' cannot be null!");
			this.stream = stream;
			settings = new Dictionary<string, string>();
			this.separator = separator;
			this.comment = comment;
			this.charAccepted = charAccepted is null ? DefaultCharAccepted : charAccepted;
		}

		///<inheritdoc cref="IConfig.Delete(string)"/>
		///<remarks>After the method returns, the position of the stream is set to 0.
		/// Unless an exception is thrown.</remarks>
		///<exception cref="ArgumentException">Setting is empty or null.</exception>
		///<exception cref="FormatException">Setting name is illegal(whitespace, comment,
		/// or non accepted characters)</exception>
		///<exception cref="IOException">An I/O error occurs.</exception>
		///<exception cref="NotSupportedException">The stream does not support seeking.</exception>
		///<exception cref="ObjectDisposedException">Methods were called after the stream
		/// was closed.</exception>
		///<exception cref="OutOfMemoryException">There is insufficient memory to
		/// allocate a buffer for the returned string.</exception>
		public virtual void Delete(string setting) {
			//Check validity
			if (string.IsNullOrEmpty(setting))
				throw new ArgumentException("Setting name is null or empty", "setting");
			foreach (char c in setting) {
				if (char.IsWhiteSpace(c)) throw new FormatException("Space in " +
					$"the middle of the setting name: '{setting}'");
				if (c == comment) throw new FormatException("Comment char" +
					$"('{comment}') in the middle of the setting name: '{setting}'");
				if (!charAccepted(c)) throw new FormatException("Illegal " +
					 $"char '{c}' in the setting name: '{setting}'");
			}

			//Go to the start of the stream
			stream.Position = 0;
			StreamReader reader = new StreamReader(stream);
			StringBuilder lines = new StringBuilder((int)stream.Length);
			string line;

			//Read the stream line by lines
			while(!((line = reader.ReadLine()) is null)) {
				int index = 0;
				//Skip leading whitespace
				while (index < line.Length && char.IsWhiteSpace(line[index])) index++;
				//Empty line
				if (index >= line.Length) {
					lines.AppendLine(line);
					continue;
				}
				//Comment line
				if (line[index] == comment) {
					lines.AppendLine(line);
					continue;
				}

				//Compare setting line with setting name
				for (int i = 0; i < setting.Length && index < line.Length; i++, index++) {
					if (line[index] == setting[i]) continue;

					while (index < line.Length) index++;
				}

				//Skip whitespace after setting name
				while (index < line.Length && char.IsWhiteSpace(line[index])) index++;

				//Check for separator, AppendLine from for loop happens here
				if (index >= line.Length || line[index] != separator) {
					lines.AppendLine(line);
				}

				//Delete setting
				settings.Remove(setting);
				//line is not appended to 'lines'
			}

			//Write all the lines back to the file
			stream.SetLength(0);
			stream.Position = 0;
			StreamWriter writer = new StreamWriter(stream);
			for (int i = 0; i < lines.Length; i++) writer.Write(lines[i]);
			writer.Flush();
			stream.Position = 0;
		}

		///<inheritdoc cref="IConfig.Save"/>
		///<remarks>After the method returns, the position of the stream is set to 0.
		/// Unless an exception is thrown.</remarks>
		///<exception cref="IOException">An I/O error occurs.</exception>
		///<exception cref="NotSupportedException">The stream does not support seeking.</exception>
		///<exception cref="ObjectDisposedException">Methods were called after the stream
		/// was closed.</exception>
		///<exception cref="OutOfMemoryException">There is insufficient memory to
		/// allocate a buffer for the returned string.</exception>
		public virtual void Save() {
			stream.Position = 0;

			HashSet<string> found = new HashSet<string>();
			StringBuilder lines = new StringBuilder((int)stream.Length);

			//Read the stream line by line
			StreamReader reader = new StreamReader(stream);
			string line;
			while (!((line = reader.ReadLine()) is null)) {
				int index = 0;
				char cur;
				//Skip white space
				while (index < line.Length && char.IsWhiteSpace(line[index])) {
					lines.Append(line[index]);
					index++;
				}
				//Skip empty lines
				if (index >= line.Length) {
					lines.AppendLine();
					continue;
				}
				//Skip comments
				cur = line[index];
				if (cur == comment) {
					while (index < line.Length) lines.Append(line[index++]);
					lines.AppendLine();
					continue;
				}

				//Read the name of the setting
				StringBuilder builder = new StringBuilder();
				bool space = false;
				while ((cur = line[index++]) != separator) {
					if (cur == comment) {
						lines.Append(cur);
						while (index < line.Length) lines.Append(line[index++]);
						lines.AppendLine();
						goto nestedBreak;
					}
					if (char.IsWhiteSpace(cur)) {
						lines.Append(cur);
						space = true;
						continue;
					}
					if (!charAccepted(cur)) {
						lines.Append(cur);
						while (index < line.Length) lines.Append(line[index++]);
						lines.AppendLine();
						goto nestedBreak;
					}

					//The error isn't because of the whitespace, it is because there is
					//a non-whitespace, valid, non separator character after the white space
					if (space) {
						lines.Append(cur);
						while (index < line.Length) lines.Append(line[index++]);
						lines.AppendLine();
						goto nestedBreak;
					}

					builder.Append(cur);
					lines.Append(cur);
					if (index >= line.Length) {
						lines.AppendLine();
						goto nestedBreak;
					}
				}
				lines.Append(cur);
				string setting = builder.ToString();

				if (string.IsNullOrEmpty(setting) || !settings.ContainsKey(setting)) {
					while (index < line.Length) lines.Append(line[index++]);
					lines.AppendLine();
					break;
				}

				//Read the setting value
				builder.Clear();
				while (index < line.Length && char.IsWhiteSpace(line[index]))
					lines.Append(line[index++]);
				while (index < line.Length) {
					cur = line[index];
					if (cur == comment) break;

					if (char.IsWhiteSpace(cur)) builder.Append(cur);
					else builder.Clear();

					index++;
				}

				lines.Append(settings[setting]);
				if (builder.Length > 0) lines.Append(builder.ToString());
				while (index < line.Length) lines.Append(line[index++]); //comments
				lines.AppendLine();

				//Add the setting to the collection
				found.Add(setting);
nestedBreak:;
			}//end while(line)

			//Write the modified lines to the file
			stream.SetLength(0);
			stream.Position = 0;
			StreamWriter writer = new StreamWriter(stream);
			//writer.Write(lines.ToString());
			for (int i = 0; i < lines.Length; i++) writer.Write(lines[i]);
			bool first = true;
			foreach (var pair in settings)
				if (!found.Contains(pair.Key)) {
					if (first) {
						writer.WriteLine();
						first = false;
					}
					writer.WriteLine();
					writer.Write($"{pair.Key} = {pair.Value}");
				}
			writer.Flush();
			stream.Position = 0;
		}

		///<inheritdoc cref="IConfig.Load"/>
		///<remarks>After the method returns, the position of the stream is returned to
		/// where it was before calling the method. Unless an exception is thrown.</remarks>
		///<exception cref="FormatException">Fail to parse text inside the stream.</exception>
		///<exception cref="IOException">An I/O error occurs.</exception>
		///<exception cref="NotSupportedException">The stream does not support seeking.</exception>
		///<exception cref="ObjectDisposedException">Methods were called after the stream
		/// was closed.</exception>
		///<exception cref="OutOfMemoryException">There is insufficient memory to
		/// allocate a buffer for the returned string.</exception>
		public virtual void Load() {
			long pos = stream.Position;
			stream.Position = 0;

			//Read the stream line by line
			StreamReader reader = new StreamReader(stream);
			string line;
			while (!((line = reader.ReadLine()) is null)) {
				int index = 0;
				char cur;
				//Skip white space
				while (index < line.Length && char.IsWhiteSpace(line[index])) index++;
				//Skip empty lines
				if (index >= line.Length) continue;
				//Skip comments
				cur = line[index];
				if (cur == comment) continue;

				//Read the name of the setting
				StringBuilder builder = new StringBuilder();
				bool space = false;
				while ((cur = line[index++]) != separator) {
					if (cur == comment) throw new FormatException("Comment char" + 
						$"('{comment}') in the middle of the line:\n'{line}'");
					if (char.IsWhiteSpace(cur)) {
						space = true;
						continue;
					}
					if (!charAccepted(cur)) throw new FormatException("Illegal " +
						$"char '{cur}' in the line:\n'{line}'");

					//The exception isn't because of the whitespace, it is because there is
					//a non-whitespace, valid, non separator character after the white space
					if (space) throw new FormatException("Space in the middle of " +
						$"the setting name, in the line:\n'{line}'");

					builder.Append(cur);
					if (index >= line.Length) throw new FormatException("Could not " +
						$"find the separator('{separator}') in the line:\n'{line}'");
				}
				string setting = builder.ToString();
				if (string.IsNullOrEmpty(setting)) throw new FormatException("No " +
					$"Setting name in the line:\n'{line}'");

				//Read the setting value
				builder.Clear();
				while (index < line.Length && char.IsWhiteSpace(line[index])) index++;
				int endSpaces = 0;
				while (index < line.Length) {
					cur = line[index];
					if (cur == comment) break;

					if (char.IsWhiteSpace(cur)) endSpaces++;
					else endSpaces = 0;

					builder.Append(cur);
					index++;
				}

				string value = builder.Length <= endSpaces ? string.Empty :
					builder.ToString(0, builder.Length - endSpaces);

				//Add the setting to the collection
				settings[setting] = value;
			}//end while(line)

			stream.Position = pos;
		}//end Load()

		///<summary>The default function for <see cref="charAccepted"/>. Accepts letters,
		/// digits, and the underscore '_' character.</summary>
		///<param name="chr">The char to validate.</param>
		///<returns>True if the char is a letter, a digit,  or an underscore('_')
		/// character.</returns>
		protected virtual bool DefaultCharAccepted(char chr) =>
			char.IsLetterOrDigit(chr) || chr == '_';

		public virtual IEnumerator<(string, string)> GetEnumerator() =>
			new SettingsEnumerator(settings);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public void Dispose() => stream.Dispose();

		protected class SettingsEnumerator : IEnumerator<(string, string)> {
			IEnumerator<KeyValuePair<string, string>> enumerator;

			object IEnumerator.Current => Current;
			public (string, string) Current =>
				(enumerator.Current.Key, enumerator.Current.Value);

			public SettingsEnumerator(Dictionary<string, string> settings) =>
				enumerator = settings.GetEnumerator();

			public void Dispose() => enumerator.Dispose();
			public bool MoveNext() => enumerator.MoveNext();
			public void Reset() => enumerator.Reset();
		}
	}
}