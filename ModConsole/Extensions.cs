using System;
using Mono.CSharp;

namespace ModConsole
{
    public static class Extensions
    {
        public static bool TryEvaluate(this Evaluator self, string input, out object output)
        {
            try
            {
                output = self.Evaluate(input);

                return true;
            }
            catch (ArgumentException)
            {
                // This happens every time you have a compiler error.
                // Instead of throwing the error, it just says 'The expression cannot be resolved'
                // Actual errors are sent to the LambdaWriter, so we can mostly ignore the exception.
                
                output = null;
                
                return false;
            }
        }
    }
}