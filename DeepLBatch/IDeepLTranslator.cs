
using DeepL.Model;

namespace DeepLBatch
{
    internal interface IDeepLTranslator
    {

        string? SourceLanguageCode { get; }
        string TargetLanguageCode { get; }

        TextResult[]? Translate(IEnumerable<string> texts, CancellationToken cancellationToken = default);

        Task TranslateDocument(string inputFile, string outputFile, string? sourceLanguage, string targetLanguage, 
            CancellationToken cancellationToken = default);
    }
}