﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Giant.Share
{
    public class IdGenerator
    {
        private static uint startId = 10000 * 10;

        public static uint NewId { get { return ++startId; } }
    }
}
