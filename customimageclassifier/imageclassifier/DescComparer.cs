using System.Collections.Generic;

namespace imageclassifier
{
    internal partial class TensorFlowHelper
    {
        public class DescComparer<T> : IComparer<T>
        {
            public int Compare(T x, T y)
            {
                int result = Comparer<T>.Default.Compare(y, x);

                if (result == 0)
                    return 1;   // Handle equality as beeing greater
                else
                    return result;
            }
        }
    }
}