using Cocona.Command;
using Cocona.Help;
using Cocona.Help.DocumentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeepLBatch
{
    internal class DescriptionTransformHelpAttribute : TransformHelpAttribute
    {
        public override void TransformHelp(HelpMessage helpMessage, CommandDescriptor command)
        {

            string? text = null;

            text = command switch
            {
                var x when x.Method.Name == nameof(CommandLineHandlers.TranslateDocument) =>
"""

Description:
    Supports translating docx, pptx, xlsx, pdf, htm/html, txt, and xlf/xliff 2.1.
    The process generally takes seconds but could take minutes based on server load.

    This method does not cache previous translations for re-use. 
    DeepL's documentation indicates translating a full document is considered to be at a minimum of 50,000 
    characters of usage.
""",

                var x when x.Name == "ShowDefaultMessage" =>
"""

Description: 
    Translates text in a file using DeepL.com and writes the results to an output file.
    Uses a local cache for previously translated text to reduce API calls.

    The user must have a DeepL.com account.  The key can be saved with the set-api-key 
    command or as a parameter to the translate command.
""",
                _ => null
            };

            if(text == null) return;

            var descSection = (HelpSection)helpMessage.Children.First(x => x is HelpSection section && section.Id == HelpSectionId.Description);
            descSection.Children.Add(new HelpPreformattedText(text));
        }
    }
}
