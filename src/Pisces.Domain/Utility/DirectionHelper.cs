﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pisces.Domain.Types;

namespace Pisces.Domain.Utility
{
    public static class DirectionHelper
    {
        public static string GetDirectionKey(DirectionType direction)
        {
            switch (direction)
            {
                case DirectionType.Forward:
                    return "F";
                case DirectionType.Reverse:
                    return "R";
                default:
                    return "S";
            }
        }

        public static DirectionType GetDirection(string directionKey)
        {
            switch (directionKey)
            {
                case "F":
                    return DirectionType.Forward;
                case "R":
                    return DirectionType.Reverse;
                case "S":
                    return DirectionType.Stitched;
                default:
                    throw new Exception(string.Format("Unrecognized direction key '{0}'", directionKey));
            }
        }
    }
}