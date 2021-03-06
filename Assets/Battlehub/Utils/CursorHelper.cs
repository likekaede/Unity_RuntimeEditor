﻿using UnityEngine;
using System.Collections.Generic;

namespace Battlehub.Utils
{
    public enum KnownCursor
    {
        VResize,
        HResize,
        DropNowAllowed,
        DropAllowed
    }

    public static class CursorHelper
    {
        private static object m_locker;

        private static readonly Dictionary<KnownCursor, Texture2D> m_knownCursorToTexture = new Dictionary<KnownCursor, Texture2D>();
        public static void Map(KnownCursor cursorType, Texture2D texture)
        {
            m_knownCursorToTexture[cursorType] = texture;
        }

        public static void Reset()
        {
            m_knownCursorToTexture.Clear();
        }

        public static void SetCursor(object locker, KnownCursor cursorType)
        {
            SetCursor(locker, cursorType, new Vector2(0.5f, 0.5f), CursorMode.Auto);
        }

        public static void SetCursor(object locker, KnownCursor cursorType, Vector2 hotspot, CursorMode mode)
        {
            Texture2D texture;
            if(!m_knownCursorToTexture.TryGetValue(cursorType, out texture))
            {
                texture = null;
            }
            
            SetCursor(locker, texture, hotspot, mode);
        }

        public static void SetCursor(object locker, Texture2D texture)
        {
            SetCursor(locker, texture, new Vector2(0.5f, 0.5f), CursorMode.Auto);
        }

        public static void SetCursor(object locker, Texture2D texture, Vector2 hotspot, CursorMode mode)
        {
            if (m_locker != null && m_locker != locker)
            {
                return;
            }

            if(texture != null)
            {
                hotspot = new Vector2(texture.width * hotspot.x, texture.height * hotspot.y);
            }

            m_locker = locker;
            Cursor.SetCursor(texture, hotspot, mode);
        }

        public static void ResetCursor(object locker)
        {            
            if (m_locker != locker)
            {
                return;
            }

            m_locker = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

    }

}
