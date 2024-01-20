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
