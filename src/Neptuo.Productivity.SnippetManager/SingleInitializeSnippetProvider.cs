using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public abstract partial class SingleInitializeSnippetProvider : ISnippetProvider
{
    private Task? initializeTask;
    private object initializeLock = new();
    private WrappedSnippetProviderContext? wrappedContext;

    public virtual Task InitializeAsync(SnippetProviderContext context)
    {
        if (initializeTask != null)
        {
            InitializeNewContext(context);
            return initializeTask;
        }

        lock (initializeLock)
        {
            if (initializeTask != null)
            {
                InitializeNewContext(context);
                return initializeTask;
            }

            wrappedContext = new WrappedSnippetProviderContext(context);
            return initializeTask = InitializeOnceAsync(wrappedContext);
        }
    }

    private void InitializeNewContext(SnippetProviderContext context)
    {
        context.AddRange(wrappedContext!.Models);
        wrappedContext!.Inner = context;
    }

    protected abstract Task InitializeOnceAsync(SnippetProviderContext context);

    public Task UpdateAsync(SnippetProviderContext context)
    {
        if (wrappedContext == null)
            throw Ensure.Exception.NotSupported($"The '{nameof(UpdateAsync)}' called before '{nameof(InitializeAsync)}'.");

        return UpdateOverrideAsync(wrappedContext);
    }

    protected virtual Task UpdateOverrideAsync(SnippetProviderContext context)
        => Task.CompletedTask;
}
