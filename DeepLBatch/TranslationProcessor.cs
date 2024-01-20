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
        /// Uses DeepL to translate each line in the strings parameter.
        /// </summary>
        /// <param name="strings">The strings to translate</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The results of the translation.</returns>
        /// <exception cref="BatchParseException">Thrown when a translation files</exception>
        public List<Translation> Translate(IEnumerable<string> strings, 
            CancellationToken cancellationToken = default)
        {
            List<Translation> results = new List<Translation>();

            try
            {
                List<Translation> batchTranslations = new List<Translation>();      //The lines in original order
                List<Translation> deepLTranslationList = new List<Translation>();   //References to items that need to be sent for translation

                string? sourceLanguage = _deepLTranslator.SourceLanguageCode?.Trim().ToLower();

                if (sourceLanguage == null && IgnoreCache == false)
                {
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
        public int TranslateFile(string inputFile, string outputFile, int batchSize = 100, CancellationToken cancellationToken = default)
        {
            List<Translation> results;

            results = TranslateFile(inputFile, batchSize);

            string outputText =  string.Join('\n', results.Select(x=> x.TranslatedText));
            File.WriteAllText(outputFile,outputText);

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
        public List<Translation> TranslateFile(string filePath, int batchSize = 100, CancellationToken cancellationToken = default)
        {
            int startingLineNumber = 1; //The one based line count for error messages.
            int endingLineNumber = 1;

            List<Translation> results = new List<Translation>();
            string[]? processingBatch = null;

            TranslationProgress progress = new();

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

                    results.AddRange(Translate(batch, cancellationToken));

                    startingLineNumber += endingLineNumber;
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
