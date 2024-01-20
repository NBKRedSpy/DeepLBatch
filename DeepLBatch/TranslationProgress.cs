using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepLBatch
{
    /// <summary>
    /// The current progress for a batch request.
    /// </summary>
    internal class TranslationProgress
    {
        public int TotalItems { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }

        public int CurrentBatchCount 
        {  
            get
            {
                return EndLine - StartLine;
            } 
        }

        public float PercentCompleted 
        {
            get { return StartLine / ((float)TotalItems); }
        }




    }
}
