using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
// using Myra.Graphics2D.UI;
using CS4620IS.Components;

namespace CS4620IS;

public enum OneWay
{
    None = 0,
    Right = 1,
    Left = 2
}

public class PathSegmentSystems
{    
    public static VertexPositionColor[] GenerateRoadOutline(Vector3[] path, bool isLaneRuler)
    {
        Color color = isLaneRuler == true ? Color.Red : Color.White;
        VertexPositionColor[] vertexPositionColor = new VertexPositionColor[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            vertexPositionColor[i] = new VertexPositionColor(path[i], color);
            //points[0] = new VertexPositionColor(p1, color);
        }

        return vertexPositionColor;
    }

    // public static void DebugDraw()
    // {
    //     effect.View = viewMatrix;
    //     effect.Projection = projectionMatrix;
    //     effect.DiffuseColor = new Vector3(1f, 1f, 1f);
    //     effect.CurrentTechnique.Passes[0].Apply();
    //
    //     for (int i = 0; i < Points.Length - 1; i++)
    //     {
    //         graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{Points[i], Points[i + 1]}, 0, 1);
    //     }
    //     //
    //     // effect.DiffuseColor = new Vector3(0, 0, 1f);
    //     // effect.CurrentTechnique.Passes[0].Apply();
    //     // for (int i = 0; i < connectedSegments.Count; i++)
    //     // {
    //     //     PathSegment connectedSegment = connectedSegments[i];
    //     //     int connectedFromIndex = connectedIndex[i * 2];
    //     //     int connectedToIndex = connectedIndex[i * 2 + 1];
    //     //
    //     //     if (connectedFromIndex > 0)
    //     //     {
    //     //         graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{points[connectedIndex[i * 2]], points[connectedIndex[i * 2] - 1]}, 0, 1);
    //     //     }
    //     //     
    //     //     if (connectedFromIndex < Path.Length - 1)
    //     //     {
    //     //         graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{points[connectedIndex[i * 2]], points[connectedIndex[i * 2] + 1]}, 0, 1);
    //     //     }
    //     //     //graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{connectedSegments[i].points[connectedIndex[i * 2]], points[connectedIndex[i * 2 + 1]]}, 0, 1);
    //     //     //graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{points[connectedIndex[i * 2]], points[connectedIndex[i * 2] + 1]}, 0, 1);
    //     // }
    // }

    // public void AddConnectedSegment(PathSegment segment, int connectedFromIndex, int connectedToIndex)
    // {
    //     //connectedSegments.Add(segment);
    //     connectedIndex.Add(connectedFromIndex);
    //     connectedIndex.Add(connectedToIndex);
    // }
}