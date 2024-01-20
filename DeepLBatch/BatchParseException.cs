using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DeepLBatch
{
    internal class BatchParseException : Exception
    {
        public int EndlingLineNumber { get; }
        public int StartingLineNumber { get; }
        public string LineText { get; }

        public BatchParseException() : base() { 
            StartingLineNumber = -1;
            EndlingLineNumber = -1;
            LineText = "";
        }

        public BatchParseException(Exception innerException, int startingLineNumber, int endingLineNumber, string lineText) : 
            base(BatchParseException.CreateMessage(innerException, startingLineNumber, endingLineNumber), innerException)
        {
            StartingLineNumber = startingLineNumber;
            EndlingLineNumber = endingLineNumber;
            LineText = lineText;
            Data.Add("Text", lineText);
        }

        public BatchParseException(string? message, Exception? innerException, int lineNumber, string lineText) : base(message, innerException)
        {
            StartingLineNumber = lineNumber;
            LineText = lineText;
        }

        private static string CreateMessage(Exception ex, int startingLineNumber, int endingLineNumber)
        {
            return $"Error parsing lines: {startingLineNumber} - {endingLineNumber}. Exception: '{ex.ToString()}'";
        }
    }
}
