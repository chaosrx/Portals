using System.Reflection;
using UnityEngine;

public static class ExtensionMethods {
    /// <summary>
    /// Returns the Manhattan Distance of two points. Always accurate when comparing which is farther, always overshoots.
    /// </summary>
    /// <param name="value">First point</param>
    /// <param name="other">Second point</param>
    /// <returns></returns>
    public static float CheapDistance(this Vector3 value, Vector3 other) {
        return Mathf.Abs(value.x - other.x) +
            Mathf.Abs(value.y - other.y) +
            Mathf.Abs(value.z - other.z);
    }

    /// <summary>
    /// Sets the layer of the object, as well as the layers of its children
    /// </summary>
    /// <param name="obj">The object to set</param>
    /// <param name="layer">The layer to set</param>
    public static void SetLayerRecursively(this GameObject obj, int layer) {
        obj.layer = layer;

        foreach (Transform child in obj.transform) {
            child.gameObject.SetLayerRecursively(layer);
        }
    }

    public static void SetTagRecursively(this GameObject obj, string tag) {
        obj.tag = tag;

        foreach (Transform child in obj.transform) {
            child.gameObject.SetTagRecursively(tag);
        }
    }

    /// <summary>
    /// Returns the path of the gameobject in the form of object/child1/child2/...etc, starting from the transform the method is called upon. Does not cap with a final /.
    /// </summary>
    /// <param name="current">The transform to start on</param>
    /// <param name="goal">The transform to find</param>
    /// <returns></returns>
    public static string GetPath(this Transform current, Transform goal) {
        if (!current)
            return null;
        if (current.parent == null || current.parent == goal)
            return "/" + current.name;
        return current.parent.GetPath(goal) + "/" + current.name;
    }

    /// <summary>
    /// Normalizes a rect to fit within a second rect
    /// </summary>
    /// <param name="rectIn"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public static Rect FitRect(this Rect rectIn, Rect bounds) {
        //Left
        if (rectIn.xMin < bounds.xMin)
            rectIn.xMin = bounds.xMin;
        //Right
        if (rectIn.xMax > bounds.xMax)
            rectIn.xMax = bounds.xMax;
        //Top
        if (rectIn.yMax > bounds.yMax)
            rectIn.yMax = bounds.yMax;
        //Bottom
        if (rectIn.yMin < bounds.yMin)
            rectIn.yMin = bounds.yMin;
        //edge case
        if (rectIn.yMax <= 0 || rectIn.xMax <= 0)
            return bounds;
        return rectIn;
    }

    /// <summary>
    /// Using reflection, attempts to copy all attributes of a given component into the component the method is called upon. Typically works. Will spam the console with warnings, thanks to Unity.
    /// </summary>
    /// <typeparam name="T">The type of the component</typeparam>
    /// <param name="comp">The original component</param>
    /// <param name="other">The component to copy</param>
    /// <returns></returns>
    public static T GetCopyOf<T>(this Component comp, T other) where T : Component {
        System.Type type = comp.GetType();
        if (type != other.GetType()) return null; // type mis-match
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        //Previously: PropertyInfo[] pinfos = type.GetProperties(flags);
        PropertyInfo[] pinfos = type.GetProperties();
        foreach (var pinfo in pinfos) {
            if (pinfo.CanWrite) {
                try {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                } catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos) {
            try {
                finfo.SetValue(comp, finfo.GetValue(other));
            } catch { }//Occasionally, components do not want to allow copying of values due to certain values only being settable from within certain methods
        }
        return comp as T;
    }
}
