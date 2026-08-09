using System;
using System.Collections.Generic;

namespace Quaver.Shared.Graphics.Notifications
{
    internal sealed class NotificationHistoryEntry
    {
        internal long Id { get; }

        internal string Identity { get; }

        internal NotificationLevel Level { get; }

        internal NotificationInfo Notification { get; }

        internal string Text { get; }

        internal EventHandler ClickAction { get; }

        internal DateTimeOffset ReceivedAt { get; }

        internal bool IsNew { get; }

        internal NotificationHistoryEntry(long id, string identity, NotificationInfo info, bool isNew)
            : this(id, identity, info, DateTimeOffset.Now, isNew) { }

        private NotificationHistoryEntry(long id, string identity, NotificationInfo info,
            DateTimeOffset receivedAt, bool isNew)
        {
            Id = id;
            Identity = identity;
            Notification = info;
            Level = info.Level;
            Text = info.Text;
            ClickAction = info.ClickAction;
            ReceivedAt = receivedAt;
            IsNew = isNew;
        }

        internal NotificationHistoryEntry MarkSeen() =>
            new NotificationHistoryEntry(Id, Identity, Notification, ReceivedAt, false);
    }

    internal sealed class NotificationHistoryStore
    {
        private object SyncRoot { get; } = new object();

        private LinkedList<NotificationHistoryEntry> Entries { get; } =
            new LinkedList<NotificationHistoryEntry>();

        private Dictionary<string, LinkedListNode<NotificationHistoryEntry>> EntriesByIdentity { get; } =
            new Dictionary<string, LinkedListNode<NotificationHistoryEntry>>();

        private Dictionary<long, LinkedListNode<NotificationHistoryEntry>> EntriesById { get; } =
            new Dictionary<long, LinkedListNode<NotificationHistoryEntry>>();

        private long NextId { get; set; }

        private bool IsHistoryViewOpen { get; set; }

        internal event EventHandler Changed;

        internal void AddOrUpdate(string key, NotificationInfo info)
        {
            var identity = GetIdentity(key, info);

            lock (SyncRoot)
            {
                LinkedListNode<NotificationHistoryEntry> node;
                if (EntriesByIdentity.TryGetValue(identity, out var existingNode))
                {
                    Entries.Remove(existingNode);
                    node = existingNode;
                    node.Value = new NotificationHistoryEntry(node.Value.Id, identity, info, !IsHistoryViewOpen);
                }
                else
                {
                    node = new LinkedListNode<NotificationHistoryEntry>(
                        new NotificationHistoryEntry(++NextId, identity, info, !IsHistoryViewOpen));
                    EntriesByIdentity.Add(identity, node);
                    EntriesById.Add(node.Value.Id, node);
                }

                Entries.AddFirst(node);
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        internal NotificationHistoryEntry[] GetHistory()
        {
            lock (SyncRoot)
            {
                var result = new NotificationHistoryEntry[Entries.Count];
                var index = 0;
                foreach (var entry in Entries)
                    result[index++] = entry;

                return result;
            }
        }

        internal NotificationHistoryEntry[] GetNew()
        {
            lock (SyncRoot)
            {
                var count = 0;
                foreach (var entry in Entries)
                {
                    if (entry.IsNew)
                        count++;
                }

                var result = new NotificationHistoryEntry[count];
                var resultIndex = 0;
                foreach (var entry in Entries)
                {
                    if (entry.IsNew)
                        result[resultIndex++] = entry;
                }

                return result;
            }
        }

        internal void SetHistoryViewOpen(bool open)
        {
            var changed = false;
            lock (SyncRoot)
            {
                IsHistoryViewOpen = open;
                if (open)
                    changed = MarkAllSeenInternal();
            }

            if (changed)
                Changed?.Invoke(this, EventArgs.Empty);
        }

        internal void MarkAllSeen()
        {
            var changed = false;
            lock (SyncRoot)
                changed = MarkAllSeenInternal();

            if (changed)
                Changed?.Invoke(this, EventArgs.Empty);
        }

        internal void Remove(long id)
        {
            var removed = false;
            lock (SyncRoot)
            {
                if (EntriesById.TryGetValue(id, out var node))
                {
                    Entries.Remove(node);
                    EntriesByIdentity.Remove(node.Value.Identity);
                    EntriesById.Remove(id);
                    removed = true;
                }
            }

            if (removed)
                Changed?.Invoke(this, EventArgs.Empty);
        }

        internal void Clear()
        {
            lock (SyncRoot)
            {
                if (Entries.Count == 0)
                    return;

                Entries.Clear();
                EntriesByIdentity.Clear();
                EntriesById.Clear();
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private bool MarkAllSeenInternal()
        {
            var changed = false;
            var node = Entries.First;
            while (node != null)
            {
                if (node.Value.IsNew)
                {
                    node.Value = node.Value.MarkSeen();
                    changed = true;
                }

                node = node.Next;
            }

            return changed;
        }

        private static string GetIdentity(string key, NotificationInfo info)
        {
            if (!string.IsNullOrEmpty(key))
                return $"key:{key}";

            return $"text:{(int) info.Level}:{info.Text}";
        }
    }
}
