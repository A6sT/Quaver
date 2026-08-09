using System;
using Quaver.Server.Client.Enums;

namespace Quaver.Shared.Graphics.Notifications
{
    internal sealed class MultiplayerInviteNotificationInfo : NotificationInfo
    {
        internal string SenderName { get; }

        internal ulong SenderSteamId { get; }

        internal UserGroups SenderGroups { get; }

        internal string SenderClanTag { get; }

        internal string SenderClanAccentColor { get; }

        internal EventHandler DeclineAction { get; }

        internal MultiplayerInviteNotificationInfo(string senderName, ulong senderSteamId, UserGroups senderGroups,
            string senderClanTag, string senderClanAccentColor, EventHandler joinAction,
            EventHandler declineAction = null)
            : base(NotificationLevel.Info, $"{senderName} invited you to a multiplayer game.", true, joinAction)
        {
            SenderName = senderName;
            SenderSteamId = senderSteamId;
            SenderGroups = senderGroups;
            SenderClanTag = senderClanTag;
            SenderClanAccentColor = senderClanAccentColor;
            DeclineAction = declineAction;
        }
    }
}
