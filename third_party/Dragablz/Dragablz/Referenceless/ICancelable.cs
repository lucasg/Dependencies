using System;

namespace Dragablz.Referenceless
{
    internal interface ICancelable : IDisposable
    {
        bool IsDisposed { get; }
    }
}
