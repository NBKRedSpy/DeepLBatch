using DeepL;
using DeepL.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DeepLBatch
{
    /// <summary>
    /// Translates text
    /// </summary>
    internal class DeepLTranslator : IDeepLTranslator
    {
        public string? SourceLanguageCode { get; private set; }
        public string TargetLanguageCode { get; private set; }

        private Translator _translator { get; set; }

        private TextTranslateOptions _textTranslateOptions { get; set; }

        public DeepLTranslator(string apiKey, string? sourceLanguageCode, string targetLanguageCode)
        {

            _translator = new DeepL.Translator(apiKey, new DeepL.TranslatorOptions()
            {
                appInfo = new AppInfo()
                {
                    AppName = "NBK_RedSpy.DeepLBatchUtility",
                    AppVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString()
                },
            });

            _textTranslateOptions = new TextTranslateOptions()
            {
                PreserveFormatting = true
            };

            this.SourceLanguageCode = sourceLanguageCode;
            this.TargetLanguageCode = targetLanguageCode;
        }


        /// <summary>
        /// Translates a supported document format.
        /// Note the docs says that calls are counted as a minimum of 50,000 characters.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="sourceLanguage"></param>
        /// <param name="targetLanguage"></param>
        public async Task TranslateDocument(string inputFile, string outputFile, string? sourceLanguage, 
            string targetLanguage, CancellationToken cancellationToken = default)
        {
            await _translator.TranslateDocumentAsync(new FileInfo(inputFile), new FileInfo(outputFile), sourceLanguage,
                targetLanguage, null, cancellationToken);
        }

        /// <summary>
        /// Translates a list of text items.
        /// </summary>
        /// <param name="texts"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public TextResult[]? Translate(IEnumerable<string> texts, CancellationToken cancellationToken = default)
        {
            try
            {
                TextResult[]? translationResults = _translator.TranslateTextAsync(texts, SourceLanguageCode, TargetLanguageCode,
                    _textTranslateOptions, cancellationToken).Result;

                return translationResults;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error Translating text", ex);
            }

        }
    }
}
