using System;
using System.Collections.Generic;
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

        var firstHandler  = _handlers.Pop();
        var interactionContext = new InteractionContext<TInput, TOutput>(input);
        while (_handlers.Count > 0 && interactionContext.Output == null)
        {
            var handler = _handlers.Pop();
            handler.Invoke(interactionContext);
        }
        
        if(interactionContext.Output == null)
        {
            throw new InvalidOperationException("No handler produced an output for this interaction.");
        }
        
        return Task.FromResult(interactionContext.Output);
    }
    
    public void RegisterHandler(Action<InteractionContext<TInput, TOutput>> handler)
    {
        if (_handlers.Contains(handler))
        {
            return;
        }
        _handlers.Push(handler);
    }
}