using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityScript;

namespace horde
{
    class pathfinding : BaseScript
    {
        private static List<pathNode> _nodes = new List<pathNode>();

        public static void initPathNodes()
        {
            List<Vector3> nodes = new List<Vector3>();
            switch (horde._mapname)
            {
                case "mp_dome":
                    nodes.Add(new Vector3(-336, 1442, -286));
                    nodes.Add(new Vector3(-645, 1418, -280));
                    nodes.Add(new Vector3(-449, 977, -283));
                    nodes.Add(new Vector3(226, 961, -310));
                    nodes.Add(new Vector3(5, 1458, -290));
                    nodes.Add(new Vector3(136, 820, -309));
                    nodes.Add(new Vector3(-604, 83, -413));
                    nodes.Add(new Vector3(-220, 183, -398));
                    nodes.Add(new Vector3(-85, 648, -354));
                    nodes.Add(new Vector3(-136, 47, -390));
                    nodes.Add(new Vector3(164, 154, -390));
                    nodes.Add(new Vector3(326, -125, -390));
                    nodes.Add(new Vector3(10, -243, -390));
                    nodes.Add(new Vector3(608, -228, -394));
                    nodes.Add(new Vector3(655, 226, -400));
                    nodes.Add(new Vector3(916, 361, -388));
                    nodes.Add(new Vector3(991, 903, -325));
                    nodes.Add(new Vector3(261, 1135, -311));
                    nodes.Add(new Vector3(620, 1014, -315));
                    nodes.Add(new Vector3(1324, 677, -322));
                    nodes.Add(new Vector3(1272, 3, -394));
                    nodes.Add(new Vector3(810, -395, -395));
                    nodes.Add(new Vector3(-106, 2049, -290));
                    nodes.Add(new Vector3(386, 2061, -254));
                    nodes.Add(new Vector3(750, 2236, -254));
                    nodes.Add(new Vector3(652, 1854, -235));
                    break;
                default:
                    Utilities.PrintToConsole("No pathNodes are defined for this map!");
                    return;
            }

            foreach (Vector3 v in nodes)
                new pathNode(v);
        }
        public static void bakeAllPathNodes()
        {
            List<pathNode> badPoints = new List<pathNode>();
            foreach (pathNode node in _nodes)
            {
                List<pathNode> bakes = new List<pathNode>();
                foreach (pathNode newNode in _nodes)
                {
                    if (newNode == node) continue;//No unneccesary trace
                    if (newNode.location.DistanceTo(node.location) > 2000) continue;//Don't trace for too far away points
                    bool trace = GSCFunctions.SightTracePassed(node.location + new Vector3(0, 0, 5), newNode.location + new Vector3(0, 0, 5), false);
                    if (trace)
                        bakes.Add(newNode);
                }
                if (bakes.Count > 0) node.visibleNodes = bakes;
                else
                    badPoints.Add(node);
            }
            if (badPoints.Count > 0)
            {
                foreach (pathNode p in badPoints)
                {
                    _nodes.Remove(p);
                    Utilities.PrintToConsole("A pathNode had no visible links! (" + p.location.ToString() + ")");
                }
            }
            badPoints.Clear();
        }
        public static void bakePathNode(pathNode node)
        {
            List<pathNode> bakes = new List<pathNode>();
            foreach (pathNode newNode in _nodes)
            {
                if (newNode == node) continue;//No unneccesary trace
                if (newNode.location.DistanceTo(node.location) > 2000) continue;//Don't trace for too far away points
                bool trace = GSCFunctions.SightTracePassed(node.location + new Vector3(0, 0, 5), newNode.location + new Vector3(0, 0, 5), false);
                if (trace)
                    bakes.Add(newNode);
            }
            if (bakes.Count > 0) node.visibleNodes = bakes;
            else
                Utilities.PrintToConsole("A pathNode had no visible links! (" + node.location.ToString() + ")");
        }

        public class pathNode
        {
            public List<pathNode> visibleNodes = new List<pathNode>();
            public Vector3 location;

            public pathNode(Vector3 point)
            {
                location = point;
                _nodes.Add(this);
            }

            public pathNode getClosestPathNodeToEndNode(pathNode node)
            {
                float dis = 999999999;
                pathNode closest = this;

                foreach (pathNode p in node.visibleNodes)
                {
                    if (p.location.DistanceTo2D(location) < dis) closest = p;
                }

                return closest;
            }
            public pathNode getClosestPathNode()
            {
                float dis = 999999999;
                pathNode closest = this;

                foreach (pathNode p in this.visibleNodes)
                {
                    if (p.location.DistanceTo2D(location) < dis) closest = p;
                }

                return closest;
            }

            public List<pathNode> getPathToNode(pathNode node)
            {
                List<pathNode> path = new List<pathNode>();
                //End = node
                float currentDistance = 999999;
                pathNode currentNode = node;
                while (currentDistance > 256)//Risky business...
                {
                    currentNode = currentNode.getClosestPathNodeToEndNode(this);
                    currentDistance = location.DistanceTo2D(currentNode.location);
                    path.Add(currentNode);
                }
                return path;
            }
        }

        public pathNode getClosestNode(Vector3 pos)
        {
            float dis = 999999999;
            pathNode closest = null;
            foreach (pathNode p in _nodes)
            {
                if (p.location.DistanceTo2D(pos) < dis) closest = p;
            }
            return closest;
        }

        public static List<Vector3> getAllNodeLocations()
        {
            List<Vector3> nodes = new List<Vector3>();

            foreach (pathNode node in _nodes)
            {
                if (!nodes.Contains(node.location))
                    nodes.Add(node.location);
            }

            return nodes;
        }
        public static pathNode[] getAllNodes()
            => _nodes.ToArray();
    }
}
