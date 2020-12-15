using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ChangeTracking : IDisposable, IChangeTracking
    {
        public ChangeTracking()
        {
            RawString = "{}";
        }

        [JsonProperty]
        public virtual string RawString
        {
            get
            {
                this.Accepet();
                return RootElement.GetRawText();
            }
            set
            {
                if (json != null)
                    json.Dispose();
                json = JsonDocument.Parse(value);
                RootElement = json.RootElement;
            }
        }

        public static implicit operator ChangeTracking(string rawString)
        {
            return new ChangeTracking() { RawString = rawString };
        }

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
                SyncAllChanges();
                return _ChangeTracking.Count > 0;
            }
        }

        private JsonDocument json;

        internal JsonElement RootElement;

        private readonly Dictionary<string, object> _ChangeTracking = new Dictionary<string, object>();

        private void SyncAllChanges()
        {
            long ver = DateTime.Now.Ticks;
            bool hasChanged = false;
            Type IChangeTrackingType = typeof(IChangeTracking);
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(this))
            {
                if (IChangeTrackingType.IsAssignableFrom(descriptor.PropertyType))
                {
                    var v = descriptor.GetValue(this) as IChangeTracking;
                    if (v == null || v.TrackingVersion != this._OldVersion)
                    {
                        if (_ChangeTracking.ContainsKey(descriptor.DisplayName))
                            _ChangeTracking[descriptor.DisplayName] = v;
                        else
                            _ChangeTracking.Add(descriptor.DisplayName, v);
                        hasChanged = true;
                    }
                }
            }
            if (hasChanged)
                this.TrackingVersion = ver;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>
        /// true: there have changes
        /// false: ther have no changes
        /// </returns>
        public bool Accepet(long newVersion = 0)
        {
            if (!HasChanged)
                return false;
            if (newVersion == 0)
                newVersion = this.TrackingVersion;
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            foreach (var item in this.RootElement.EnumerateObject())
            {
                if (_ChangeTracking.TryGetValue(item.Name, out object value))
                {
                    _ChangeTracking.Remove(item.Name);
                    if (value == null)// this property was removed
                        continue;
                    WriteValue(newVersion, writer, item.Name, value);
                }
                else
                {
                    item.WriteTo(writer);
                }
            }
            // for new added property
            foreach (var item in _ChangeTracking)
            {
                WriteValue(newVersion, writer, item.Key, item.Value);
            }
            writer.WriteEndObject();
            writer.Flush();
            this.RawString = Encoding.UTF8.GetString(ms.ToArray());
            this._ChangeTracking.Clear();
            this.TrackingVersion = newVersion;
            return true;
        }

        private static void WriteValue(long newVersion, Utf8JsonWriter writer, string name, object value)
        {
            if (value is JsonElement json)
            {
                writer.WritePropertyName(name);
                json.WriteTo(writer);
            }
            else if (value is int i)
                writer.WriteNumber(name, i);
            else if (value is bool b)
                writer.WriteBoolean(name, b);
            else if (value is string s)
                writer.WriteString(name, s);
            else if (value is IChangeTracking c)
            {
                c.Accepet(newVersion);
                writer.WriteRawString(name, c.ToString());
            }
            else if (value is JsonValue j)
            {
                writer.WriteRawString(name, j.ToString());
            }
            else if (value != null)
                throw new NotSupportedException($"{value.GetType().FullName} is not supported in ChangeJsonValue method");
        }

        public virtual void Change(object value, string name)
        {
            _ChangeTracking[name] = value;
            _NewVersion = DateTime.Now.Ticks;
        }

        public void Dispose()
        {
            if (json != null)
                json.Dispose();
        }

        public override string ToString()
        {
            return this.RawString;
        }
    }
}