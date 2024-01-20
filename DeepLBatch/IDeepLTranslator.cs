
using DeepL.Model;

namespace DeepLBatch
{
    internal interface IDeepLTranslator
    {

        public string? SourceLanguageCode { get; }
        public string TargetLanguageCode { get; }

        TextResult[]? Translate(IEnumerable<string> texts, CancellationToken cancellationToken = default);
    }
}