﻿using Giant.EnumUtil;

namespace Giant.Battle
{
    partial class Unit
    {
        /// <summary>
        /// 向listener 广播消息
        /// </summary>
        public IBattleMsgListener MsgListener { get; protected set; }

        public void BroadcastSimpleInfo()
        { 
        }

        public void Broadcast(Google.Protobuf.IMessage message)
        {
            if (CurrentMap == null) return;
            switch (CurrentMap.MapModel.AOIType)
            {
                case AOIType.All:
                    BroadcastAll(message);
                    break;
                case AOIType.Nearby:
                    BroadcastNearby(message);
                    break;
            }
        }

        private void BroadcastAll(Google.Protobuf.IMessage message)
        {
            CurrentMap.BroadcastMsg(message);
        }

        private void BroadcastNearby(Google.Protobuf.IMessage message)
        { 
        }
    }
}
