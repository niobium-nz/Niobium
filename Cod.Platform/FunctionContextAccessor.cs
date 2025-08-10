using Microsoft.AspNetCore.Http;

namespace Cod.Platform
{
    internal sealed class FunctionContextAccessor : IHttpContextAccessor
    {
        private static readonly AsyncLocal<FunctionContextRedirect> _currentContext = new();

        public HttpContext? HttpContext
        {
            get => _currentContext.Value?.HeldContext;
            set
            {
                FunctionContextRedirect? holder = _currentContext.Value;
                if (holder != null)
                {
                    // Clear current context trapped in the AsyncLocals, as its done.
                    holder.HeldContext = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the context in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _currentContext.Value = new FunctionContextRedirect { HeldContext = value };
                }
            }
        }

        private sealed class FunctionContextRedirect
        {
            public HttpContext? HeldContext;
        }
    }
}
