using System;
using System.Collections.Generic;
using System.Linq;
using CS4620IS.Collision;
// using info.lundin.math;
using Microsoft.Xna.Framework;
using CS4620IS.Components;

namespace CS4620IS;

public class OctreeSuper<T> where T : class
{
    public float Size;
    public byte Depth;
    public Vector3 Location;
    private List<OctreeNode<T>> _nodes = new List<OctreeNode<T>>();
    private List<BoundingBox> _boundingBoxes = new List<BoundingBox>();
    private List<OctreeElement<T>> _elements = new List<OctreeElement<T>>();
    private Dictionary<OctreeElement<T>, List<int[]>> _nodePaths = new Dictionary<OctreeElement<T>, List<int[]>>();

    public OctreeSuper(float size, byte depth, Vector3 location)
    {
        Size = size;
        Depth = depth;
        Location = location;
        
        CreateSubNodes(Depth, Size, Location, null);
    }
    
    private void CreateSubNodes(byte depth, float size, Vector3 position, OctreeNode<T> parent)
    {
        float locationOffset = size / 4.0f;
        int lastCreated = _nodes.Count;
        _nodes.Add(new OctreeNode<T>(depth, size / 2.0f, new Vector3(position.X - locationOffset, position.Y + locationOffset, position.Z + locationOffset)));//top left front
        _nodes.Add(new OctreeNode<T>(depth, size / 2.0f, new Vector3(position.X + locationOffset, position.Y + locationOffset, position.Z + locationOffset)));//top right front
        _nodes.Add(new OctreeNode<T>(depth, size / 2.0f, new Vector3(position.X - locationOffset, position.Y - locationOffset, position.Z + locationOffset)));//bottom left front
        _nodes.Add(new OctreeNode<T>(depth, size / 2.0f, new Vector3(position.X + locationOffset, position.Y - locationOffset, position.Z + locationOffset)));//bottom right front
        _nodes.Add(new OctreeNode<T>(depth, size / 2.0f, new Vector3(position.X - locationOffset, position.Y + locationOffset, position.Z - locationOffset)));//top left back
        _nodes.Add(new OctreeNode<T>(depth, size / 2.0f, new Vector3(position.X + locationOffset, position.Y + locationOffset, position.Z - locationOffset)));//top right back
        _nodes.Add(new OctreeNode<T>(depth, size / 2.0f, new Vector3(position.X - locationOffset, position.Y - locationOffset, position.Z - locationOffset)));//bottom left back
        _nodes.Add(new OctreeNode<T>(depth, size / 2.0f, new Vector3(position.X + locationOffset, position.Y - locationOffset, position.Z - locationOffset)));//bottom right back
        
        for (int i = lastCreated; i < lastCreated + 8; i++)
        {
            OctreeNode<T> node = _nodes[i];
            Vector3 location = node.Position;
            Vector3 p1 = new Vector3(location.X - size / 4, location.Y - size / 4, location.Z - size / 4);
            Vector3 p2 = new Vector3(location.X + size / 4, location.Y + size / 4, location.Z + size / 4); 
            _boundingBoxes.Add(new BoundingBox(p1, p2));
        }
    }
    
    private void InsertElementToLeaf(OctreeNode<T> node, T elementData, int[] nodePath)
    {
        OctreeElement<T> newElement = new OctreeElement<T>
        {
            ElementDataIndex = _elements.Count,
            Data = elementData,
            Parent = node
        };
        _elements.Add(newElement);
        if (node.FirstElement != null)
        {
            newElement.NextElement = node.FirstElement;
        }
        node.FirstElement = newElement;

        if (!_nodePaths.ContainsKey(newElement))
            _nodePaths.Add(newElement, new List<int[]>());
        
        _nodePaths[newElement].Add((int[])nodePath.Clone());
        
        // String nodeString = "[INSERT NODE PATH] NodePath: [ ";
        // foreach (int nodeID in nodePath)
        // {
        //     nodeString += nodeID + " ";
        // }
        // nodeString += "]";
        //
        // Console.WriteLine(elementData + " " + nodeString);
        //Console.WriteLine("TOTAL NODES: " + _nodes.Count);
    }

    private void InsertElementToLeaf(OctreeNode<T> node, T elementData)
    {
        OctreeElement<T> newElement = new OctreeElement<T>
        {
            ElementDataIndex = _elements.Count,
            Data = elementData,
            Parent = node
        };
        _elements.Add(newElement);
        if (node.FirstElement != null)
        {
            newElement.NextElement = node.FirstElement;
        }
        node.FirstElement = newElement;
    }
    
    private void RemoveByNodePath(OctreeElement<T> elementToRemove, int[] nodePath, int currentNode = 0)
    {
        OctreeNode<T> node = _nodes[nodePath[currentNode]];
        
        if (node.Depth == 0)
        {
            OctreeElement<T> previousElement = null;
            OctreeElement<T> currentElement = node.FirstElement;
            while (true)
            {
                if (currentElement == null)
                    break;
                if (currentElement.Equals(elementToRemove))
                {
                    if (previousElement == null)
                        node.FirstElement = currentElement.NextElement;
                    else
                        previousElement.NextElement = currentElement.NextElement;
                    
                    _elements[currentElement.ElementDataIndex] = _elements[^1];
                    _elements[currentElement.ElementDataIndex].ElementDataIndex = currentElement.ElementDataIndex;
                    _elements.RemoveAt(_elements.Count - 1);
                    break;
                }

                previousElement = currentElement;
                currentElement = currentElement.NextElement;
            }
            return;
        }
        
        RemoveByNodePath(elementToRemove, nodePath, currentNode + 1);
    }

    public void RemoveVolumetricElement(OctreeElement<T> octreeElement)
    {
        HashSet<OctreeNode<T>> uniqueNodes = new HashSet<OctreeNode<T>>();
        // Console.WriteLine("[OCTREE REMOVING VOLUMETRIC]");
        for (int i = 0; i < _nodePaths[octreeElement].Count; i++)
        {
            int[] nodePath = _nodePaths[octreeElement][i];
            RemoveByNodePath(octreeElement, nodePath);
            foreach (int nodeIndex in nodePath)
            {
                uniqueNodes.Add(_nodes[nodeIndex]);
            }
            
            // String nodeString = "[NODE PATH] " + i + " [ ";
            // foreach (int node in nodePath)
            // {
            //     nodeString += node + " ";
            // }
            //
            // nodeString += "] ";
            //     
            // Console.WriteLine(nodeString);
        }
        
        _nodePaths.Remove(octreeElement);
        
        ReduceElementCountInNodes(uniqueNodes.ToList());
    }

    public void InsertElementToOctreeFromPlaneSquare(PlaneSquare planeSquare, T elementData, int firstchildNodeIndex = 0, int[] nodePath = null)
    {
        nodePath ??= new int[Depth + 1];

        for (int i = firstchildNodeIndex; i < firstchildNodeIndex + 8; i++)
        {
            OctreeNode<T> node = _nodes[i];
            BoundingBox box = _boundingBoxes[i];

            if (planeSquare.IntersectsCheap(ref box))
            {
                nodePath[node.Depth] = i;
                if (node.Depth == 0)
                {
                    InsertElementToLeaf(node, elementData, nodePath);
                }
                
                node.ElementCount += 1;
                
                if (node.FirstChild == -1 && node.Depth != 0)
                {
                    node.FirstChild = _nodes.Count;
                    CreateSubNodes((byte)(node.Depth - 1), node.Size, node.Position, node);
                }

                if (node.Depth != 0)
                {
                    InsertElementToOctreeFromPlaneSquare(planeSquare, elementData, node.FirstChild, nodePath);
                }
            }
        }
    }

    // public void InsertElementToOctreeFromOOB(BoundingOrientedBox boundingOrientedBox, T elementData, int firstchildNodeIndex = 0, int[] nodePath = null)
    // {
    //     if (nodePath == null)
    //     {
    //         nodePath = new int[Depth + 1];
    //         
    //     }
    //     for (int i = firstchildNodeIndex; i < firstchildNodeIndex + 8; i++)
    //     {
    //         OctreeNode<T> node = _nodes[i];
    //         BoundingBox box = _boundingBoxes[i];
    //
    //         if (boundingOrientedBox.Contains(ref box) != ContainmentType.Disjoint)
    //         {
    //             nodePath[node.Depth] = i;
    //             if (node.Depth == 0)
    //             {
    //                 InsertElementToLeaf(node, elementData, nodePath);
    //             }
    //             
    //             node.ElementCount += 1;
    //             
    //             if (node.FirstChild == -1 && node.Depth != 0)
    //             {
    //                 node.FirstChild = _nodes.Count;
    //                 CreateSubNodes((byte)(node.Depth - 1), node.Size, node.Position, node);
    //             }
    //
    //             if (node.Depth != 0)
    //             {
    //                 InsertElementToOctreeFromOOB(boundingOrientedBox, elementData, node.FirstChild, nodePath);
    //             }
    //             
    //             //keep going because the box might exist in multiple nodes
    //         }
    //     }
    // }

    public void InsertElementToOctreeFromPoint(Vector3 location, T elementData, int firstChildNodeIndex = 0)
    {
        for (int i = firstChildNodeIndex; i < firstChildNodeIndex + 8; i++)
        {
            OctreeNode<T> node = _nodes[i];
            BoundingBox box = _boundingBoxes[i];

            if (box.Contains(location) == ContainmentType.Contains)
            {
                node.ElementCount += 1;
                if (node.Depth == 0)
                {
                    InsertElementToLeaf(node, elementData);
                }
                
                if (node.FirstChild == -1 && node.Depth != 0)
                {
                    node.FirstChild = _nodes.Count;
                    CreateSubNodes((byte)(node.Depth - 1), node.Size, node.Position, node);
                }

                if (node.Depth != 0)
                {
                    InsertElementToOctreeFromPoint(location, elementData, node.FirstChild);
                }
                
                //break out if found since can be in duplicate node if point exists with the x = 0, y = 0 or z = 0
                //If added by point, it should not be allowed to exist in multiple nodes unlike another object
                //that's added by a box or a sphere
                break;
            }
        }
    }

    public List<OctreeElement<T>> GetElementListFromRay(Ray ray)
    {
        List<OctreeElement<T>> elements = new List<OctreeElement<T>>();

        void RecFromLocation(int firstChildNodeIndex)
        {
            for (int i = firstChildNodeIndex; i < firstChildNodeIndex + 8; i++) {
                BoundingBox box = _boundingBoxes[i];
                OctreeNode<T> node = _nodes[i];

                if (node.ElementCount == 0)// || node.FirstChild == -1)
                    continue;

                if (ray.Intersects(box) != null) {
                    if (node.Depth != 0 )
                        RecFromLocation(node.FirstChild);
                    else 
                    if (node.FirstElement == null)
                        //TODO Look into this bug where the first element can be null but the element count is reporting != 0. The count is becoming negative
                        Console.WriteLine("Element Count: " + node.ElementCount + " which is != 0, but first element is null. Something strange happened.");
                    else
                        elements.Add(node.FirstElement);
                }
            }
        }
        
        RecFromLocation(0);
        return elements;
    }

    // public List<OctreeElement<T>> GetElementListFromOOB(BoundingOrientedBox boundingOrientedBox) {
    //     List<OctreeElement<T>> elements = new List<OctreeElement<T>>();
    //     void RecFromLocation(int firstChildNodeIndex)
    //     {
    //         for (int i = firstChildNodeIndex; i < firstChildNodeIndex + 8; i++) {
    //             BoundingBox box = _boundingBoxes[i];
    //             OctreeNode<T> node = _nodes[i];
    //
    //             if (node.ElementCount == 0)// || node.FirstChild == -1)
    //                 continue;
    //
    //             if (boundingOrientedBox.Contains(ref box) != ContainmentType.Disjoint) {
    //                 if (node.Depth != 0 )
    //                     RecFromLocation(node.FirstChild);
    //                 else 
    //                     if (node.FirstElement == null)
    //                         //TODO Look into this bug where the first element can be null but the element count is reporting != 0. The count is becoming negative
    //                         Console.WriteLine("Element Count: " + node.ElementCount + " which is != 0, but first element is null. Something strange happened.");
    //                     else
    //                         elements.Add(node.FirstElement);
    //             }
    //         }
    //     }
    //     RecFromLocation(0);
    //     return elements;
    // }

    public OctreeElement<T> GetElementListFromLocation(Vector3 location)
    {
        OctreeElement<T> element = null;
        void RecFromLocation(int firstChildNodeIndex)
        {
            for (int i = firstChildNodeIndex; i < firstChildNodeIndex + 8; i++)
            {
                BoundingBox box = _boundingBoxes[i];
                OctreeNode<T> node = _nodes[i];

                if (node.ElementCount == 0)// || node.FirstChild == -1)
                    continue;

                if (box.Contains(location) == ContainmentType.Contains)
                {
                    if (node.Depth != 0)
                    {
                        RecFromLocation(node.FirstChild);
                    }
                    else
                    {
                        element = node.FirstElement;
                        return;
                    }
                }
            }
        }
        
        RecFromLocation(0);

        return element;
    }
    
    public List<OctreeElement<T>> GetElementListsFromSphere(Vector3 centerPosition, float radius)
    {
        List<OctreeElement<T>> firstElements = new List<OctreeElement<T>>();
        BoundingSphere boundingSphere = new BoundingSphere(centerPosition, radius);

        void RecFromSphere(int firstChildNodeIndex)
        {
            for (int i = firstChildNodeIndex; i < firstChildNodeIndex + 8; i++)
            {
                BoundingBox box = _boundingBoxes[i];
                OctreeNode<T> node = _nodes[i];
                
                if (node.ElementCount == 0)// || node.FirstChild == -1)
                    continue;

                if (box.Intersects(boundingSphere))
                {
                    if (node.Depth != 0)
                        RecFromSphere(node.FirstChild);
                    else
                        firstElements.Add(node.FirstElement);
                }
            }
        }
        
        RecFromSphere(0);

        return firstElements;
    }

    // private List<OctreeNode<T>> GetIntersectedNodes(Ray ray)
    // {
    //     List<OctreeNode<T>> collidedNodes = new List<OctreeNode<T>>();
    //
    //     void IntersectNode(int firstChildNodeIndex = 0)
    //     {
    //         for (int i = firstChildNodeIndex; i < firstChildNodeIndex + 8; i++)
    //         {
    //             //add to collidedNodes list if intersected
    //             //continue call IntersectNode recursively f new firstChildNode is != -1
    //         }   
    //     }
    // }

    // public OctreeElement GetElementListFromRay(Ray ray)
    // {
    //     
    // }

    /// <summary>
    /// Builds a list of nodes intersected by a location
    /// </summary>
    /// <param name="location">Location inside the octree</param>
    /// <returns>List of nodes intersected by the location. Last node in the list is always the leaf</returns>
    public List<OctreeNode<T>> GetCollidedNodes(Vector3 location)
    {
        List<OctreeNode<T>> collidedNodes = new List<OctreeNode<T>>();
        void IntersectNode(int firstChildNodeIndex = 0)
        {
            for (int i = firstChildNodeIndex; i < firstChildNodeIndex + 8; i++)
            {
                OctreeNode<T> node = _nodes[i];
                BoundingBox box = _boundingBoxes[i];
                if (node.ElementCount == 0)
                    continue;
                
                if (box.Contains(location) == ContainmentType.Contains)
                {
                    collidedNodes.Add(node);
                    if (node.Depth == 0)
                    {
                        break;
                    }

                    if (node.FirstChild != -1)
                    {
                        IntersectNode(node.FirstChild);
                    }
                }
            }
        }
        
        IntersectNode();

        return collidedNodes;
    }

    public void ReduceElementCountInNodes(List<OctreeNode<T>> nodes)
    {
        foreach (OctreeNode<T> node in nodes)
        {
            node.ElementCount -= 1;
        }
    }
    
    public bool RemoveElementAtLocation(Vector3 location, T elementData)
    {
        List<OctreeNode<T>> collidedNodes = GetCollidedNodes(location);
        OctreeNode<T> leaf = collidedNodes[^1];
        OctreeElement<T> element = leaf.FirstElement;
        bool found = false;

        if (element == null)
            return false;

        OctreeElement<T> previousElement = null;

        for (int i = 0; i < leaf.ElementCount; i++)
        {
            OctreeElement<T> existingElement = _elements[element.ElementDataIndex];
            if (existingElement.Data.Equals(elementData))
            {
                found = true;
                ReduceElementCountInNodes(collidedNodes);
                _elements[element.ElementDataIndex] = _elements[^1];
                _elements[element.ElementDataIndex].ElementDataIndex = element.ElementDataIndex;
                _elements.RemoveAt(_elements.Count - 1);

                if (previousElement != null)
                {
                    previousElement.NextElement = element.NextElement;
                }
                else
                {
                    leaf.FirstElement = element.NextElement;
                }
                break;
            }

            previousElement = element;
            element = element.NextElement;   
        }
        
        return found;
    }
}
    
public class OctreeNode<T>
{
    public OctreeNode(byte depth, float size, Vector3 position, OctreeNode<T> parent = null)
    {
        Depth = depth;
        Size = size;
        Position = position;
        Parent = parent;
    }
    public readonly byte Depth;
    public readonly float Size;
    public readonly Vector3 Position;
    public readonly OctreeNode<T> Parent;
    public int FirstChild = -1;
    public OctreeElement<T> FirstElement;
    public int ElementCount;
}
    
public class OctreeElement<T>
{
    public int ElementDataIndex;
    public OctreeElement<T> NextElement;
    public T Data { get; init; }
    public OctreeNode<T> Parent;

    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is OctreeElement<T>) || Data == null)
            return false;
        var other = (OctreeElement<T>)obj;
        return Data.Equals(other.Data);
    }

    public override int GetHashCode()
    {
        return Data.GetHashCode();
    }

    // public override bool Equals(object obj)
    // {
    //     return base.Equals(obj);
    // }

    //public int ElementIndex;
    //public int NextElementIndex;
    // public int EntityID;
    // public int NextEntityID;
}