using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PlanetaryExpansion
{
    public class Octree
    {
        private bool isLeafNode;
        //bool isPartitioned;
        float size;
        public Octree[] SubNodes;
        Vector3 location;
        public BoundingBox BoundingBox;
        //public List<int> indices;
        public List<int> Triangles;
        public int Depth;
        public int TotalObjects;
        // public CubeOutline outline;
        
        private BasicEffect basicEffect;

        private Vector3 p1;
        private Vector3 p2;

        public Octree(int depth, Vector3 location, float size)
        {
            p1 = new Vector3(location.X - size / 2, location.Y - size / 2, location.Z - size / 2);
            p2 = new Vector3(location.X + size / 2, location.Y + size / 2, location.Z + size / 2);
            BoundingBox = new BoundingBox(p1, p2);

            this.location = location;
            this.size = size;
            //indices = new List<int>();
            
            if (depth == 0)
            {
                Triangles = new List<int>();
            }
            
            Depth = depth;
            // outline = new CubeOutline(p1, p2, Color.White);
            
            //Console.WriteLine($"Node Created with points P1: {p1.X}, {p1.Y} P2: {p2.X}, {p2.Y}");

            //if (depth > 0)
            //{
                //isPartitioned = true;
            //    CreateSubNodes(depth - 1);
            //}
        }

        // public void InsertData(VertexMultitextured[] vertices)
        // {
        //     for (int i = 0; i < vertices.Count(); i++)
        //     {
        //         InsertIndex(i, vertices[i].Position);
        //     }
        // }

        // public void InsertIndex(int index, Vector3 location)
        // {
        //     if (boundingBox.Contains(location) != ContainmentType.Contains)
        //     {
        //         return;
        //     }
        //     totalObjects += 1;
        //     if (Depth > 0)
        //     {
        //         subNodes[GetSubNodeIndex(location)].InsertIndex(index, location);
        //         //indices.Add(index);
        //     }
        //     else
        //     {
        //         //indices.Add(index);
        //     }
        // }

        public int GetSubNodeIndex(Vector3 location)
        {
            int index = 0;

            index |= location.Z > this.location.Z ? 0 : 4;
            index |= location.Y > this.location.Y ? 0 : 2;
            index |= location.X > this.location.X ? 1 : 0;

            return index;
        }

        public List<Octree> GetSubNode(Ray ray, Octree node, out float? distance)
        {
            distance = null;

            if (node == null || node.Depth == 0)
            {
                Console.WriteLine("returned node");
                return new List<Octree>{node};
            }
            
            List<Octree> possibleNodes = new List<Octree>();
            
            //float? prevDistance = null;
            // Octree returnNode = null;
            for (int i = 0; i < SubNodes.Length; i++)
            {
                //TODO
                //Need to double check this didn't introduce a new bug with adding/removing edited triangles
                //I will need to revisit that anyway and make sure the TotalObjects for parent nodes are 
                //being updated correctly as well as the bottom nodes
                if (node.SubNodes[i].TotalObjects == 0)
                {
                    continue;
                }
                
                float? nodeDistance = ray.Intersects(node.SubNodes[i].BoundingBox);

                if (nodeDistance == null)
                {
                    continue;
                }
                
                // if (node.subNodes[i].indices.Count == 0)
                // {
                //     continue;
                // }
                
                ///new code
                possibleNodes.AddRange(GetSubNode(ray, SubNodes[i], out distance));

                // if (nodeDistance < prevDistance || prevDistance == null)
                // {
                //     prevDistance = nodeDistance;
                //     returnNode = node.subNodes[i];
                // }
            }

            return possibleNodes;
        }

        public List<Octree> GetNodesFromRay(Ray ray, out float? distance)
        {
            distance = ray.Intersects(BoundingBox);
            
            if (distance == null)
            {
                return null;
            }

            return GetSubNode(ray, this, out distance);
        }

        // public Octree GetNodeFromLocation(Vector3 location)
        // {
        //     if (BoundingBox.Contains(location) != ContainmentType.Contains || totalObjects < 1)
        //     {
        //         return null;
        //     }
        //     if (Depth < 1)
        //         return this;
        //     else
        //     {
        //         return SubNodes[GetSubNodeIndex(location)].GetNodeFromLocation(location);
        //     }
        //}

        public void Load(GraphicsDevice graphicsDevice)
        {
            // outline.Load(graphicsDevice);
            if (Depth == 0) return;
            for (int i = 0; i < SubNodes.Length; i++)
            {
                SubNodes[i].Load(graphicsDevice);
            }
        }

        //public void DebugDraw(GraphicsDevice graphicsDevice, ArcBallCamera camera)
        //{
        //    outline.Draw(graphicsDevice, camera);
        //    if (Depth == 0) return;
        //    for (int i = 0; i < SubNodes.Length; i++)
        //    {
        //        SubNodes[i].DebugDraw(graphicsDevice, camera);
        //    }
        //}

        public void CreateSubNodes(int depth)
        {
            SubNodes = new Octree[8];
            float locationOffset = size / 4;
            SubNodes[0] = new Octree(depth, new Vector3(location.X - locationOffset, location.Y + locationOffset, location.Z + locationOffset), size / 2); //top left front
            SubNodes[1] = new Octree(depth, new Vector3(location.X + locationOffset, location.Y + locationOffset, location.Z + locationOffset), size / 2); //top right front
            SubNodes[2] = new Octree(depth, new Vector3(location.X - locationOffset, location.Y - locationOffset, location.Z + locationOffset), size / 2); //bottom left front
            SubNodes[3] = new Octree(depth, new Vector3(location.X + locationOffset, location.Y - locationOffset, location.Z + locationOffset), size / 2); //bottom right front
            SubNodes[4] = new Octree(depth, new Vector3(location.X - locationOffset, location.Y + locationOffset, location.Z - locationOffset), size / 2); //top left back
            SubNodes[5] = new Octree(depth, new Vector3(location.X + locationOffset, location.Y + locationOffset, location.Z - locationOffset), size / 2); //top right back
            SubNodes[6] = new Octree(depth, new Vector3(location.X - locationOffset, location.Y - locationOffset, location.Z - locationOffset), size / 2); //bottom left back
            SubNodes[7] = new Octree(depth, new Vector3(location.X + locationOffset, location.Y - locationOffset, location.Z - locationOffset), size / 2); //bottom right back
        }
    }
}