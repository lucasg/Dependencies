using System;
using System.Collections.Generic;

namespace Dragablz.Core
{
    internal class FuncComparer<TObject> : IComparer<TObject>
    {
        private readonly Func<TObject, TObject, int> _comparer;

        public FuncComparer(Func<TObject, TObject, int> comparer)
        {
            if (comparer == null) throw new ArgumentNullException("comparer");
            
            _comparer = comparer;
        }

        public int Compare(TObject x, TObject y)
        {
            return _comparer(x, y);
        }
    }
}