// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.Controllers;

internal sealed class ReentrancyNode
{
    public string EntityId { get; }
    public ReentrancyNode? Previous { get; }

    public ReentrancyNode(string entityId, ReentrancyNode? previous)
    {
        EntityId = entityId;
        Previous = previous;
    }

    public bool Contains(string entityId)
    {
        var current = this;
        while (current != null)
        {
            if (string.Equals(current.EntityId, entityId, StringComparison.Ordinal))
            {
                return true;
            }

            current = current.Previous;
        }

        return false;
    }
}
