﻿using Giant.Core;
using Giant.EnumUtil;
using Giant.Framework;

namespace Server.Zone
{
    [Event(EventType.InitFrontend)]
    public class AffterFrontendEvent : Event<FrontendComponent>
    {
        public override void Handle(FrontendComponent frontend)
        {
            switch (frontend.AppConfig.AppType)
            {
            }
        }
    }
}
