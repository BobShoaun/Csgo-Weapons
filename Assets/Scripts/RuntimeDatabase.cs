using System;
using UnityEngine;
using System.Collections.Generic;

public abstract class RuntimeDatabase<TElement, TInstance> : SemiSingletonMonoBehaviour<TInstance> 
	where TElement : IIdentifiable where TInstance : RuntimeDatabase<TElement, TInstance> {

	[SerializeField]
	protected List<TElement> Elements;

	protected override void Awake () {
		base.Awake ();
		Elements = new List<TElement> ();
	}

	public int Add (TElement element) {
		Elements.Add (element);
		return Elements.IndexOf (element);
	}

	public TElement this [int id] {
		get {
			TElement result = Elements.Find (element => element.Id == id);
			if (result != null)
				return result;
			Debug.LogError ("Element with given id does not exist: " + id);
			return default (TElement);
		}
	}

	public bool QueryById (int id, ref TElement element) {
		bool querySuccessful = ((element = Elements.Find (_element => _element.Id == id)) != null);
		element = element != null ? element : default (TElement);
		return querySuccessful;
	}

	public TElement this [string name] {
		get { 
			TElement result = Elements.Find (element => element.Name == name);
			if (result != null)
				return result;
			Debug.LogError ("Element with given name does not exist: " + name);
			return default (TElement);
		}
	}

	public bool QueryByName (string name, ref TElement element) {
		bool querySuccessful = ((element = Elements.Find (_element => _element.Name == name)) != null);
		element = element != null ? element : default (TElement);
		return querySuccessful;
	}

}