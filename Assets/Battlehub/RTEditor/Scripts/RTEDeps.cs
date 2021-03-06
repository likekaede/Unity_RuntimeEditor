﻿using UnityEngine;

namespace Battlehub.RTEditor
{
    public interface IRTSL2Deps
    {
        IResourcePreviewUtility ResourcePreview
        {
            get;
        }
    }

    [DefaultExecutionOrder(-1)]
    public class RTEDeps : MonoBehaviour, IRTSL2Deps
    {
        public static IRTSL2Deps Get
        {
            get;
            private set;
        }

        private IResourcePreviewUtility m_resourcePreview;
        public IResourcePreviewUtility ResourcePreview
        {
            get { return m_resourcePreview; }
        }

        protected virtual void Awake()
        {
            if (Get != null)
            {
                Destroy(((MonoBehaviour)Get).gameObject);
                Debug.LogWarning("Another instance of RTSL2Deps exist");
            }
            Get = this;
            m_resourcePreview = FindObjectOfType<ResourcePreviewUtility>();
        }

        protected virtual void OnDestroy()
        {
            if (this == (MonoBehaviour)Get)
            {
                Get = null;
                m_resourcePreview = null;
            }
        }
    }

}

