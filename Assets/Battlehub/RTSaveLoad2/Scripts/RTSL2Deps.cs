﻿using UnityEngine;

namespace Battlehub.RTSaveLoad2
{
    public interface IRTSL2Deps
    {
        IIDMap IDMap
        {
            get;
        }

        IAssetDB AssetDB
        {
            get;
        }

        ITypeMap TypeMap
        {
            get;
        }

        IUnityObjectFactory UnityObjFactory
        {
            get;
        }

        ISerializer Serializer
        {
            get;
        }

        IStorage Storage
        {
            get;
        }

        IProject Project
        {
            get;
        }
    }

    [DefaultExecutionOrder(-1)]
    public class RTSL2Deps : MonoBehaviour, IRTSL2Deps
    {
        public static IRTSL2Deps Get
        {
            get;
            private set;
        }

        private IAssetDB m_assetDB;
        public IIDMap IDMap
        {
            get { return m_assetDB; }
        }

        public IAssetDB AssetDB
        {
            get { return m_assetDB; }
        }

        private ITypeMap m_typeMap;
        public ITypeMap TypeMap
        {
            get { return m_typeMap; }
        }

        private IUnityObjectFactory m_unityObjFactory;
        public IUnityObjectFactory UnityObjFactory
        {
            get { return m_unityObjFactory; }
        }

        private ISerializer m_serializer;
        public ISerializer Serializer
        {
            get { return m_serializer; }
        }

        private IStorage m_storage;
        public IStorage Storage
        {
            get { return m_storage; }
        }

        private IProject m_project;
        public IProject Project
        {
            get { return m_project; }
        }

        protected virtual void Awake()
        {
            if(Get != null)
            {
                Destroy(((MonoBehaviour)Get).gameObject);
                Debug.LogWarning("Another instance of RTSL2Deps exist");
            }
            Get = this;
            m_assetDB = new AssetDB();
            m_typeMap = new TypeMap();
            m_unityObjFactory = new UnityObjectFactory();
            m_serializer = new ProtobufSerializer();
            m_storage = new FileSystemStorage();
            m_project = FindObjectOfType<Project>();
        }

        protected virtual void OnDestroy()
        {
            if(this == (MonoBehaviour)Get)
            {
                Get = null;
                m_assetDB = null;
                m_typeMap = null;
                m_unityObjFactory = null;
                m_serializer = null;
                m_storage = null;
                m_project = null;
            }
        }


    }
}