﻿using Giant.Core;

namespace Giant.Data
{
    public class HeroModel : IData<HeroModel>
    {
        public int Id { get; private set; }
        public int Attack { get; private set; }
        public int HP { get; private set; }

        public void Bind(Core.DataModel data)
        {
            Id = data.Id;
            Attack = data.GetInt("Attack");
            HP = data.GetInt("HP");
        }
    }
}
