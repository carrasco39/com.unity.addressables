using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement
{
    class ChainOperation<TObject, TObjectDependency> : AsyncOperationBase<TObject>
    {
        AsyncOperationHandle<TObjectDependency> m_DepOp;
        AsyncOperationHandle<TObject> m_WrappedOp;
        DownloadStatus m_depStatus = default;
        DownloadStatus m_wrapStatus = default;
        Func<AsyncOperationHandle<TObjectDependency>, AsyncOperationHandle<TObject>> m_Callback;
        Action<AsyncOperationHandle<TObject>> m_CachedOnWrappedCompleted;
        bool m_ReleaseDependenciesOnFailure = true;
        
        public ChainOperation()
        {
            m_CachedOnWrappedCompleted = OnWrappedCompleted;
        }

        protected override string DebugName { get { return $"ChainOperation<{typeof(TObject).Name},{typeof(TObjectDependency).Name}> - {m_DepOp.DebugName}"; } }

        protected override void GetDependencies(List<AsyncOperationHandle> deps)
        {
            if (m_DepOp.IsValid())
                deps.Add(m_DepOp);
        }

        public void Init(AsyncOperationHandle<TObjectDependency> dependentOp, Func<AsyncOperationHandle<TObjectDependency>, AsyncOperationHandle<TObject>> callback, bool releaseDependenciesOnFailure)
        {
            m_DepOp = dependentOp;
            m_DepOp.Acquire();
            m_Callback = callback;
            m_ReleaseDependenciesOnFailure = releaseDependenciesOnFailure;
            RefreshDownloadStatus();
        }

        internal override bool InvokeWaitForCompletion()
        {
            if (IsDone)
                return true;

            if (!m_DepOp.IsDone)
                m_DepOp.WaitForCompletion();

            m_RM?.Update(Time.deltaTime);

            if (!HasExecuted)
                InvokeExecute();

            if (!m_WrappedOp.IsValid())
                return m_WrappedOp.IsDone;
            m_WrappedOp.WaitForCompletion();
            return m_WrappedOp.IsDone;
        }

        protected override void Execute()
        {
            m_WrappedOp = m_Callback(m_DepOp);
            m_WrappedOp.Completed += m_CachedOnWrappedCompleted;
            m_Callback = null;
        }

        private void OnWrappedCompleted(AsyncOperationHandle<TObject> x)
        {
            string errorMsg = string.Empty;
            if (x.Status == AsyncOperationStatus.Failed)
                errorMsg = string.Format("ChainOperation of Type: {0} failed because dependent operation failed\n{1}", typeof(TObject), x.OperationException != null ? x.OperationException.Message : string.Empty);
            Complete(m_WrappedOp.Result, x.Status == AsyncOperationStatus.Succeeded, errorMsg, m_ReleaseDependenciesOnFailure);
        }

        protected override void Destroy()
        {
            if (m_WrappedOp.IsValid())
                m_WrappedOp.Release();

            if (m_DepOp.IsValid())
                m_DepOp.Release();
        }

        internal override void ReleaseDependencies()
        {
            if (m_DepOp.IsValid())
                m_DepOp.Release();
        }

        internal override DownloadStatus GetDownloadStatus(HashSet<object> visited)
        {
            RefreshDownloadStatus(visited);
            return new DownloadStatus() { DownloadedBytes = m_depStatus.DownloadedBytes + m_wrapStatus.DownloadedBytes, TotalBytes = m_depStatus.TotalBytes + m_wrapStatus.TotalBytes, IsDone = IsDone };
        }

        void RefreshDownloadStatus(HashSet<object> visited = default)
        {
            m_depStatus = m_DepOp.IsValid() ? m_DepOp.InternalGetDownloadStatus(visited) : m_depStatus;
            m_wrapStatus = m_WrappedOp.IsValid() ? m_WrappedOp.InternalGetDownloadStatus(visited) : m_wrapStatus;
        }

        protected override float Progress
        {
            get
            {
                DownloadStatus downloadStatus = GetDownloadStatus(new HashSet<object>());
                if (!downloadStatus.IsDone && downloadStatus.DownloadedBytes == 0)
                    return 0.0f;

                float total = 0f;
                int numberOfOps = 2;

                if (m_DepOp.IsValid())
                    total += m_DepOp.PercentComplete;
                else
                    total++;

                if (m_WrappedOp.IsValid())
                    total += m_WrappedOp.PercentComplete;
                else
                    total++;

                return total / numberOfOps;
            }
        }
    }

    class ChainOperationTypelessDepedency<TObject> : AsyncOperationBase<TObject>
    {
        AsyncOperationHandle m_DepOp;
        AsyncOperationHandle<TObject> m_WrappedOp;
        DownloadStatus m_depStatus = default;
        DownloadStatus m_wrapStatus = default;
        Func<AsyncOperationHandle, AsyncOperationHandle<TObject>> m_Callback;
        Action<AsyncOperationHandle<TObject>> m_CachedOnWrappedCompleted;
        bool m_ReleaseDependenciesOnFailure = true;

        public ChainOperationTypelessDepedency()
        {
            m_CachedOnWrappedCompleted = OnWrappedCompleted;
        }

        protected override string DebugName { get { return $"ChainOperation<{typeof(TObject).Name}> - {m_DepOp.DebugName}"; } }

        protected override void GetDependencies(List<AsyncOperationHandle> deps)
        {
            if (m_DepOp.IsValid())
                deps.Add(m_DepOp);
        }

        public void Init(AsyncOperationHandle dependentOp, Func<AsyncOperationHandle, AsyncOperationHandle<TObject>> callback, bool releaseDependenciesOnFailure)
        {
            m_DepOp = dependentOp;
            m_DepOp.Acquire();
            m_Callback = callback;
            m_ReleaseDependenciesOnFailure = releaseDependenciesOnFailure;
            RefreshDownloadStatus();
        }

        internal override bool InvokeWaitForCompletion()
        {
            if (IsDone)
                return true;

            if (!m_DepOp.IsDone)
                m_DepOp.WaitForCompletion();

            m_RM?.Update(Time.deltaTime);

            if (!HasExecuted)
                InvokeExecute();

            if (!m_WrappedOp.IsValid())
                return m_WrappedOp.IsDone;
            m_WrappedOp.WaitForCompletion();
            return m_WrappedOp.IsDone;
        }

        protected override void Execute()
        {
            m_WrappedOp = m_Callback(m_DepOp);
            m_WrappedOp.Completed += m_CachedOnWrappedCompleted;
            m_Callback = null;
        }

        private void OnWrappedCompleted(AsyncOperationHandle<TObject> x)
        {
            string errorMsg = string.Empty;
            if (x.Status == AsyncOperationStatus.Failed)
                errorMsg = string.Format("ChainOperation of Type: {0} failed because dependent operation failed\n{1}", typeof(TObject), x.OperationException != null ? x.OperationException.Message : string.Empty);
            Complete(m_WrappedOp.Result, x.Status == AsyncOperationStatus.Succeeded, errorMsg, m_ReleaseDependenciesOnFailure);
        }

        protected override void Destroy()
        {
            if (m_WrappedOp.IsValid())
                m_WrappedOp.Release();

            if (m_DepOp.IsValid())
                m_DepOp.Release();
        }

        internal override void ReleaseDependencies()
        {
            if (m_DepOp.IsValid())
                m_DepOp.Release();
        }

        internal override DownloadStatus GetDownloadStatus(HashSet<object> visited)
        {
            RefreshDownloadStatus(visited);
            return new DownloadStatus() { DownloadedBytes = m_depStatus.DownloadedBytes + m_wrapStatus.DownloadedBytes, TotalBytes = m_depStatus.TotalBytes + m_wrapStatus.TotalBytes, IsDone = IsDone };
        }

        void RefreshDownloadStatus(HashSet<object> visited = default)
        {
            m_depStatus = m_DepOp.IsValid() ? m_DepOp.InternalGetDownloadStatus(visited) : m_depStatus;
            m_wrapStatus = m_WrappedOp.IsValid() ? m_WrappedOp.InternalGetDownloadStatus(visited) : m_wrapStatus;
        }

        protected override float Progress
        {
            get
            {
                DownloadStatus downloadStatus = GetDownloadStatus(new HashSet<object>());
                if (!downloadStatus.IsDone && downloadStatus.DownloadedBytes == 0)
                    return 0.0f;

                float total = 0f;
                int numberOfOps = 2;

                if (m_DepOp.IsValid())
                    total += m_DepOp.PercentComplete;
                else
                    total++;

                if (m_WrappedOp.IsValid())
                    total += m_WrappedOp.PercentComplete;
                else
                    total++;

                return total / numberOfOps;
            }
        }
    }
}
