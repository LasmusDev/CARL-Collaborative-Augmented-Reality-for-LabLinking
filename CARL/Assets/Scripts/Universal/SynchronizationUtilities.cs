using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Utilities for different commonly used Synchronization functions
/// </summary>
public class SynchronizationUtilities
{
    /// <summary>
    /// Writes a Vector3 to the stream
    /// </summary>
    /// <param name="val">The Vector</param>
    /// <param name="writer">The stream to be written to</param>
    public static void WriteVector3ToStream(Vector3 val, FastBufferWriter writer)
    {
        writer.TryBeginWrite(3 * 4); //3 floats, 4byte each
        writer.WriteValue(val.x);
        writer.WriteValue(val.y);
        writer.WriteValue(val.z);
    }

    /// <summary>
    /// Reads a Vector3 from the stream
    /// </summary>
    /// <param name="reader">The stream to be read from</param>
    /// <returns>The read Vector3</returns>
    public static Vector3 ReadVector3FromStream(FastBufferReader reader)
    {
        reader.TryBeginRead(3 * 4); //3 floats, 4byte each
        reader.ReadValue(out float x);
        reader.ReadValue(out float y);
        reader.ReadValue(out float z);
        return new Vector3(x,y,z);
    }



    /// <summary>
    /// Writes a transforms rot&pos to the given writer
    /// </summary>
    /// <param name="transform">The transform</param>
    /// <param name="writer">The writer to be written to</param>
    /// <param name="local">Whether local or global coordinates are used</param>
    public static void WriteTransformPoseToStream(Transform transform, FastBufferWriter writer, bool local = true)
    {
        writer.TryBeginWrite(7 * 4); //7 floats, 4byte each
        if (local)
        {
            writer.WriteValue(transform.localPosition.x);
            writer.WriteValue(transform.localPosition.y);
            writer.WriteValue(transform.localPosition.z);
            writer.WriteValue(transform.localRotation.x);
            writer.WriteValue(transform.localRotation.y);
            writer.WriteValue(transform.localRotation.z);
            writer.WriteValue(transform.localRotation.w);
        } else  {
            writer.WriteValue(transform.position.x);
            writer.WriteValue(transform.position.y);
            writer.WriteValue(transform.position.z);
            writer.WriteValue(transform.rotation.x);
            writer.WriteValue(transform.rotation.y);
            writer.WriteValue(transform.rotation.z);
            writer.WriteValue(transform.rotation.w);
        }
    }

    /// <summary>
    /// Overwrites a transforms rot&pos with data from the given reader
    /// </summary>
    /// <param name="transform">The transform to be overwritten</param>
    /// <param name="stream">The reader to read from</param>
    /// <param name="local">Whether the read coordinates are applied as local or global</param>
    public static void ReadTransformPoseFromStream(Transform transform, FastBufferReader stream, bool local = true)
    {
        stream.TryBeginRead(7 * 4);     //7 floats, 4byte each
        stream.ReadValue(out float xPos);
        stream.ReadValue(out float yPos);
        stream.ReadValue(out float zPos);
        stream.ReadValue(out float xRot);
        stream.ReadValue(out float yRot);
        stream.ReadValue(out float zRot);
        stream.ReadValue(out float wRot);
        if (local)
        {
            transform.localPosition = new Vector3(xPos, yPos, zPos);
            transform.localRotation = new Quaternion(xRot, yRot, zRot, wRot);
        }
        else
        {
            transform.SetPositionAndRotation(new Vector3(xPos, yPos, zPos), new Quaternion(xRot, yRot, zRot, wRot));
        }
    }

    /// <summary>
    /// Writes a transforms pos, rot and scale into the given stream
    /// </summary>
    /// <param name="transform">The transform to be read from</param>
    /// <param name="writer">The stream to be written to</param>
    /// <param name="local">Whether local or global coordinates are used</param>
    public static void WriteFullTransformToStream(Transform transform, FastBufferWriter writer, bool local = true)
    {
        writer.TryBeginWrite(10 * 4); //10 floats, 4byte each
        if (local)
        {
            writer.WriteValue(transform.localPosition.x);
            writer.WriteValue(transform.localPosition.y);
            writer.WriteValue(transform.localPosition.z);
            writer.WriteValue(transform.localRotation.x);
            writer.WriteValue(transform.localRotation.y);
            writer.WriteValue(transform.localRotation.z);
            writer.WriteValue(transform.localRotation.w);
            writer.WriteValue(transform.localScale.x);
            writer.WriteValue(transform.localScale.y);
            writer.WriteValue(transform.localScale.z);
        }
        else
        {
            writer.WriteValue(transform.position.x);
            writer.WriteValue(transform.position.y);
            writer.WriteValue(transform.position.z);
            writer.WriteValue(transform.rotation.x);
            writer.WriteValue(transform.rotation.y);
            writer.WriteValue(transform.rotation.z);
            writer.WriteValue(transform.rotation.w);
            writer.WriteValue(transform.lossyScale.x);
            writer.WriteValue(transform.lossyScale.y);
            writer.WriteValue(transform.lossyScale.z);
        }
    }

    /// <summary>
    /// Overwrites a transforms rot,pos&scale with data from the given reader
    /// </summary>
    /// <param name="transform">The transform to be overwritten</param>
    /// <param name="stream">The reader to read from</param>
    /// <param name="local">Whether the read coordinates are applied as local or global</param>
    public static void ReadFullTransformFromStream(Transform transform, FastBufferReader stream, bool local = true)
    {
        stream.TryBeginRead(10 * 4); //10 floats, 4byte each
        stream.ReadValue(out float xPos);
        stream.ReadValue(out float yPos);
        stream.ReadValue(out float zPos);
        stream.ReadValue(out float xRot);
        stream.ReadValue(out float yRot);
        stream.ReadValue(out float zRot);
        stream.ReadValue(out float wRot);
        stream.ReadValue(out float xScal);
        stream.ReadValue(out float yScal);
        stream.ReadValue(out float zScal);
        if (local)
        {
            transform.localPosition = new Vector3(xPos, yPos, zPos);
            transform.localRotation = new Quaternion(xRot, yRot, zRot, wRot);
            transform.localScale = new Vector3(xScal, yScal, zScal);
        }
        else
        {
            transform.SetPositionAndRotation(new Vector3(xPos, yPos, zPos), new Quaternion(xRot, yRot, zRot, wRot));
            if (transform.parent != null) //Since lossy scale is not writable, set local scale by multiplying with parent lossy scale to get same lossy scale
            {
                transform.localScale = new Vector3(xScal * transform.parent.lossyScale.x, yScal * transform.parent.lossyScale.y, zScal * transform.parent.lossyScale.z);
            } else
            {
                transform.localScale = new Vector3(xScal, yScal, zScal);
            }
        }
    }

}
