using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    public enum SupplyItem {
        Food, Sock
    }

    /// <summary>
    /// Supply point provides food + weapons.
    /// </summary>
    public class ResupplyPoint : IVisual, IIdentified {
        // Generate a sock or some food on alternate turns, every /ResupplyDelay/ turns.
        // There can be a maximum of 6 items at a ResupplyPoint.
        uint t = 0; // turn-tracking.
        uint w = 0; // tracks what was generated last.
        List<SupplyItem> stored = new List<SupplyItem>(WorldConstants.ResupplyPointCapacity);

        public Position Position { get; private set; }
        public double Radius { get; private set; }

        internal ResupplyPoint(uint id, int x, int y) {
            Radius = 1.5;
            Position = new Position(x, y);
            Id = id;
        }

        internal void Update() {
            if (++t % WorldConstants.ResupplyDelay != 0) return;
            if (stored.Count == WorldConstants.ResupplyPointCapacity) return; // already at max storage.
            stored.Add(++w % 2 == 0 ? SupplyItem.Food : SupplyItem.Sock);
        }

        public SupplyItem[] Available { get { return stored.ToArray(); } }

        string IVisual.Texture { get { return "supply"; } }

        internal void Remove(SupplyItem item) {
            throw new NotImplementedException();
        }

        public uint Id { get; private set; }
    }
}