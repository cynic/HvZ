using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using HvZ.Common;

namespace HvZ {
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Extensions {
        /// <summary>
        /// Returns true if two items on the map overlap
        /// </summary>
        public static bool Intersects(this ITakeSpace a, ITakeSpace b) {
            return Intersects(a.Position.X, a.Position.Y, a.Radius, b.Position.X, b.Position.Y, b.Radius);
        }

        /// <summary>
        /// Returns the distance after which a walker's path, at the current heading, would probably collide with the specified item.
        /// </summary>
        /// <returns>A positive distance at which a collision would occur, OR a negative number when no collision would occur.</returns>
        internal static double CollisionDistance(this IWalker a, ITakeSpace b, double proposedDistanceForward) {
            if (proposedDistanceForward < 0)
                throw new ArgumentException("proposedDistanceForward can't be negative.", "proposedDistanceForward");
            // if the player goes forward here, would they intersect?
            double distTraveled = 0.0;
            var h = a.Heading.ToRadians();
            double distPerStep = Math.Max(0.05, (Math.Min(WorldConstants.HumanSpeed, WorldConstants.ZombieSpeed) / WorldConstants.StepsPerTurn));
            var distXPerStep = distPerStep * Math.Sin(h);
            var distYPerStep = -distPerStep * Math.Cos(h);
            var curX = a.Position.X;
            var curY = a.Position.Y;
            while (distTraveled < proposedDistanceForward) {
                if (Intersects(curX, curY, a.Radius, b.Position.X, b.Position.Y, b.Radius))
                    return distTraveled;
                distTraveled += distPerStep;
                curX += distXPerStep;
                curY += distYPerStep;
            }
            return -1;
        }

        internal static bool Intersects(this ITakeSpace a, double bx, double by, double bradius) {
            return Intersects(a.Position.X, a.Position.Y, a.Radius, bx, by, bradius);
        }

        internal static bool Intersects(double ax, double ay, double aradius, double bx, double by, double bradius) {
            // thanks to: http://stackoverflow.com/questions/8367512/algorithm-to-detect-if-a-circles-intersect-with-any-other-circle-in-the-same-pla
            var dX = ax - bx;
            var dY = ay - by;
            var distBetween = Math.Sqrt((dX * dX) + (dY * dY));
            var sum = Math.Abs(aradius + bradius);
            //var diff = Math.Abs(a.Radius - radius);
            return /*distBetween >= diff &&*/ distBetween <= sum;
        }

        /// <summary>Calculates straight line distance between two things on the map</summary>
        public static double DistanceFrom(this ITakeSpace a, ITakeSpace b) {
            double distX = a.Position.X - b.Position.X;
            double distY = a.Position.Y - b.Position.Y;
            return Math.Sqrt(distX * distX + distY * distY);
        }

        /// <summary>
        /// Returns true if the player is close enough to interact with (e.g., bite or take socks from) the specified thing on the map
        /// </summary>
        public static bool IsCloseEnoughToInteractWith(this IWalker a, ITakeSpace b) {
            return a.DistanceFrom(b) - a.Radius - b.Radius <= WorldConstants.InteractionDistance;
        }

        /// <summary>
        /// Gives the smallest angle that the walker must turn in order to have a particular heading.
        /// </summary>
        public static double AngleToHeading(this IWalker a, double heading) {
            var desired = (90.0 - heading.PositiveAngle()).ToRadians(); // relative to the x-axis
            double curHeading = (90.0 - a.Heading).ToRadians(); // relative to the x-axis
            Vector vHeading = new Vector(-Math.Cos(curHeading), Math.Sin(curHeading));
            Vector vTarget = new Vector(-Math.Cos(desired), Math.Sin(desired));
            return Vector.AngleBetween(vHeading, vTarget).MinimumAngle();
        }

        /// <summary>Calculates how many degrees a zombie/human should turn to face a particular thing</summary>
        /// <returns>The angle to turn.  A negative number means a left turn, a positive number means a right turn.</returns>
        public static double AngleTo(this IWalker a, ITakeSpace b) {
            /*
             * Thanks to Chris for inspiring me to fix this; my version didn't work, and his version didn't work either.
             * (Atleast for what Yusuf was wanting them to do. XD )
             * 
             * GreedyHuman fails on the Plentiful map with either implementation.
             * On the other hand, this version works well, gives left-or-right turns, and it's short & clean too.
             */
            double distToTarget = a.DistanceFrom(b);
            double curHeading = (90.0 - a.Heading).ToRadians(); // relative to the x-axis
            // the heading intersects a circle with radius /distAB/ at some point, call it C.  Find that point of intersection.
            // x = xA + r cos (heading)
            // y = yA + r sin (heading)
            double xC = a.Position.X + distToTarget * Math.Cos(curHeading);
            double yC = a.Position.Y + -distToTarget * Math.Sin(curHeading);
            // let the built-in vector math sort it all out :-).
            Vector vHeading = new Vector(a.Position.X - xC, a.Position.Y - yC);
            Vector vTarget = new Vector(a.Position.X - b.Position.X, a.Position.Y - b.Position.Y);
            return Vector.AngleBetween(vHeading, vTarget).MinimumAngle();
        }

        /// <summary>Calculates how many degrees a zombie/human should turn to face away from a particular thing</summary>
        public static double AngleAwayFrom(this IWalker a, ITakeSpace b) {
            var angleTo = a.AngleTo(b);
            if (angleTo + 180.0 >= 180.0)
                return (180.0 - angleTo).MinimumAngle();
            return (angleTo + 180.0).MinimumAngle();
        }

        /// <summary>Calculates how many degrees a zombie/human should turn to avoid a particular thing</summary>
        public static double AngleAvoiding(this IWalker a, ITakeSpace b) {
            var angleTo = a.AngleTo(b);
            if (Math.Abs(angleTo + 90.0) > Math.Abs(angleTo - 90.0))
                return (angleTo - 90.0).MinimumAngle();
            return (angleTo + 90.0).MinimumAngle();
        }

        internal static double MinimumAngle(this double x) {
            if (x == 0.0) return x;
            if (Math.Abs(x) < 0.0001) return 0.0001 * Math.Sign(x);
            return x;
        }

        internal static double PositiveAngle(this double x) {
            return (360 + (x % 360)) % 360;
        }

        public static double ToDegrees(this double x) {
            return (x * 180.0) / Math.PI;
        }

        public static double ToRadians(this double x) {
            return (x * Math.PI) / 180.0;
        }
    }
}
