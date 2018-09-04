﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityObject = UnityEngine.Object;
namespace Battlehub.RTSaveLoad2
{     
    public interface IIDMap
    {
        long NullID { get; }

        bool IsNullID(long id);
        bool IsInstanceID(long id);
        bool IsStaticResourceID(long id);
        bool IsStaticFolderID(long id);
        bool IsDynamicResourceID(long id);
        bool IsDynamicFolderID(long id);
        bool IsResourceID(long id);

        int ToOrdinal(long id);
        int ToOrdinal(int id);

        long ToExposedResourceID(int ordinal, int id);
        long ToExposedFolderID(int ordinal, int id);
        long ToRuntimeResourceID(int ordinal, int id);
        long ToRuntimeFolderID(int ordinal, int id);

        long ToID(UnityObject uo);
        long[] ToID(UnityObject[] uo);

        bool IsLoaded(long id);
        T FromID<T>(long id) where T : UnityObject;
        T[] FromID<T>(long[] id) where T : UnityObject;
    }

    public interface IIDMapManager : IIDMap
    {
        void LoadMapping(int ordinal, bool IIDtoPID, bool PIDtoObj);
        void UnloadMapping(int ordinal);
        void LoadMapping(bool IIDtoPID, bool PIDtoObj);
    }

    public interface IAssetDB : IIDMapManager
    {
        void RegisterSceneObjects(Dictionary<int, UnityObject> idToObj);
        void UnregisterSceneObjects();

        //void RegisterRuntimeResource(Dictionary<int, UnityObject> idToObj);

        bool IsLibraryLoaded(int ordinal);
        bool LoadLibrary(string assetLibrary, int ordinal);
        void UnloadLibrary(int ordinal);
        AsyncOperation UnloadUnusedAssets(Action<AsyncOperation> completedCallback = null);   
    }

    public class AssetDB : IAssetDB
    {
        private readonly HashSet<AssetLibraryAsset> m_loadedLibraries = new HashSet<AssetLibraryAsset>();
        private readonly Dictionary<int, AssetLibraryAsset> m_ordinalToLib = new Dictionary<int, AssetLibraryAsset>();
        private MappingInfo m_mapping = new MappingInfo();

        private Dictionary<int, UnityObject> m_persistentIDToSceneObject;
        private Dictionary<int, int> m_idToPersistentID;

        public void RegisterSceneObjects(Dictionary<int, UnityObject> idToObj)
        {
            if(m_persistentIDToSceneObject != null)
            {
                Debug.LogWarning("scene objects were not unregistered");
            }
            m_persistentIDToSceneObject = idToObj;
            m_idToPersistentID = m_persistentIDToSceneObject.ToDictionary(kvp => kvp.Value.GetInstanceID(), kvp => kvp.Key);
        }

        public void UnregisterSceneObjects()
        {
            m_persistentIDToSceneObject = null;
            m_idToPersistentID = null;
        }

        public bool IsLibraryLoaded(int ordinal)
        {
            return m_ordinalToLib.ContainsKey(ordinal);
        }

        public bool LoadLibrary(string assetLibrary, int ordinal)
        {
            if (m_ordinalToLib.ContainsKey(ordinal))
            {
                Debug.LogWarningFormat("Asset Library {0} with this same ordinal {1} already loaded", m_ordinalToLib[ordinal].name, ordinal);
                return false;
            }

            AssetLibraryAsset assetLib = Resources.Load<AssetLibraryAsset>(assetLibrary);
            if(assetLib == null)
            {
                Debug.LogWarningFormat("Asset Library {0} not found", assetLibrary);
                return false;
            }

            if (m_loadedLibraries.Contains(assetLib))
            {
                Debug.LogWarningFormat("Asset Library {0} already loadeded", assetLibrary);
                return false;
            }

            assetLib.Ordinal = ordinal;
            m_loadedLibraries.Add(assetLib);
            m_ordinalToLib.Add(ordinal, assetLib);

            return true;
        }

        public void UnloadLibrary(int ordinal)
        {
            AssetLibraryAsset assetLibrary;
            if(m_ordinalToLib.TryGetValue(ordinal, out assetLibrary))
            {
                m_ordinalToLib.Remove(ordinal);
                m_loadedLibraries.Remove(assetLibrary);
                Resources.UnloadAsset(assetLibrary);
            }
        }

        public AsyncOperation UnloadUnusedAssets(Action<AsyncOperation> completedCallback = null)
        {
            AsyncOperation operation = Resources.UnloadUnusedAssets();

            if(completedCallback != null)
            {
                if(operation.isDone)
                {
                    completedCallback(operation);
                }
                else
                {
                    Action<AsyncOperation> onCompleted = null;
                    onCompleted = ao =>
                    {
                        operation.completed -= onCompleted;
                        completedCallback(operation);
                    };
                    operation.completed += onCompleted;
                }
            }
           
            return operation;
        }

        public void LoadMapping(int ordinal, bool IIDtoPID, bool PIDtoObj)
        {
            AssetLibraryAsset assetLib;
            if(m_ordinalToLib.TryGetValue(ordinal, out assetLib))
            {
                assetLib.LoadIDMappingTo(m_mapping, IIDtoPID, PIDtoObj);
            }
            else
            {
                throw new ArgumentException(string.Format("Unable to find assetLibrary with ordinal = {0}", ordinal), "ordinal");
            }
        }

        public void UnloadMapping(int ordinal)
        {
            AssetLibraryAsset assetLib;
            if (m_ordinalToLib.TryGetValue(ordinal, out assetLib))
            {
                assetLib.UnloadIDMappingFrom(m_mapping);
            }
            else
            {
                throw new ArgumentException(string.Format("Unable to find assetLibrary with ordinal = {0}", ordinal), "ordinal");
            }
        }

        public void LoadMapping(bool IIDtoPID, bool PIDtoObj)
        {
            m_mapping = new MappingInfo();
            foreach(AssetLibraryAsset assetLib in m_loadedLibraries)
            {
                assetLib.LoadIDMappingTo(m_mapping, IIDtoPID, PIDtoObj);
            }
        }

        public void UnloadMappings()
        {
            m_mapping = new MappingInfo();
        }

        private const long m_nullID = 1L << 32;
        private const long m_instanceIDMask = 1L << 33;
        private const long staticResourceIDMask = 1L << 34;
        private const long m_staticFolderIDMask = 1L << 35;
        private const long m_dynamicResourceIDMask = 1L << 36;
        private const long m_dynamicFolderIDMask = 1L << 37;

        public long NullID { get { return m_nullID; } }

        public bool IsNullID(long id)
        {
            return (id & m_nullID) != 0;
        }

        public bool IsInstanceID(long id)
        {
            return (id & m_instanceIDMask) != 0;
        }

        public bool IsStaticResourceID(long id)
        {
            return (id & staticResourceIDMask) != 0;
        }

        public bool IsStaticFolderID(long id)
        {
            return (id & m_staticFolderIDMask) != 0;
        }
        
        public bool IsDynamicResourceID(long id)
        {
            return (id & m_dynamicResourceIDMask) != 0;
        }

        public bool IsDynamicFolderID(long id)
        {
            return (id & m_dynamicFolderIDMask) != 0;
        }

        public bool IsResourceID(long id)
        {
            return IsStaticResourceID(id) || IsDynamicResourceID(id);
        }

        public long ToExposedResourceID(int ordinal, int id)
        {
            return ToID(ordinal, id, staticResourceIDMask);
        }

        public long ToExposedFolderID(int ordinal, int id)
        {
            return ToID(ordinal, id, m_staticFolderIDMask);
        }

        public long ToRuntimeResourceID(int ordinal, int id)
        {
            return ToID(ordinal, id, m_dynamicResourceIDMask);
        }

        public long ToRuntimeFolderID(int ordinal, int id)
        {
            return ToID(ordinal, id, m_dynamicFolderIDMask);
        }

        private static long ToID(int ordinal, int id, long mask)
        {
            if (id > AssetLibraryInfo.ORDINAL_MASK)
            {
                throw new ArgumentException("id > AssetLibraryInfo.ORDINAL_MASK");
            }

            id = (ordinal << AssetLibraryInfo.ORDINAL_OFFSET) | (AssetLibraryInfo.ORDINAL_MASK & id);
            return mask | (0x00000000FFFFFFFFL & id);
        }

        public int ToOrdinal(long id)
        {
            int intId = (int)(0x00000000FFFFFFFFL & id);
            return (intId >> AssetLibraryInfo.ORDINAL_OFFSET) & AssetLibraryInfo.ORDINAL_MASK;
            
        }
        public int ToOrdinal(int id)
        {
            return (id >> AssetLibraryInfo.ORDINAL_OFFSET) & AssetLibraryInfo.ORDINAL_MASK;
        }

        public long ToID(UnityObject uo)
        {
            if(uo == null)
            {
                return m_nullID;
            }

            int instanceID = uo.GetInstanceID();
            int persistentID;
            if(m_mapping.InstanceIDtoPID.TryGetValue(instanceID, out persistentID))
            {
                return staticResourceIDMask | (0x00000000FFFFFFFFL & persistentID);
            }
            
            if(m_idToPersistentID != null && m_idToPersistentID.TryGetValue(instanceID, out persistentID))
            {
                return m_instanceIDMask | (0x00000000FFFFFFFFL & persistentID);
            }

            return m_instanceIDMask | (0x00000000FFFFFFFFL & instanceID);
        }

        public long[] ToID(UnityObject[] uo)
        {
            if(uo == null)
            {
                return null;
            }
            long[] ids = new long[uo.Length];
            for(int i = 0; i < uo.Length; ++i)
            {
                ids[i] = ToID(uo[i]);
            }
            return ids;
        }

        public bool IsLoaded(long id)
        {
            if (IsNullID(id))
            {
                return true;
            }
            if (IsStaticFolderID(id))
            {
                return true;
            }
            if (IsDynamicFolderID(id))
            {
                return true;
            }
            if (IsInstanceID(id))
            {
                int persistentID = unchecked((int)id);
                return m_persistentIDToSceneObject.ContainsKey(persistentID);
            }
            if (IsStaticResourceID(id))
            {
                int persistentID = unchecked((int)id);
                return m_mapping.PersistentIDtoObj.ContainsKey(persistentID);
            }
            if(IsDynamicResourceID(id))
            {
                //int persistentID = unchecked((int)id);
                //return
            }
            return false;
        }

        public T FromID<T>(long id) where T : UnityObject
        {
            if(IsNullID(id))
            {
                return null;
            }

            if(IsStaticResourceID(id))
            {
                UnityObject obj;
                int persistentID = unchecked((int)id);
                if (m_mapping.PersistentIDtoObj.TryGetValue(persistentID, out obj))
                {
                    return obj as T;
                }
            }
            else if(IsInstanceID(id))
            {
                UnityObject obj;
                int persistentID = unchecked((int)id);
                if(m_persistentIDToSceneObject.TryGetValue(persistentID, out obj))
                {
                    return obj as T;
                }
            }
            else if(IsDynamicResourceID(id))
            {

            }
            return null;
        }

        public T[] FromID<T>(long[] id) where T : UnityObject
        {
            if(id == null)
            {
                return null;
            }

            T[] objs = new T[id.Length];
            for(int i = 0; i < id.Length; ++i)
            {
                objs[i] = FromID<T>(id[i]);
            }
            return objs;
        }
    }
}
