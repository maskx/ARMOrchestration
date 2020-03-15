using Dynamitey;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class ResourceCollection : ICollection<Resource>
    {
        private ConcurrentDictionary<string, Resource> _Resources = new ConcurrentDictionary<string, Resource>();
        private ConcurrentDictionary<string, ConcurrentBag<TaskCompletionSource<string>>> _DependsOn = new ConcurrentDictionary<string, ConcurrentBag<TaskCompletionSource<string>>>();

        public bool TryAdd(Resource r)
        {
            var rtv = this._Resources.TryAdd(r.Name, r);
            if (this._DependsOn.TryRemove(r.Name, out ConcurrentBag<TaskCompletionSource<string>> ts))
            {
                foreach (var t in ts)
                {
                    t.SetResult(r.Name);
                }
            }
            return rtv;
        }

        public Resource this[string name]
        {
            get
            {
                return this._Resources[name];
            }
            set
            {
                this.TryAdd(value);
            }
        }

        public int Count { get { return this._Resources.Count; } }

        public bool IsReadOnly => false;

        public bool TryGetValue(string name, out Resource resource)
        {
            return this._Resources.TryGetValue(name, out resource);
        }

        public bool ContainsKey(string name)
        {
            return this._Resources.ContainsKey(name);
        }

        // TODO: need check circle depends
        public void WaitDependsOn(string name)
        {
            if (this._Resources.ContainsKey(name))
                return;
            TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();
            this._DependsOn.AddOrUpdate(name,
                new ConcurrentBag<TaskCompletionSource<string>>() { taskCompletionSource },
                (name, t) =>
                {
                    t.Add(taskCompletionSource);
                    return t;
                });
            taskCompletionSource.Task.Wait();
            return;
        }

        public IEnumerator<Resource> GetEnumerator()
        {
            return this._Resources.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._Resources.Values.GetEnumerator();
        }

        public void Add(Resource item)
        {
            this.TryAdd(item);
        }

        public void Clear()
        {
            this._Resources.Clear();
            this._DependsOn.Clear();
        }

        public bool Contains(Resource item)
        {
            return this._Resources.ContainsKey(item.Name);
        }

        public void CopyTo(Resource[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException();
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException();
            if (array.Length - arrayIndex < this._Resources.Count)
                throw new ArgumentException("The number of elements in the source is greater than the available space from arrayIndex to the end of the destination array");
            int i = arrayIndex;
            foreach (var item in this._Resources.Values)
            {
                array[i] = item;
                i++;
            }
        }

        public bool Remove(Resource item)
        {
            return this._Resources.TryRemove(item.Name, out Resource v);
        }
    }
}