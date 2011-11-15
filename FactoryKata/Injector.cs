using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;

namespace XpressiveCode.Injection {

    public class Injector {

        public static MapDictionary Mappings { get; private set; }

        static Dictionary<int, object> _constructorCache;
        static MethodInfo _fetchMethod = typeof(MapDictionary).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Instance);

        static Injector() {
            Mappings = new MapDictionary();

            _constructorCache = new Dictionary<int, object>();
        }

        public static T Build<T>() {
            int key = Mappings.GenerateKeyForWarehouse<T>();

            return (T)BuildObject(typeof(T));
        }

        static ConstructorInfo GetConstructorFromCache(int key) {
            if (_constructorCache.ContainsKey(key)) return (ConstructorInfo)_constructorCache[key];
            return null;
        }

        static object InvokeGenericFetchMethod(Type t) {
            return _fetchMethod.MakeGenericMethod(new Type[] { t }).Invoke(Mappings, null);
        }

        static object BuildObject(Type t) {
            if (Mappings.TypeExistsInWarehouse(t)) {
                return InvokeGenericFetchMethod(t);
            }
            
            int key = Mappings.GenerateKeyForWarehouse(t);

            ConstructorInfo ci = GetConstructorFromCache(key) ?? GetHighestMatchingParametersConstructor(key, t);
            if (ci == null) {
                throw new InvalidConstructorSignatureException("Failed to find a suitable constructor for type: " + t.FullName + ". Please double check your mappings to ensure it's parameters have been mapped.");
            }

            List<object> param = new List<object>();

            foreach (ParameterInfo pi in ci.GetParameters()) {
                if (Mappings.TypeExistsInWarehouse(pi.ParameterType)) {
                    param.Add(InvokeGenericFetchMethod(pi.ParameterType));
                } else {
                    param.Add(BuildObject(pi.ParameterType));
                }
            }

            return Activator.CreateInstance(t, param.ToArray());
        }

        static ConstructorInfo GetHighestMatchingParametersConstructor(int key, Type t) {
            ConstructorInfo constructor = null;

            if (t.GetConstructors().Length == 0) return t.GetConstructor(new Type[] { });

            bool isMatch;
            foreach (ConstructorInfo ci in t.GetConstructors()) {
                isMatch = true;
                foreach (ParameterInfo pi in ci.GetParameters()) {
                    isMatch = Mappings.TypeExistsInWarehouse(pi.ParameterType);
                    if (!isMatch) continue;
                }

                if (constructor == null) {
                    constructor = ci;
                } else {
                    if (isMatch && ci.GetParameters().Length > constructor.GetParameters().Length) constructor = ci;
                }
            }

            AddToConstructorCache(key, constructor);
            return constructor;
        }

        static void AddToConstructorCache(int key, ConstructorInfo constructorInfo) {
            lock (_constructorCache) {
                if (_constructorCache.ContainsKey(key)) {
                    _constructorCache[key] = constructorInfo;
                } else {
                    _constructorCache.Add(key, constructorInfo);
                }
            }
        }

        internal static void RemoveFromConstructorCache(int key) {
            if (_constructorCache.ContainsKey(key)) {
                lock (_constructorCache)
                    _constructorCache.Remove(key);
            }
        }

        internal static void ClearConstructorCache() {
            lock (_constructorCache)
                _constructorCache.Clear();
        }
    }

    public class MapDictionary {
        Dictionary<int, Map> _objectWarehouse;
        
        public int Count { get { return _objectWarehouse.Count; } }

        public MapDictionary() {
            _objectWarehouse = new Dictionary<int, Map>();
        }

        public MapStatistics Add<T>(Func<T> func) {
            MapStatistics stats = new MapStatistics();
           
            int key = GenerateKeyForWarehouse<T>();

            Map map = new Map {
                BoundType = typeof(T),
                Function = func,
                Key = key
            };

            if (!TypeExistsInWarehouse(key)) {
                lock (_objectWarehouse) {
                    _objectWarehouse.Add(key, map);
                }
                stats.Added++;
            } else {
                lock (_objectWarehouse) {
                    _objectWarehouse[key] = map;
                }
                stats.Updated++;
                Injector.RemoveFromConstructorCache(key);
            }
            return stats;
        }

        public MapStatistics Update<T>(Func<T> func) {
            return Add<T>(func);
        }

        public MapStatistics Remove<T>() {
            int count = _objectWarehouse.Count;
            int key = GenerateKeyForWarehouse<T>();

            RemoveFromWarehouse(key);
            Injector.RemoveFromConstructorCache(key);

            return new MapStatistics {
                Removed = count - _objectWarehouse.Count
            };
        }

        public MapStatistics Clear() {
            MapStatistics stats = new MapStatistics {
                Removed = _objectWarehouse.Count
            };

            lock (_objectWarehouse)
                _objectWarehouse.Clear();

            Injector.ClearConstructorCache();

            return stats;
        }

        public bool IsMapped<T>() {
            return TypeExistsInWarehouse(typeof(T));
        }

        public IList<Type> Types() {
            List<Type> types = new List<Type>();

            foreach (KeyValuePair<int, Map> kvp in _objectWarehouse) {
                types.Add(
                    kvp.Value.BoundType
                );
            }

            return types;
        }

        T Get<T>() {
            int key = GenerateKeyForWarehouse<T>();

            if (!TypeExistsInWarehouse(key))
                throw new UnMappedTypeException("Mappings does not contain a store for type: " + typeof(T).FullName + ". Please double check your mappings.");

            Func<T> func = (Func<T>)_objectWarehouse[key].Function;
            return func();
        }

        internal void RemoveFromWarehouse(int key) {
            if (TypeExistsInWarehouse(key)) {
                lock (_objectWarehouse) {
                    _objectWarehouse.Remove(key);
                }
            }
        }

        internal int GenerateKeyForWarehouse<T>() {
            return GenerateKeyForWarehouse(typeof(T));
        }

        internal int GenerateKeyForWarehouse(Type t) {
            return t.GetHashCode();
        }

        internal bool TypeExistsInWarehouse(Type t) {
            int key = GenerateKeyForWarehouse(t);
            return TypeExistsInWarehouse(key);
        }

        internal bool TypeExistsInWarehouse(int key) {
            return _objectWarehouse.ContainsKey(key);
        }
    }

    public class Map {
        public int Key { get; set; }
        public Type BoundType { get; set; }
        public object Function { get; set; }
    }

    public class MapStatistics {
        public int Updated { get; set; }
        public int Added { get; set; }
        public int Removed { get; set; }
        public Exception Exception { get; set; }
    }

    public class UnMappedTypeException : Exception {
        public UnMappedTypeException(string message) : base(message) { }
    }

    public class InvalidConstructorSignatureException : Exception {
        public InvalidConstructorSignatureException(string message) : base(message) { }
    }
}