﻿using Giant.Battle;
using Google.Protobuf;

namespace Server.App
{
    partial class Player : IBattleMsgListener
    {
        public void OnBattleStart()
        {
            //战斗开始
        }

        public void OnBattleStop(MapModel model, BattleResult result)
        {
            //战斗奖励结算
        }

        public void OnBattleEnd()
        {
        }
    }
}
