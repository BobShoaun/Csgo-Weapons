using UnityEngine;

public class WeaponDatabase : Database<Weapon2, WeaponDatabase> {

	public GameObject[] firstPersonPrefabs;
	public GameObject [] droppedPrefabs;
	public Recoil [] recoilPatterns;

	protected override void Awake () {
		base.Awake ();
		Elements = new Weapon2 [] {
			new Gun2 (0, "m4a4", Weapon2.SlotType.Primary, firstPersonPrefabs [0], null,
				droppedPrefabs [0], 10, 30, 90, true, 30, recoilPatterns[0]),
			new Knife2 (1, "Knife", Weapon2.SlotType.Knife, firstPersonPrefabs [1], null,
				droppedPrefabs [1], 0.5f, 1, 25, 50),
			new Gun2 (2, "Usp-S", Weapon2.SlotType.Secondary, firstPersonPrefabs [2], null,
				droppedPrefabs [2], 10, 30, 90, true, 30,recoilPatterns[1]),
			new Gun2 (3, "Nova", Weapon2.SlotType.Primary, firstPersonPrefabs [3], null,
				droppedPrefabs [3], 10, 30, 90, true, 30, recoilPatterns[2]),
			new Gun2 (4, "AWP", Weapon2.SlotType.Primary, firstPersonPrefabs [4], null,
				droppedPrefabs [4], 10, 30, 90, true, 30, recoilPatterns[3])
		};
	}
}