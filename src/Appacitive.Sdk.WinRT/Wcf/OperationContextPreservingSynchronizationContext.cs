﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Appacitive.Sdk.Wcf
{
    /// <summary>
    ///     A custom synchronisation context that propagates the operation context across threads.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "We don't actually want to dispose the operation context scope because it may wind up being disposed on a different thread than the one that created it.")]
    internal class OperationContextPreservingSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        ///     The operation context to propagate.
        /// </summary>
        readonly OperationContext _operationContext;

        /// <summary>
        ///     Object used for locking the live scope.
        /// </summary>
        readonly object _scopeLock = new object();

        /// <summary>
        ///     Our live operation context scope.
        /// </summary>
        OperationContextScope _operationContextScope;


        /// <summary>
        ///     Create a new operation-context-preserving synchronization context.
        /// </summary>
        /// <param name="operationContext">
        ///     The operation context to propagate.
        /// </param>
        public OperationContextPreservingSynchronizationContext(OperationContext operationContext)
        {
            if (operationContext == null)
                throw new ArgumentNullException("operationContext");

            _operationContext = operationContext;
        }

        /// <summary>
        ///     Create a copy of the synchronisation context.
        /// </summary>
        /// <returns>
        ///     The new synchronisation context.
        /// </returns>
        public override SynchronizationContext CreateCopy()
        {
            return new OperationContextPreservingSynchronizationContext(_operationContext);
        }

        /// <summary>
        ///     Dispatch a synchronous message to the synchronization context.
        /// </summary>
        /// <param name="callback">
        ///     The <see cref="SendOrPostCallback"/> delegate to call.
        /// </param>
        /// <param name="state">
        ///     The state object passed to the delegate.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///     The method was called in a Windows Store app. The implementation of <see cref="SynchronizationContext"/> for Windows Store apps does not support the <see cref="SynchronizationContext.Send"/> method.
        /// </exception>
        public override void Send(SendOrPostCallback callback, object state)
        {
            base.Send(
                chainedState =>
                    CallWithOperationContext(callback, state),
                state
            );
        }

        /// <summary>
        ///     Dispatch an asynchronous message to the synchronization context.
        /// </summary>
        /// <param name="callback">
        ///     The <see cref="SendOrPostCallback"/> delegate to call in the synchronisation context.
        /// </param>
        /// <param name="state">
        ///     The state object passed to the delegate.
        /// </param>
        public override void Post(SendOrPostCallback callback, object state)
        {
            base.Post(
                chainedState =>
                    CallWithOperationContext(callback, state),
                state
            );
        }

        /// <summary>
        ///     Push a new operation context scope onto the scope stack, if required.
        /// </summary>
        /// <remarks>
        ///     <c>true</c>, if a new operation context scope was created, otherwise, <c>false</c>.
        /// </remarks>
        bool PushOperationContextScopeIfRequired()
        {
            if (OperationContext.Current != _operationContext)
            {
                lock (_scopeLock)
                {
                    ReleaseOperationContextScopeIfRequired();
                    _operationContextScope = new OperationContextScope(_operationContext);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Release the current operation context scope generated by the synchronisation context (if it exists).
        /// </summary>
        void ReleaseOperationContextScopeIfRequired()
        {
            if (_operationContextScope == null)
            {
                lock (_scopeLock)
                {
                    if (_operationContextScope != null)
                    {
                        _operationContextScope.Dispose();
                        _operationContextScope = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Call a callback delegate with a the operation context set.
        /// </summary>
        /// <param name="chainedCallback">
        ///     The chained delegate to call.
        /// </param>
        /// <param name="chainedState">
        ///     The callback state, if any.
        /// </param>
        void CallWithOperationContext(SendOrPostCallback chainedCallback, object chainedState)
        {
            if (chainedCallback == null)
                throw new ArgumentNullException("chainedCallback");

            bool pushedNewScope = PushOperationContextScopeIfRequired();
            try
            {
                chainedCallback(chainedState);
            }
            finally
            {
                if (pushedNewScope)
                    ReleaseOperationContextScopeIfRequired();
            }
        }
    }
}
