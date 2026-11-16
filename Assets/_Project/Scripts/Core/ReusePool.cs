using System;
using System.Collections.Generic;

namespace Capstone.App.Core
{
    /// <summary>
    /// A small object pool. Items are created once by a factory and then handed out and taken
    /// back instead of being created and destroyed every time, which avoids garbage-collection
    /// spikes and repeated UI rebuilding on the phone.
    /// Pure C#, so the bookkeeping is covered by EditMode tests; Unity objects use it through
    /// the onGet and onRelease hooks (for example SetActive(true) and SetActive(false)).
    /// </summary>
    public sealed class ReusePool<T> where T : class
    {
        readonly Func<T> create;
        readonly Action<T> onGet;
        readonly Action<T> onRelease;
        readonly Stack<T> idle = new Stack<T>();
        readonly List<T> active = new List<T>();

        public ReusePool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null)
        {
            this.create = create ?? throw new ArgumentNullException(nameof(create));
            this.onGet = onGet;
            this.onRelease = onRelease;
        }

        /// <summary>How many items were ever created. Stays flat once the pool is warm.</summary>
        public int Created { get; private set; }

        public int ActiveCount => active.Count;

        public int IdleCount => idle.Count;

        public IReadOnlyList<T> Active => active;

        public T Get()
        {
            T item = null;
            while (idle.Count > 0 && item == null)
                item = idle.Pop();
            if (item == null)
            {
                item = create();
                Created++;
            }

            active.Add(item);
            onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !active.Remove(item))
                return;
            onRelease?.Invoke(item);
            idle.Push(item);
        }

        /// <summary>Returns every item that is out, newest first.</summary>
        public void ReleaseAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                T item = active[i];
                active.RemoveAt(i);
                onRelease?.Invoke(item);
                idle.Push(item);
            }
        }
    }
}
