﻿using Giant.Battle;
using Giant.Core;
using UnityEngine;

namespace Server.Zone
{
    partial class Player
    {
        public PlayerUnit PlayerUnit { get; private set; }
        public MapScene OriginalMap { get; private set; }
        public MapScene CurrMap { get; private set; }

        public void EnterWorld()
        {
            PlayerUnit = ComponentFactory.Create<PlayerUnit, MapScene, Player>(CurrMap, this);

            EnterMap(MapId);
        }

        public void EnterMap(int mapId)
        {
            //MapComponent map = MapManangerComponent.Instance.GetMap(mapId);
            MapScene aimMap = null;
            if (aimMap == null) return;

            if (CurrMap != null)
            {
                LeaveMap();
            }

            SetCurrMap(aimMap);

            aimMap.OnUnitEnter(PlayerUnit);
        }

        public void LeaveMap()
        {
            if (CurrMap != null)
            {
                CurrMap.OnUnitEnter(PlayerUnit);
            }

            SetCurrMap(null);
        }

        private void SetCurrMap(MapScene map)
        {
            CurrMap = map;
        }
    }
}
