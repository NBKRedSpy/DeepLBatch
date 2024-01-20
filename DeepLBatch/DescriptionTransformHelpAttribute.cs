using Cocona.Command;
using Cocona.Help;
using Cocona.Help.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepLBatch
{
    internal class DescriptionTransformHelpAttribute : TransformHelpAttribute
    {
        public override void TransformHelp(HelpMessage helpMessage, CommandDescriptor command)
        {
            var descSection = (HelpSection)helpMessage.Children.First(x => x is HelpSection section && section.Id == HelpSectionId.Description);
            descSection.Children.Add(new HelpPreformattedText(
"""
Description: 
    Translates text in a file using DeepL.com and writes the results to an output file.
    Uses a local cache for previously translated text to reduce API calls.

    The user must have a DeepL.com account.  The key can be saved with the set-api-key 
    command or as a parameter to the translate command.
"""));
        }
    }
}
