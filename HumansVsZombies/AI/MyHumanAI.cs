using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;

namespace HumansVsZombies
{
    public class MyHumanAI : IHumanAI
    {
        public void Collision(IHumanPlayer player, Edge edge)
        {
            throw new NotImplementedException();
        }

        public void Collision(IHumanPlayer player, ITakeSpace other)
        {
            throw new NotImplementedException();
        }

        public void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply)
        {
            throw new NotImplementedException();
        }

        public void Failure(string reason)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
        }
    }
}
