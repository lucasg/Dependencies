using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragablz.Referenceless
{
    internal sealed class SerialDisposable : ICancelable, IDisposable
    {
        private readonly object _gate = new object();
        private IDisposable _current;
        private bool _disposed;

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// 
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                lock (this._gate)
                    return this._disposed;
            }
        }

        /// <summary>
        /// Gets or sets the underlying disposable.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// If the SerialDisposable has already been disposed, assignment to this property causes immediate disposal of the given disposable object. Assigning this property disposes the previous disposable object.
        /// </remarks>
        public IDisposable Disposable
        {
            get
            {
                return this._current;
            }
            set
            {
                bool flag = false;
                IDisposable disposable = (IDisposable)null;
                lock (this._gate)
                {
                    flag = this._disposed;
                    if (!flag)
                    {
                        disposable = this._current;
                        this._current = value;
                    }
                }
                if (disposable != null)
                    disposable.Dispose();
                if (!flag || value == null)
                    return;
                value.Dispose();
            }
        }

        /// <summary>
        /// Disposes the underlying disposable as well as all future replacements.
        /// 
        /// </summary>
        public void Dispose()
        {
            IDisposable disposable = (IDisposable)null;
            lock (this._gate)
            {
                if (!this._disposed)
                {
                    this._disposed = true;
                    disposable = this._current;
                    this._current = (IDisposable)null;
                }
            }
            if (disposable == null)
                return;
            disposable.Dispose();
        }
    }
}
