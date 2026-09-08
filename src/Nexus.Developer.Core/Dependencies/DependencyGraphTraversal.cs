namespace Nexus.Developer.Core.Dependencies;

// Pure graph-structure helper over an already-loaded list of Blocking edges. Not a
// repository, and deliberately stateless -- takes the edges as input so it is
// trivially unit-testable without a database. Only builds and walks the graph
// structure; RequiredState *evaluation* against real work-item state is explicitly
// out of scope for WI-07-2.1.1.
public static class DependencyGraphTraversal
{
    // Returns every node that transitively blocks the given node (direct + indirect
    // Blocking predecessors), walking backward through Blocking edges. Order:
    // nearest predecessor first. "A blocks B, B blocks C" -> GetBlockingChain(C)
    // returns [B, A].
    public static IReadOnlyList<(WorkItemDependencyNodeType Type, Guid Id)> GetBlockingChain(
        IReadOnlyList<WorkItemDependency> existingBlockingEdges,
        WorkItemDependencyNodeType nodeType,
        Guid nodeId)
    {
        var incoming = BuildIncoming(existingBlockingEdges);

        var chain = new List<(WorkItemDependencyNodeType Type, Guid Id)>();
        var visited = new HashSet<(WorkItemDependencyNodeType Type, Guid Id)>();
        var queue = new Queue<(WorkItemDependencyNodeType Type, Guid Id)>();

        // Seed the queue with the start node's direct blockers (nearest first, so
        // the level-order walk below naturally returns nearest-before-farther).
        if (incoming.TryGetValue((nodeType, nodeId), out var direct))
        {
            foreach (var blocker in direct)
            {
                if (visited.Add(blocker))
                {
                    chain.Add(blocker);
                    queue.Enqueue(blocker);
                }
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (incoming.TryGetValue(current, out var predecessors))
            {
                foreach (var predecessor in predecessors)
                {
                    if (visited.Add(predecessor))
                    {
                        chain.Add(predecessor);
                        queue.Enqueue(predecessor);
                    }
                }
            }
        }

        return chain;
    }

    // Returns the full cycle path -- including the proposed new edge -- that adding
    // upstream -> downstream would create, or an empty list if it would create none.
    // The returned path is an ordered walk of nodes where each consecutive pair is
    // connected and the final node connects back to the first via the proposed
    // edge; e.g. with A->B and B->C existing, adding C->A returns [A, B, C, A].
    public static IReadOnlyList<(WorkItemDependencyNodeType Type, Guid Id)> FindCycleIfAdded(
        IReadOnlyList<WorkItemDependency> existingBlockingEdges,
        WorkItemDependencyNodeType upstreamType,
        Guid upstreamId,
        WorkItemDependencyNodeType downstreamType,
        Guid downstreamId)
    {
        var start = (downstreamType, downstreamId);
        var target = (upstreamType, upstreamId);

        if (start == target)
        {
            // Degenerate self-loop: adding X -> X is itself a one-edge cycle. The
            // entity constructor rejects this before any traversal runs, but stay
            // total here regardless.
            return new[] { start, start };
        }

        // Adding upstream -> downstream closes a cycle iff downstream can already
        // reach upstream through existing Blocking edges.
        var outgoing = BuildOutgoing(existingBlockingEdges);

        var predecessorOf = new Dictionary<(WorkItemDependencyNodeType Type, Guid Id), (WorkItemDependencyNodeType Type, Guid Id)>();
        var visited = new HashSet<(WorkItemDependencyNodeType Type, Guid Id)>();
        var queue = new Queue<(WorkItemDependencyNodeType Type, Guid Id)>();
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == target)
            {
                break;
            }

            if (!outgoing.TryGetValue(current, out var nexts))
            {
                continue;
            }

            foreach (var next in nexts)
            {
                if (visited.Add(next))
                {
                    predecessorOf[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        if (!visited.Contains(target))
        {
            return Array.Empty<(WorkItemDependencyNodeType Type, Guid Id)>();
        }

        // Reconstruct downstream -> ... -> upstream, then close the walk with the
        // proposed upstream -> downstream edge (the final element repeats start).
        var path = new List<(WorkItemDependencyNodeType Type, Guid Id)>();
        var cursor = target;
        while (cursor != start)
        {
            path.Add(cursor);
            cursor = predecessorOf[cursor];
        }

        path.Add(start);
        path.Reverse();
        path.Add(start);

        return path;
    }

    // downstream -> [upstreams that block it].
    private static Dictionary<(WorkItemDependencyNodeType Type, Guid Id), List<(WorkItemDependencyNodeType Type, Guid Id)>> BuildIncoming(
        IReadOnlyList<WorkItemDependency> edges)
    {
        var incoming = new Dictionary<(WorkItemDependencyNodeType Type, Guid Id), List<(WorkItemDependencyNodeType Type, Guid Id)>>();

        foreach (var edge in edges)
        {
            if (edge.Kind != WorkItemDependencyKind.Blocking)
            {
                continue;
            }

            var downstream = (edge.DownstreamType, edge.DownstreamId);
            var upstream = (edge.UpstreamType, edge.UpstreamId);

            if (!incoming.TryGetValue(downstream, out var blockers))
            {
                blockers = new List<(WorkItemDependencyNodeType Type, Guid Id)>();
                incoming[downstream] = blockers;
            }

            blockers.Add(upstream);
        }

        return incoming;
    }

    // upstream -> [downstreams it blocks].
    private static Dictionary<(WorkItemDependencyNodeType Type, Guid Id), List<(WorkItemDependencyNodeType Type, Guid Id)>> BuildOutgoing(
        IReadOnlyList<WorkItemDependency> edges)
    {
        var outgoing = new Dictionary<(WorkItemDependencyNodeType Type, Guid Id), List<(WorkItemDependencyNodeType Type, Guid Id)>>();

        foreach (var edge in edges)
        {
            if (edge.Kind != WorkItemDependencyKind.Blocking)
            {
                continue;
            }

            var upstream = (edge.UpstreamType, edge.UpstreamId);
            var downstream = (edge.DownstreamType, edge.DownstreamId);

            if (!outgoing.TryGetValue(upstream, out var nexts))
            {
                nexts = new List<(WorkItemDependencyNodeType Type, Guid Id)>();
                outgoing[upstream] = nexts;
            }

            nexts.Add(downstream);
        }

        return outgoing;
    }
}
