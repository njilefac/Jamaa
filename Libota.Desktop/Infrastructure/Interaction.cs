using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Libota.Desktop.Infrastructure;

public class Interaction<TInput, TOutput>
{
    private readonly Stack<Action<InteractionContext<TInput, TOutput>>> _handlers = new();
    public Task<TOutput> Handle(TInput input)
    {
        if (_handlers.Count == 0)
        {
            throw new InvalidOperationException("No handlers for this interaction.");
        }

        var interactionContext = new InteractionContext<TInput, TOutput>(input);
        // Execute handlers in LIFO order without removing them, until one produces an output
        var snapshot = _handlers.ToArray(); // top-first order
        for (var i = 0; i < snapshot.Length && interactionContext.Output == null; i++)
        {
            snapshot[i].Invoke(interactionContext);
        }
        
        if(interactionContext.Output == null)
        {
            throw new InvalidOperationException("No handler produced an output for this interaction.");
        }
        
        return Task.FromResult(interactionContext.Output);
    }
    
    public IDisposable RegisterHandler(Action<InteractionContext<TInput, TOutput>> handler)
    {
        if (_handlers.Contains(handler))
        {
            return Disposable.Empty;
        }
        
        _handlers.Push(handler);
        return new HandlerSubscription(this, handler);
    }

    private void RemoveHandler(Action<InteractionContext<TInput, TOutput>> handler)
    {
        if (_handlers.Count == 0)
        {
            return;
        }

        var buffer = new Stack<Action<InteractionContext<TInput, TOutput>>>();
        var removed = false;
        while (_handlers.Count > 0)
        {
            var current = _handlers.Pop();
            if (!removed && current == handler)
            {
                removed = true;
                continue; // skip adding this one back
            }
            buffer.Push(current);
        }

        // Restore remaining handlers preserving original order
        while (buffer.Count > 0)
        {
            _handlers.Push(buffer.Pop());
        }
    }

    private sealed class HandlerSubscription : IDisposable
    {
        private Interaction<TInput, TOutput>? _owner;
        private Action<InteractionContext<TInput, TOutput>>? _handler;
        private bool _disposed;

        public HandlerSubscription(
            Interaction<TInput, TOutput> owner,
            Action<InteractionContext<TInput, TOutput>> handler)
        {
            _owner = owner;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var owner = _owner;
            var handler = _handler;
            _owner = null;
            _handler = null;
            if (owner != null && handler != null)
            {
                owner.RemoveHandler(handler);
            }
        }
    }
}
