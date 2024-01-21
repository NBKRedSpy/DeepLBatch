using Cocona;
using DeepL;
using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace DeepLBatch
{
    [DescriptionTransformHelp]
    internal class CommandLineHandlers
    {
        [Command(Description = "Removes all cached translations.")]
        public void ResetCache()
        {
            Console.WriteLine("The cache has been cleared.");
        }

        [Command(Description = "Removes the DeepL API key from the settings.")]
        public void ClearApiKey()
        {
            Program.SetApiKey("");
            Console.WriteLine("The API key has been cleared.");
        }

        [Command(Description = "Stores the DeepL API key for automatic use.")]
        public void SetApiKey([Argument] string apiKey)
        {
            Console.WriteLine("The API key has been stored for future API calls.");
            Program.SetApiKey(apiKey);
        }

        [Command(Description = "Shows the registered API key.")]
        public void ShowApiKey()
        {
            string? apiKey = Program.GetApiKey();


            string message = apiKey is null ?
                "The api key is not set.  Use set-api-key to register." :
                $"Registered API key : '{apiKey}'";

            Console.WriteLine(message); 
        }

        [Command(Aliases = ["d"], Description = "Translates a document.")]
        public void TranslateDocument(
            [Argument] string inputFile,
            [Argument] string outputFile,

            [Option('a', Description = "If not provided, will use the API key previously stored with the set-API-key command if available.")]
            string? apiKey = null,

            [Argument(Description = "The source language. Defaults to auto detect.  The list of Language codes can be found here https://www.deepl.com/docs-api/translate-text")]
            string? sourceLanguage = null,

            [Option(Description = "The destination language. The list of Language codes can be found here https://www.deepl.com/docs-api/translate-text")]
            string destinationLanguage = LanguageCode.EnglishAmerican)
        {
            try
            {
                string? actualApiKey = GetApiKey(apiKey);
                if (actualApiKey is null) { return; }

                var processor = new TranslationProcessor(
                   null!,
                   new DeepLTranslator(actualApiKey, sourceLanguage, destinationLanguage), true);

                Console.Write("Processing document");

                CancellationTokenSource cancellationSource = new CancellationTokenSource();

                Task translationTask = processor.TranslateDocument(inputFile, outputFile, sourceLanguage, destinationLanguage, cancellationSource.Token);



                Console.CursorVisible = false;
                
                _cancelWasRequested = false;
                Console.CancelKeyPress += Console_CancelKeyPress;

                int animationIndex = 0; //Animate since the user may think the program is not doing anything.
                int cursorStartPosition = Console.CursorLeft;

                while (translationTask.Wait(500) == false)
                {

                    animationIndex++;
                    if(animationIndex == 4)
                    {
                        Console.CursorLeft = cursorStartPosition;
                        Console.Write("    ");
                        Console.CursorLeft = cursorStartPosition;
                        animationIndex = 0;
                    }
                    else
                    {
                        Console.Write('.');
                    }

                    if (_cancelWasRequested)
                    {
                        try
                        {
                            cancellationSource.Cancel();
                        }
                        catch (TaskCanceledException)
                        {
                            //Ignore
                        }

                        Console.WriteLine();
                        Console.WriteLine("Operation canceled.  DeepL may have charged the usage amount.");


                        return;
                    }


                }

                Console.WriteLine();
                Console.WriteLine("Done");

            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.ToString());

            }
            finally
            {
                Console.CancelKeyPress -= Console_CancelKeyPress;
                _cancelWasRequested = false;
                Console.CursorVisible = true; 
            }
        }

        private bool _cancelWasRequested = false;
        private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            _cancelWasRequested = true;
        }

        private static string? GetApiKey(string? apiKey)
        {
            var foundApiKey = string.IsNullOrEmpty(apiKey) ? Program.GetApiKey() : apiKey;

            if (string.IsNullOrEmpty(foundApiKey))
            {
                Console.WriteLine("The API key must be provided with the --api-key option, or stored previously using the set-api-key command.");
                return null;
            }

            return foundApiKey;
        }


        [Command(Aliases = ["t"], Description = "Translates the lines in input file and writes to the output file.  Alias is t.")]
        public void Translate(
            [Argument] string inputFile,
            [Argument] string outputFile,

            [Option('a', Description = "If not provided, will use the API key previously stored with the set-API-key command if available.")]
            string? apiKey = null,

            [Option(Description = "The number of lines of text to send in a single DeepL.com API call.")]
            int batchSize = 500,

            [Argument(Description = "The source language. Defaults to auto detect.  It is recommended to provide the code for the source language to ensure an accurate translation.  The list of Language codes can be found here https://www.deepl.com/docs-api/translate-text")]
            string? sourceLanguage = null,

            [Option(Description = "The destination language. The list of Language codes can be found here https://www.deepl.com/docs-api/translate-text")]
            string destinationLanguage = LanguageCode.EnglishAmerican,

            [Option(Description = "If true, will not use the translation cache, resulting in every line always being sent to DeepL.")]
            bool ignoreCache = false,

            [Option('d', Description = "Throws an error if an entry is not in cache.  Prevents API calls for debugging purposes.")]
            bool noApiRequests = false,

            [Option('t', Description = "Exports the source text and the translated text in a tab delminited format.")]
            bool exportTsv = false


            )
        {
            TranslationProcessor processor = null!;

            try
            {
                string? actualApiKey = GetApiKey(apiKey);
                if (actualApiKey is null) { return; }

                if (sourceLanguage == null && ignoreCache == false)
                {
                    Console.WriteLine("""
Source Language must be set for the translation cache to be used.  
Provide a source language or use --ignore-cache to not use the cache.
""");
                    return;
                }


                long charactersSentToApi = 0;

                using (var liteDb = new LiteDatabase())
                {
                    processor = new(
                        new DeepLBatch.DbRepository(liteDb), 
                        new DeepLTranslator(actualApiKey, sourceLanguage, destinationLanguage),
                        ignoreCache);

                    Console.CursorVisible = false;
                    processor.ProgressUpdate += Processor_ProgressUpdate;


                    int sentTranslations = processor.TranslateFile(inputFile, outputFile, noApiRequests, exportTsv,
                        out charactersSentToApi, batchSize);
                }

                Console.WriteLine("\rTranslation Completed                                            ");
                Console.WriteLine($"{charactersSentToApi} uncached characters were sent to DeepL for translation");

            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.ToString());

            }
            finally
            {
                Console.CursorVisible = true;

                if (processor is not null)
                {
                    processor.ProgressUpdate -= Processor_ProgressUpdate;
                }
            }

        }

        private void Processor_ProgressUpdate(object? sender, TranslationProgress e)
        {
            Console.Write($"\r[{e.PercentCompleted:P0}] Processing items {e.StartLine} - {e.EndLine}           ");
        }
    }
}
