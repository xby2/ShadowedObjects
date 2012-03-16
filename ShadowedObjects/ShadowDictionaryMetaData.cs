using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ShadowedObjects
{
    public class ShadowDictionaryMetaData<TKey, TValue> : ShadowMetaData
    {
        protected override void ResetProperty(object instance, object propertyName)
        {
            if (propertyName is TKey)
            {
                TKey key = (TKey)propertyName;
                IDictionary<TKey, TValue> dict = instance as IDictionary<TKey, TValue>;
                if (!Originals.ContainsKey(propertyName))
                { return; }

                TValue origValue = (TValue)Originals[propertyName];
                if (origValue == null)
                {
                    dict.Remove(key);
                }
                else
                {
                    dict[key] = origValue;
                }

                Originals.Remove(propertyName);
                if (Originals.Count < 1)
                {
                    HasDirectChanges = false;
                }
            }
        }

		#region Show Changes
        public override string ListChanges<T>(T instance)
        {
            StringBuilder changes = new StringBuilder();
            Dictionary<object, object>.KeyCollection originalKeys = Originals.Keys;
            foreach (object originalKey in originalKeys)
            {
                if (originalKey is TKey)
                {
                    TKey key = (TKey)originalKey;
                    IDictionary<TKey, TValue> dict = instance as IDictionary<TKey, TValue>;
                    object originalValue = Originals[key];
                    object currentValue = null;
                    if (dict.ContainsKey(key))
                    {
                        currentValue = dict[key];
                    }

                    if (currentValue == null && originalValue != null)
                    {
                        changes.AppendLine(string.Format("Removed element {0}: {1}", key.ToString(), originalValue.ToString()));
                    }
                    else if (originalValue == null && currentValue != null)
                    {
                        changes.AppendLine(string.Format("Added element {0}: {1}", key.ToString(), currentValue.ToString()));
                    }
                    else
                    {
                        changes.AppendLine(string.Format("Changed element {0}: from {1} to {2}", key.ToString(), originalValue.ToString(), currentValue.ToString()));
                    }

                    if (currentValue is IShadowObject)
                    {
                        changes.AppendLine(string.Format("{0} changed: ", key.ToString()));
                        changes.Append(currentValue.ListChanges());
                    }
                }
            }
            // Now go through all the children and list the changes from them.
            Dictionary<object, object>.KeyCollection childKeys = Children.Keys;
            foreach (object childKey in childKeys)
            {
                object childValue = Children[childKey];
                if (childValue is IShadowObject)
                {
                    if (childValue.HasChanges())
                    {
                        changes.AppendLine(string.Format("{0} changed: ", childKey.ToString()));
                        changes.Append(childValue.ListChanges());
                    }
                }
            }

            return changes.ToString();
        }

        public override IDictionary<object, ChangeType> GetDictionaryChanges<T>(T instance)
        {
            IDictionary<object, ChangeType> changeSet = new Dictionary<object, ChangeType>();

            Dictionary<object, object>.KeyCollection originalKeys = Originals.Keys;
            foreach (object originalKey in originalKeys)
            {
                if (originalKey is TKey)
                {
                    TKey key = (TKey)originalKey;
                    IDictionary<TKey, TValue> dict = instance as IDictionary<TKey, TValue>;
                    object originalValue = Originals[key];
                    object currentValue = null;
                    if (dict.ContainsKey(key))
                    {
                        currentValue = dict[key];
                    }

                    if (currentValue == null && originalValue != null)
                    {
                        changeSet.Add(key, ChangeType.Remove);
                    }
                    else if (originalValue == null && currentValue != null)
                    {
                        changeSet.Add(key, ChangeType.Add);
                    }
                    else
                    {
                        changeSet.Add(key, ChangeType.Edit);
                    }
                }
            }
            
            // Now go through all the children and list the changes from them.
            Dictionary<object, object>.KeyCollection childKeys = Children.Keys;
            foreach (object childKey in childKeys)
            {
                object childValue = Children[childKey];
                if (childValue is IShadowObject)
                {
                    if (childValue.HasChanges() && !changeSet.ContainsKey(childKey))
                    {
                        changeSet.Add(childKey, ChangeType.Edit);
                    }
                }
            }

            return changeSet;
        }
		#endregion

        public override object GetOriginal<T>(T instance, object propertyKey)
        {
            if (propertyKey is TKey)
            {
                TKey key = (TKey)propertyKey;
                IDictionary<TKey, TValue> dict = instance as IDictionary<TKey, TValue>;
                if (Originals.ContainsKey(propertyKey))
                {
                    return Originals[propertyKey];
                }
                else if (dict.ContainsKey(key))
                {
                    return dict[key];
                }
            }
            return null;
        }
    }
}
