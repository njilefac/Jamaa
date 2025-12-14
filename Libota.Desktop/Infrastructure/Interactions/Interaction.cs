using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Libota.Desktop.Infrastructure.Interactions;

public class Interaction<TInput, TOutput>
{
    private readonly Stack<Func<InteractionContext<TInput, TOutput>, ValueTask>> _handlers = new();

    public async Task<TOutput> Handle(TInput input)
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
            await snapshot[i].Invoke(interactionContext);
        }

        if (interactionContext.Output == null)
        {
            throw new InvalidOperationException("No handler produced an output for this interaction.");
        }

        return interactionContext.Output;
    }

    public IDisposable RegisterHandler(Action<InteractionContext<TInput, TOutput>> handler)
    {
        // Wrap sync handler into ValueTask delegate
        Func<InteractionContext<TInput, TOutput>, ValueTask> wrapper = ctx =>
        {
            handler(ctx);
            return ValueTask.CompletedTask;
        };

        if (_handlers.Contains(wrapper))
        {
            return Disposable.Empty;
        }

        _handlers.Push(wrapper);
        return new HandlerSubscription(this, wrapper);
    }

    public IDisposable RegisterHandler(Func<InteractionContext<TInput, TOutput>, Task> asyncHandler)
    {
        // Adapt Task to ValueTask to keep a single storage type
        Func<InteractionContext<TInput, TOutput>, ValueTask> wrapper = ctx => new ValueTask(asyncHandler(ctx));

        if (_handlers.Contains(wrapper))
        {
            return Disposable.Empty;
        }

        _handlers.Push(wrapper);
        return new HandlerSubscription(this, wrapper);
    }

    public IDisposable RegisterHandler(Func<InteractionContext<TInput, TOutput>, ValueTask> asyncHandler)
    {
        if (_handlers.Contains(asyncHandler))
        {
            return Disposable.Empty;
        }

        _handlers.Push(asyncHandler);
        return new HandlerSubscription(this, asyncHandler);
    }

    private void RemoveHandler(Func<InteractionContext<TInput, TOutput>, ValueTask> handler)
    {
        if (_handlers.Count == 0)
        {
            return;
        }

        var buffer = new Stack<Func<InteractionContext<TInput, TOutput>, ValueTask>>();
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

    private sealed class HandlerSubscription(
        Interaction<TInput, TOutput> owner,
        Func<InteractionContext<TInput, TOutput>, ValueTask> handler)
        : IDisposable
    {
        private Interaction<TInput, TOutput>? _owner = owner;
        private Func<InteractionContext<TInput, TOutput>, ValueTask>? _handler = handler;
        private bool _disposed;

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