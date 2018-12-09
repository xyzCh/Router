using System;
using System.Threading;
using System.Collections.Generic;

namespace RoutingList
{

    internal class routerTable
    {
        private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        private Dictionary<string, Routing> _cache = new Dictionary<string, Routing>();

        public Routing ReadRouting(string url)
        {
            cacheLock.EnterReadLock();
            try
            {
                Routing routing=null;
                _cache.TryGetValue(url, out routing);
                return routing;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public RoutingAction ReadAction(string url,string key) {
            cacheLock.EnterReadLock();
            try
            {
                RoutingAction action = null;
                _cache[url].actions.TryGetValue(key,out action);
                return action;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public void AddRouting(string url, Routing routing)
        {
            cacheLock.EnterWriteLock();
            try
            {
                _cache[url] = routing;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public void AddOrUpdateAction(string url,string key, RoutingAction action)
        {
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                RoutingAction result = null;
                if (_cache[url].actions.TryGetValue(key,out result))
                {
                    {
                        cacheLock.EnterWriteLock();
                        try
                        {
                            result.action = action.action;
                            result.actionName = action.actionName;
                            result.IsDefAction = action.IsDefAction;
                            result.modifyDate = action.modifyDate;
                        }
                        finally
                        {
                            cacheLock.ExitWriteLock();
                        }
                    }
                }
                else
                {
                    cacheLock.EnterWriteLock();
                    try
                    {
                        _cache[url].actions.Add(key, action);
                    }
                    finally
                    {
                        cacheLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        ~routerTable()
        {
            if (cacheLock != null) cacheLock.Dispose();
        }
    }
}
