using System.Collections.Generic;

public class PriorityQueue<T>
{
    private readonly List<(T item, float priority)> _heap = new List<(T item, float priority)>();

    public int Count => _heap.Count;

    public void Clear()
    {
        _heap.Clear();
    }

    public void Enqueue(T item, float priority)
    {
        _heap.Add((item, priority));
        SiftUp(_heap.Count - 1);
    }

    public bool TryDequeue(out T item, out float priority)
    {
        if (_heap.Count == 0)
        {
            item = default;
            priority = 0f;
            return false;
        }

        (item, priority) = _heap[0];

        int last = _heap.Count - 1;
        _heap[0] = _heap[last];
        _heap.RemoveAt(last);

        if (_heap.Count > 0)
            SiftDown(0);

        return true;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;

            if (_heap[parent].priority <= _heap[index].priority)
                return;

            Swap(parent, index);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            int left = index * 2 + 1;
            int right = left + 1;
            int smallest = index;

            if (left < _heap.Count && _heap[left].priority < _heap[smallest].priority)
                smallest = left;

            if (right < _heap.Count && _heap[right].priority < _heap[smallest].priority)
                smallest = right;

            if (smallest == index)
                return;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (_heap[a], _heap[b]) = (_heap[b], _heap[a]);
    }
}
