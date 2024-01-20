# DeepLBatch

A command line utility to translate lines in a text file using DeepL.
Includes batching to reduce API call usage and a cache to reuse previously translated text.

# Usage

## Input Prep

It is recommended to only include the text that is to be translated to the target language.
DeepL has a usage limit based on the number of characters translated and the number of API requests made.
Additionally, the text that is not in the source language may be translated to different text.


## API Key
The program requires a DeepL.com API key to translate text.


Sign up for a free DeepL.com account.  Get the API key from the account page.

Use the set-api-key command to register the api key or the translate command's --api-key option.

To delete a stored API key, either use clear-api-key command or delete the ```userSettings.json``` file in the program's folder.


## Parameter Usage


Example Translation:
```
DeepLBatch.exe translate "c:\work\input.txt" "c:\work\output.txt" zh
```


Use ```--help``` to access the help information.  Use ```[name of command] --help``` to get more info on a specific command.

### Overview Help Output
```
Usage: DeepLBatch [command]

DeepLBatch
Description:
    Translates text in a file using DeepL.com and writes the results to an output file.
    Uses a local cache for previously translated text to reduce API calls.

    The user must have a DeepL.com account.  The key can be saved with the set-api-key
    command or as a parameter to the translate command.

Commands:
  reset-cache      Removes all cached translations
  clear-api-key    Removes the DeepL API key from the settings
  set-api-key      Stores the DeepL API key for automatic use.
  show-api-key     Shows the registered API key
  translate        Translates the lines in input file and writes to the output file.  Alias is t

Options:
  -h, --help    Show help message
  --version     Show version
```

### Translate Command Help Output

```

Usage: DeepLBatch translate [--api-key <String>] [--batch-size <Int32>] [--destination-language <String>] [--ignore-cache] [--help] input-file output-file source-language

Translates the lines in input file and writes to the output file.  Alias is t
Description:
    Translates text in a file using DeepL.com and writes the results to an output file.
    Uses a local cache for previously translated text to reduce API calls.

    The user must have a DeepL.com account.  The key can be saved with the set-api-key
    command or as a parameter to the translate command.

Arguments:
  0: input-file          (Required)
  1: output-file         (Required)
  2: source-language    The source language. Defaults to auto detect.  It is recommended to provide the code for the source language to ensure an accurate translation.  The list of Language codes can be found here https://www.deepl.com/docs-api/translate-text

Options:
  --api-key <String>                 If not provided, will use the API key previously stored with the set-API-key command if available.
  --batch-size <Int32>               The number of lines of text to send in a single DeepL.com API call. (Default: 500)
  --destination-language <String>    The destination language. The list of Language codes can be found here https://www.deepl.com/docs-api/translate-text (Default: en-US)
  --ignore-cache                     If true, will not use the translation cache, resulting in every line always being sent to DeepL
  -h, --help                         Show help message

```