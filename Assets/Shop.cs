using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Shop : MonoBehaviour {

	[SerializeField]
	private Text weaponInformation;
	[SerializeField]
	private Image weaponImage;

	[SerializeField]
	private GameObject weaponShopItemPrefab;
	[SerializeField]
	private Transform weaponShopList;
	[SerializeField]
	private WeaponDatabase weaponDatabase;

	private void Start () {
		foreach (var weapon in weaponDatabase) {
			GameObject shopItem = Instantiate (weaponShopItemPrefab, weaponShopList);
			shopItem.GetComponentInChildren<Text> ().text = 
				weapon.name + '\n' + weapon.price;

			shopItem.GetComponent<Button> ().onClick.AddListener (() => Purchase (weapon));
			
			var pointerEnter = new EventTrigger.Entry ();
			pointerEnter.eventID = EventTriggerType.PointerEnter;
			pointerEnter.callback.AddListener (eventData => DisplayInformation (weapon));

			var pointerExit = new EventTrigger.Entry ();
			pointerExit.eventID = EventTriggerType.PointerExit;
			pointerExit.callback.AddListener (eventData => weaponInformation.text = string.Empty);

			IList<EventTrigger.Entry> triggers = shopItem.GetComponent<EventTrigger> ().triggers;
			triggers.Add (pointerEnter);
			triggers.Add (pointerExit);
		}
	}

	private void DisplayInformation (Weapon weapon) {
		weaponInformation.text = weapon.name + "\nKill Reward: " + weapon.killReward + "\nMobility: " + weapon.mobility;
		Gun gun;
		Knife knife;
		if (gun = weapon as Gun)
			weaponInformation.text += "\nDamage: " + gun.damage + "\nAmmunition: " + gun.ammunitionInMagazine + "\nFire Rate: " + gun.fireRate;
		else if (knife = weapon as Knife)
			weaponInformation.text += "\nDamage: " + knife.swingDamage;
	}

	private void Purchase (Weapon weapon) {
		
	}

}