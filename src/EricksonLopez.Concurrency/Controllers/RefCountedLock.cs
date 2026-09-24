// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;

namespace EricksonLopez.Concurrency.Controllers;

internal sealed class RefCountedLock : IDisposable
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public int RefCount { get; set; } = 1;

    public void Reset()
    {
        RefCount = 0;
    }

    public void Dispose()
    {
        Semaphore.Dispose();
    }
}
