using System;
using System.Collections;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class ZoneCollection : IList<string>
    {
        private readonly List<string> _List = new List<string>();

        public string this[int index] { get { return _List[index]; } set { _List[index] = value; Change(null, null); } }

        public int Count => _List.Count;

        public bool IsReadOnly => false;

        private long _OldVersion;
        private long _NewVersion;

        public long TrackingVersion
        {
            get { return _NewVersion; }
            set { _OldVersion = _NewVersion = value; }
        }

        public bool HasChanged
        {
            get
            {
                return _NewVersion == _OldVersion;
            }
        }


        public bool Accepet(long newVersion = 0)
        {
            this.TrackingVersion = newVersion;
            return true;
        }

        public void Add(string item)
        {
            _List.Add(item);
            Change(null, null);
        }

        public void Change(object value, string name = "")
        {
            this._NewVersion = DateTime.Now.Ticks;
        }

        public void Clear()
        {
            _List.Clear();
            Change(null, null);
        }

        public bool Contains(string item)
        {
            return _List.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _List.CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _List.GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return _List.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _List.Insert(index, item);
            Change(null, null);
        }

        public bool Remove(string item)
        {
            Change(null, null);
            return _List.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _List.RemoveAt(index);
            Change(null, null);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _List.GetEnumerator();
        }
        public override string ToString()
        {
            if (_List.Count == 0)
                return "[]";
            return $"[\"{string.Join("\",\"", _List)}\"]";
        }
    }
}
