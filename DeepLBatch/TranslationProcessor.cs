using Csv;
using DeepL.Model;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DeepLBatch
{
    internal class TranslationProcessor
    {

        /// <summary>
        /// Raised when a batch process is sending a new batch.
        /// </summary>
        public event EventHandler<TranslationProgress> ProgressUpdate; 

        private readonly IDeepLTranslator _deepLTranslator;

        private readonly IDbRepository _repository;
        
        /// <summary>
        /// If true, the cache will not be used to avoid cached translations.
        /// The results will still be cached.
        /// </summary>
        public bool IgnoreCache { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TranslationProcessor(IDbRepository repository, IDeepLTranslator deepLTranslator, bool ignoreCache)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _repository = repository;
            _deepLTranslator = deepLTranslator;
            IgnoreCache = ignoreCache;

        }


        /// <summary>
        /// Translates a document.  
        /// DeepL documents indicates document translations are counted as at least 50,000 characters for translation usage.
        /// Supports docx, pptx, xlsx, pdf, htm/html, txt, xlf/xliff 2.1
        /// Generally takes seconds but could take minutes based on server load.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="sourceLanguage"></param>
        /// <param name="targetLanguage"></param>
        public Task TranslateDocument(string inputFile, string outputFile, string? sourceLanguage, string targetLanguage,
            CancellationToken cancellationToken = default)
        {
            return _deepLTranslator.TranslateDocument(inputFile, outputFile, sourceLanguage, targetLanguage, cancellationToken);
        }

        /// <summary>
        /// Uses DeepL to translate each line in the strings parameter.
        /// </summary>
        /// <param name="strings">The strings to translate</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The results of the translation.</returns>
        /// <exception cref="BatchParseException">Thrown when a translation files</exception>
        public List<Translation> Translate(IEnumerable<string> strings,
            bool noApiRequests, out long charactersSentToApi, CancellationToken cancellationToken = default)
        {
            List<Translation> results = new List<Translation>();

            charactersSentToApi = 0;

            try
            {
                List<Translation> batchTranslations = new List<Translation>();      //The lines in original order
                List<Translation> deepLTranslationList = new List<Translation>();   //References to items that need to be sent for translation

                string? sourceLanguage = _deepLTranslator.SourceLanguageCode?.Trim().ToLower();

                if (sourceLanguage == null && IgnoreCache == false)
                {
                    //TODO: Auto detect note:
                    //  I believe an auto detect entry should be able to do a cache check using blank for the source.by using blank for the source string.
                    //  It should be deterministic.
                    //  
                    //  In that case, cache should store "" and another entry for the detected target language, even if the detected language is inaccurate.
                    //  For example, if it is detected as Japanese for text that is obviously English.
                    throw new ArgumentException("Source language must be set if IgnoreCache is false", "SourceLanguage, IgnoreCache");
                }

                string targetLanguage = _deepLTranslator.TargetLanguageCode.Trim().ToLower();

                //-- Use previous translations from cache.  Split out items that need translation
                foreach (string line in strings)
                {

                    string trimmedText = line.Trim();

                    Translation? translation = null;

                    if (!IgnoreCache)
                    {
                        //Get cache.  Create a master list with cached and uncached references.
                        translation = _repository.Get(Translation.GetIndexKey(sourceLanguage!, targetLanguage, trimmedText));
                    }

                    if (translation == null)
                    {
                        charactersSentToApi = +trimmedText.Length;

                        translation = new Translation() { Text = trimmedText, SourceLanguage = sourceLanguage ?? "", TranslatedLanguage = targetLanguage};
                        deepLTranslationList.Add(translation);
                    }

                    batchTranslations.Add(translation);
                }

                //---Send to DeepL for translation
                if (deepLTranslationList.Count != 0)
                {
                    //Group by unique text to translate
                    List<IGrouping<string, Translation>> groupedTranslationRequests = deepLTranslationList.GroupBy(x => x.Text).ToList();


                    if(noApiRequests)
                    {
                        //For debugging purposes
                        throw new ApplicationException("A API translate was requested with no-api-requests enabled.");
                    }

                    //Send to DeepL
                    TextResult[]? translations = _deepLTranslator.Translate(groupedTranslationRequests.Select(x => x.Key), cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    if ((translations?.Length ?? 0) != groupedTranslationRequests.Count)
                    {
                        throw new ApplicationException($"Returned translations count does not match the sent translation count. Sent {translations?.Length} Received {deepLTranslationList.Count}");
                    }


                    //The unique requests and translated results will be in the same order.
                    var requestsToResultsMap = groupedTranslationRequests.Zip(translations!).ToList();

                    //Set the values, add unique item to cache.
                    requestsToResultsMap.ForEach(
                        x =>
                        {
                            x.First.ToList().ForEach(request =>
                            {
                                //Only use the detected language if the source is not used.
                                //There are some translators that will incorrectly guess the source langauge.
                                //  Using the original source language for cache lookups.
                                if(sourceLanguage is null)
                                { 
                                    request.SourceLanguage = x.Second.DetectedSourceLanguageCode.ToLower();
                                }

                                request.TranslatedText = x.Second.Text;

                            });

                            //TODO: see Auto detect note. I think at the least the auto detect should be also be stored
                            //  with a blank for the source language to get cache hits.

                            //Add each unique item to the cache
                            //Always add, even with "ignore cache" enabled.  
                            _repository.UpsertByKey(x.First.First());
                        }
                    );
                }

                return batchTranslations;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error translating text {ex.ToString()}", ex);
            }

            
       }


        /// <summary>
        /// Translates the specified file.
        /// </summary>
        /// <param name="inputFile">The translation file with one line per translation</param>
        /// <param name="outputFile">The file to write the results to </param>
        /// <param name="batchSize">The number of lines to send to DeepL per api call.</param>
        /// <param name="cancellationToken">Can cancel the translations</param>
        /// <returns></returns>
        public int TranslateFile(string inputFile, string outputFile, bool noApiRequests, bool exportTsv, out long charactersSentToApi, 
            int batchSize = 100, CancellationToken cancellationToken = default)
        {
            List<Translation> results;

            results = TranslateFile(inputFile, noApiRequests, out charactersSentToApi, batchSize);

            //Debug
            //string outputText = string.Join('\n', results.Select(x => String.Join("|", x.Text, x.TranslatedText)));

            string outputText;

            if(exportTsv)
            {

                using (StreamWriter fileWriter =  File.CreateText(outputFile))
                {
                    //CsvWriter.Write(fileWriter, Array.Empty<string>(), results.Select(x => new string[] { x.Text, x.TranslatedText }), '|', true);
                    CsvWriter.Write(fileWriter, new string[] { "", "" }, results.Select(x => new string[] { x.Text, x.TranslatedText }), '\t', true);
                    fileWriter.Flush();
                }

            }
            else
            {
                outputText = string.Join('\n', results.Select(x => x.TranslatedText));
                File.WriteAllText(outputFile, outputText);

            }




            return results.Count;
        }

        /// <summary>
        /// Translates each line in a file and returns a list of the results.
        /// </summary>
        /// <param name="filePath">The path to the file to read</param>
        /// <param name="batchSize">The number of lines to send to DeepL per api call.</param>
        /// <param name="cancellationToken">Can cancel the translations</param>
        /// <returns>The translated items.</returns>
        /// <exception cref="BatchParseException"></exception>
        public List<Translation> TranslateFile(string filePath, bool noApiRequests, out long charactersSentToApi, int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            int startingLineNumber = 1; //The one based line count for error messages.
            int endingLineNumber = 1;

            List<Translation> results = new List<Translation>();
            string[]? processingBatch = null;

            TranslationProgress progress = new();

            charactersSentToApi = 0;
            try
            {
                List<string> allLines = File.ReadLines(filePath).ToList();

                progress.TotalItems = allLines.Count;
                
                foreach (string[] batch in allLines.Chunk(batchSize))
                {

                    processingBatch = batch;

                    endingLineNumber = startingLineNumber + batch.Length - 1;
                    progress.StartLine = startingLineNumber;
                    progress.EndLine = endingLineNumber;

                    this.ProgressUpdate.Invoke(this, progress);

                    results.AddRange(Translate(batch, noApiRequests, out charactersSentToApi,cancellationToken));

                    startingLineNumber = endingLineNumber;
                }
            }
            catch (Exception ex)
            {
                string text = processingBatch is null ? "" : String.Join('\n', processingBatch);
                throw new BatchParseException(ex, startingLineNumber, endingLineNumber, text);
            }

            return results;
        }
    }
}
