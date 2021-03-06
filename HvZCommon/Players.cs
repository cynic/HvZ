﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    class HumanPlayer : IHumanPlayer {
        uint IIdentified.Id { get { return playerId; } }

        readonly HvZConnection connection;
        readonly uint playerId;
        readonly Map map;

        public HumanPlayer(HvZConnection connection, uint playerId, Map m) {
            this.connection = connection;
            this.playerId = playerId;
            map = m;
        }

        public int InventorySlotsLeft { get { return WorldConstants.MaximumItemsCarried - map.humans[playerId].Items.Length; } }

        public bool IsInSockRange(ITakeSpace other) {
            var dist = other.DistanceFrom(this);
            var sockRange = (WorldConstants.MissileLifespan - 1) * WorldConstants.MissileSpeed;
            return dist <= sockRange;
        }

        bool IWalker.IsStunned { get { return false; } }

        public void Eat() {
            connection.Send(Command.NewEat(playerId));
        }

        public SupplyItem[] Inventory {
            get { return map.humans[playerId].Items; }
        }

        public void TakeFoodFrom(IIdentified place) {
            connection.Send(Command.NewTakeFood(playerId, place.Id));
        }

        public void TakeSocksFrom(IIdentified place) {
            connection.Send(Command.NewTakeSocks(playerId, place.Id));
        }

        public void Throw(double heading) {
            connection.Send(Command.NewThrow(Guid.NewGuid().ToString("N"), playerId, heading.PositiveAngle()));
        }

        // these are common to humans & zombies.  C&P them?
        public void Turn(double degrees) {
            map.SetMovementState(playerId, MoveState.Moving);
            connection.Send(Command.NewTurn(playerId, degrees));
        }
        public void GoForward(double distance) {
            map.SetMovementState(playerId, MoveState.Moving);
            connection.Send(Command.NewForward(playerId, distance));
        }
        public Position Position { get { return map.walkers[playerId].Position; } }
        public double Radius { get { return map.walkers[playerId].Radius; } }
        public double Heading { get { return map.walkers[playerId].Heading; } }
        public int Lifespan { get { return map.walkers[playerId].Lifespan; } }
        public string Name { get { return map.walkers[playerId].Name; } }
        public int MaximumLifespan { get { return map.walkers[playerId].MaximumLifespan; } }
        public double MapHeight { get { return map.Height; } }
        public double MapWidth { get { return map.Width; } }
        public MoveState Movement { get { return map.walkers[playerId].Movement; } }
        public override string ToString() {
            return map.walkers[playerId].ToString();
        }
    }

    class ZombiePlayer : IZombiePlayer {
        uint IIdentified.Id { get { return playerId; } }

        readonly HvZConnection connection;
        readonly uint playerId;
        readonly Map map;

        public ZombiePlayer(HvZConnection connection, uint playerId, Map m) {
            this.connection = connection;
            this.playerId = playerId;
            map = m;
        }

        public void Bite(IIdentified target) {
            connection.Send(Command.NewBite(playerId, target.Id));
        }

        public bool IsStunned { get { return map.zombies[playerId].IsStunned; } }

        // these are common to humans & zombies.  C&P them?
        public void Turn(double degrees) {
            map.SetMovementState(playerId, MoveState.Moving);
            connection.Send(Command.NewTurn(playerId, degrees));
        }
        public void GoForward(double distance) {
            map.SetMovementState(playerId, MoveState.Moving);
            connection.Send(Command.NewForward(playerId, distance));
        }
        public Position Position { get { return map.walkers[playerId].Position; } }
        public double Radius { get { return map.walkers[playerId].Radius; } }
        public double Heading { get { return map.walkers[playerId].Heading; } }
        public int Lifespan { get { return map.walkers[playerId].Lifespan; } }
        public string Name { get { return map.walkers[playerId].Name; } }
        public int MaximumLifespan { get { return map.walkers[playerId].MaximumLifespan; } }
        public double MapHeight { get { return map.Height; } }
        public double MapWidth { get { return map.Width; } }
        public MoveState Movement { get { return map.walkers[playerId].Movement; } }
        public override string ToString() {
            return map.walkers[playerId].ToString();
        }
    }
}
