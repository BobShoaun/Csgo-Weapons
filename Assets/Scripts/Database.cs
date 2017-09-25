using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Make this inherit frm IEnumerator or IEnumerable
public abstract class Database<TElement> : ScriptableObject, IEnumerable<TElement>
	where TElement : IIdentifiable {

	[SerializeField]
	private TElement[] elements;

	protected virtual IEnumerable<TElement> Elements { 
		get { return elements; } 
	}

	public TElement this [int id] {
		get {
			TElement result = Array.Find (elements, element => element.Id == id);
			if (result != null)
				return result;
			Debug.LogError ("Element with given id does not exist: " + id);
			return default (TElement);
		}
	}

	public bool QueryById (int id, ref TElement element) {
		bool querySuccessful = ((element = Array.Find (elements, _element => _element.Id == id)) != null);
		element = element != null ? element : default (TElement);
		return querySuccessful;
	}

	public TElement this [string name] {
		get { 
			TElement result = Array.Find (elements, element => element.Name == name);
			if (result != null)
				return result;
			Debug.LogError ("Element with given name does not exist: " + name);
			return default (TElement);
		}
	}

	public bool QueryByName (string name, ref TElement element) {
		bool querySuccessful = ((element = Array.Find (elements, _element => _element.Name == name)) != null);
		element = element != null ? element : default (TElement);
		return querySuccessful;
	}

	public IEnumerator<TElement> GetEnumerator () {
		return Elements.GetEnumerator ();
	}

	IEnumerator IEnumerable.GetEnumerator () {
		return this.GetEnumerator ();
	}

}