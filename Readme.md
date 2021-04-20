# ConfigParser
ConfigParser is a "library" for parsing config files with settings.

## Usage
Can be used with any text file, The way it works is that it is given a stream through the constructor, and when calling `Load()`, the stream is read line by line, parsing each following these rules:
* Empty lines are allowed and ignored. Empty lines are allowed to have any amount of whitespace characters.
* Any whitespace characters before and after a setting/value pair are ignored. Whitespace characters before and after the separator are also ignored.
* Whitespace characters in the middle of the setting name(the part before the separator) are prohibited and will throw a FormatException. Whitespace characters in the middle of the value(the part after the separator) are treated as part of the value.
* A valid comment line is any(or none) amount of whitespace characters followed by the character specified in the constructor(by default, `#`), followed by anything. A comment line is ignored completely.
* A comment character in the middle of a setting line, before the separator, is prohibited and will throw a FormatException. A comment character after the separator is allowed and will count as the endpoint of the value, even if there is no value, at which point the value will be just an empty string.
* The setting name(before the separator) can only consist of characters specified in the constructor(by default, letters, digits, and underscore (`_`) characters). The setting value(after the separator) can consist of any characters and can be empty.
* A valid setting line is a line with any amount of whitespace followed by a single word(no space in the middle) name consisting of valid characters, followed by a separator character(by default, `=`) with any amount of whitespace before and after it, followed by a value of any type, followed by an optional comment.
* A line that has non\-whitespace and non\-comment characters but no separator is invalid and will throw a FormatException.

\* Because Save() and Delete() use a StringBuilder as a buffer, files above 2GB will crash the program. Load ***should*** work fine though.
### Examples of valid lines:
```
```
(That's an empty line)
```
   # Comment with spaces before and after
```
```
name=value
```
```
    name   =    v4lu3 w17h   sp4c3s and s!mb0ls     # Comment after a value and a lot of spaces everywhere
```
(The value here would be `v4lu3 w17h???sp4c3s and s!mb0ls`)
```
emptySetting =
```
```
empty_setting2 = #But now with a comment
```

### Examples of invalid lines:
```
three words name=value
```
```
na #me=value
```
```
emptySetting
```
```
invalid-name = valid value
```

## The `Config` class

### Constructor
Initializes a new config with a stream, a setting/value separator, and a comment char.
```CSharp
Config(Stream stream, char separator, char comment, Func<char, bool> charAccepted)
```
stream: A stream where to read and write the settings from.  
separator: The char that separates between the setting name and its value. Default is `=`  
comment: The char that represents the start of a comment. Default is `#`  
charAccepted: A function that checks whether a character is legal as a setting's name. The default(null) is the DefaultCharAccepted method.

### Properties
The value of a specified setting. Illegal setting names will throw an exception. Values will be stripped of any leading and trailing whitespace characters, and of comments.
```CSharp
string this[string setting] { get; set; }
```

The character of a comment.
```CSharp
char CommentChar { get; }
```

The character of the separator, like `=` or `:`
```CSharp
char SeparatorChar { get; }
```

### Methods
Delete a setting from both the config file and object.
```CSharp
void Delete(string setting);
```
Save the changes to the config file. Any new settings will be appended to the end of the file
```CSharp
void Save();
```

Load the settings from the config file.
```CSharp
void Load();
```

The default function for checking settings name's characters. Accepts letters, digits, and the underscore ( `_` ) character.
```CSharp
bool DefaultCharAccepted(char character)
```

## Example
### Main.cs
```CSharp
using ConfigParser;

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

```

### config.cfg
```
#config.cfg - an example config file

# personal info
name = Yuri Khordal
github = true # a boolean
link = github.com/yuriKhordal/ConfigParser # link to the page

# settings
username = yKhor
pet = 
theme = dark
```

### config.cfg after running
```
#config.cfg - an example config file

# personal info
name = Yuri Khordal
github = false # a boolean
link =  # link to the page

# settings
username = yKhor
theme = dark


colour = Red

```