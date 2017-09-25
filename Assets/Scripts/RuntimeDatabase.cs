using System;
using UnityEngine;
using System.Collections.Generic;

public abstract class RuntimeDatabase<TElement> : Database<TElement> 
	where TElement : IIdentifiable {

	protected override IEnumerable<TElement> Elements {
		get { return elements; }
	}

	[SerializeField]
	private List<TElement> elements;

	public int Add (TElement element) {
		elements.Add (element);
		return elements.IndexOf (element);
	}

}