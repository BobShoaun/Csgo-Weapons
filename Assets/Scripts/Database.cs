using System;
using UnityEngine;

public abstract class Database<TElement, TInstance> : SemiSingletonMonoBehaviour<TInstance> where TElement : IIdentifiable where TInstance : Database<TElement, TInstance> {

	[SerializeField]
	protected TElement[] Elements;

	public TElement this [int id] {
		get {
			TElement result = Array.Find (Elements, element => element.Id == id);
			if (result != null)
				return result;
			Debug.LogError ("Element with given id does not exist: " + id);
			return default (TElement);
		}
	}

	public bool QueryById (int id, ref TElement element) {
		bool querySuccessful = ((element = Array.Find (Elements, _element => _element.Id == id)) != null);
		element = element != null ? element : default (TElement);
		return querySuccessful;
	}

	public TElement this [string name] {
		get { 
			TElement result = Array.Find (Elements, element => element.Name == name);
			if (result != null)
				return result;
			Debug.LogError ("Element with given name does not exist: " + name);
			return default (TElement);
		}
	}

	public bool QueryByName (string name, ref TElement element) {
		bool querySuccessful = ((element = Array.Find (Elements, _element => _element.Name == name)) != null);
		element = element != null ? element : default (TElement);
		return querySuccessful;
	}

}