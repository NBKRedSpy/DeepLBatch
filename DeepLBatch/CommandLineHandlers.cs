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

            [Option('p', Description = "Exports the source text and the translated text in a pipe delminited format.")]
            bool exportPsv = false


            )
        {
            TranslationProcessor processor = null!;

            try
            {

                apiKey = string.IsNullOrEmpty(apiKey) ? Program.GetApiKey() : apiKey;

                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("The API key must be provided with the --api-key option, or stored previously using the set-api-key command.");
                    return;
                }

                if(sourceLanguage == null && ignoreCache == false)
                {
                    Console.WriteLine("""
Source Language must be set for the translation cache to be used.  
Provide a source language or use --ignore-cache to not use the cache.
""");
                    return;
                }


                using (var liteDb = new LiteDatabase())
                {
                    processor = new(
                        new DeepLBatch.DbRepository(liteDb), 
                        new DeepLTranslator(apiKey, sourceLanguage, destinationLanguage),
                        ignoreCache);

                    Console.CursorVisible = false;
                    processor.ProgressUpdate += Processor_ProgressUpdate;

                    var translations = processor.TranslateFile(inputFile, outputFile, noApiRequests, exportPsv, batchSize);
                }

                Console.WriteLine("\rTranslation Completed                                            ");

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
