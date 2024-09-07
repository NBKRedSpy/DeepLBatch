# DeepLBatch

A command line utility using DeepL.com's API to translate each line in a file as a stand alone translation.
Includes batching to reduce API usage costs and a cache to reuse previously translated text.

Also supports translating documents including Excel spreadsheets.  See the [Full Document Translation](#full-document-translation) section for important usage cost information.

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


Example Chinese to English Translation:
```
DeepLBatch.exe translate "c:\work\input.txt" "c:\work\output.txt" zh
```

Example Chinese to German Translation
```
DeepLBatch.exe translate --destination-language de c:\work\translation.txt c:\work\out.txt zh
```


Use ```--help``` to access the help information.  Use ```[name of command] --help``` to get more info on a specific command.

## translate vs translate-document

The ```translate``` command treats each line in a text file as an individual translation.  This mode caches previously translated lines for re-use.  

The ```translate-document``` command uses DeepL's "document" translation mode.  
This mode cannot be cached.  According to the documentation, DeepL considers the translation usage cost to be the higher of the number of characters translated or 50,000.  
This mode supports docx, pptx, xlsx, pdf, htm/html, txt, xlf/xliff 2.1.


## Spreadsheet Import Issue

Spreadsheets may have issues importing text files with non-English characters.  It may manifest as less lines shown than actually translated.

This is a common spreadsheet program issue and not related to the translation.  In this case, try using the translate command's tsv option (-t). 
The output file will be in the format of ```Input Text(a tab)Output Text```.  When importing into the spreadsheet, use the "split text to columns" command with tab as the delimiter. 

### Overview Usage Help
```
Usage: DeepLBatch [command]

DeepLBatch

Description:
    Translates text in a file using DeepL.com and writes the results to an output file.
    Uses a local cache for previously translated text to reduce API calls.

    The user must have a DeepL.com account.  The key can be saved with the set-api-key
    command or as a parameter to the translate command.

Commands:
  reset-cache           Removes all cached translations.
  clear-api-key         Removes the DeepL API key from the settings.
  set-api-key           Stores the DeepL API key for automatic use.
  show-api-key          Shows the registered API key.
  translate-document    Translates a document.
  translate             Translates the lines in input file and writes to the output file.  Alias is t.

Options:
  -h, --help    Show help message
  --version     Show version
```

### Translate Command Usage Help

```
Usage: DeepLBatch translate [--api-key <String>] [--batch-size <Int32>] [--destination-language <String>] [--ignore-cache] [--no-api-requests] [--export-tsv] [--help] input-file output-file source-language

Translates the lines in input file and writes to the output file.  Alias is t.

Arguments:
  0: input-file          (Required)
  1: output-file         (Required)
  2: source-language    The source language. Defaults to auto detect.  It is recommended to provide the code for the source language to ensure an accurate translation.  The list of Language codes can be found here https://www.deepl.com/docs-api/translate-text

Options:
  -a, --api-key <String>             If not provided, will use the API key previously stored with the set-API-key command if available.
  --batch-size <Int32>               The number of lines of text to send in a single DeepL.com API call. (Default: 500)
  --destination-language <String>    The destination language. The list of Language codes can be found here https://www.deepl.com/docs-api/translate-text (Default: en-US)
  --ignore-cache                     If true, will not use the translation cache, resulting in every line always being sent to DeepL.
  -d, --no-api-requests              Throws an error if an entry is not in cache.  Prevents API calls for debugging purposes.
  -t, --export-tsv                   Exports the source text and the translated text in a tab delminited format.
  -h, --help                         Show help message
```

# Cache Database
The database is named TranslationCache.db and is created in the application's directory.  
This is a LiteDb database and can be viewed using LiteDb Studio, which is located at https://github.com/mbdavid/LiteDB.Studio/releases .  
The file can be safely deleted.  There may also be a TranslationCache_Log.db file which is also safe to delete.

