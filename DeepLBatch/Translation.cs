using DeepL.Model;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DeepLBatch
{

    /// <summary>
    /// A cached translation
    /// </summary>
    internal class Translation
    {
        [BsonId]
        public ObjectId? ID { get; set; }
        public string Text { get; set; } = "";
        public string TranslatedText { get; set; } = "";
        public string SourceLanguage { get; set; } = "";
        public string TranslatedLanguage { get; set; } = "";

        /// <summary>
        /// The index for LiteDb.  Workaround since it does not support multi key indexes
        /// </summary>
        public string IndexKey 
        {  
            get
            {
                return GetIndexKey(SourceLanguage, TranslatedLanguage, Text);
            } 
        }

        public Translation()
        {
            
        }

        public static string GetIndexKey(string sourceLanguage, string translatedLanguage, string text)
        {
            return string.Join("|",sourceLanguage, translatedLanguage, text);
        }

        public Translation(string text, string translatedText, string sourceLanguage, string translatedLanguage)
        {
            Text = text;
            TranslatedText = translatedText;
            SourceLanguage = sourceLanguage;
            TranslatedLanguage = translatedLanguage;
            ID = ObjectId.NewObjectId();
        }
    }
}
