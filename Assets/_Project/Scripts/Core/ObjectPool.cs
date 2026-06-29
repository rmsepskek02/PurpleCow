using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : MonoBehaviour, IPoolable
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly List<T> _pool = new List<T>();

    public ObjectPool(T prefab, Transform parent, int initialSize)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNew();
            obj.gameObject.SetActive(false);
            _pool.Add(obj);
        }
    }

    public T Get()
    {
        foreach (T obj in _pool)
        {
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.gameObject.SetActive(true);
                obj.OnSpawn();
                return obj;
            }
        }

        T newObj = CreateNew();
        _pool.Add(newObj);
        newObj.OnSpawn();
        return newObj;
    }

    public void Return(T obj)
    {
        obj.OnDespawn();
        obj.gameObject.SetActive(false);
    }

    private T CreateNew()
    {
        return Object.Instantiate(_prefab, _parent);
    }
}
