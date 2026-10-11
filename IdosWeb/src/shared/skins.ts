import type { IDosGamesClient } from "@idosgames/core";

// Skins of the player, shared by the skins shop (modules/skins) and the game (modules/bloody-steak).
//
// A skin is an unstackable item of the title's "Skins" catalog, sold by a Fixed slot of the "skins"
// store (title config). `id` is what the Unity build understands (PlayerSkin.skinId, sent through
// IdosBridge.OnHostSetSkin). To add a skin: import its sprites in Unity (Bloody Steak/Import Player
// Skin), add the item + store slot to the title, put previews into public/skins/ and add a line here.

export interface SkinInfo {
  id: string;
  name: { en: string; ru: string };
  /** Item that proves ownership; null = everyone has it. */
  itemID: string | null;
  /** Store offer that sells it; null = not for sale. */
  offerID: string | null;
}

export const SKINS: SkinInfo[] = [
  { id: "default", name: { en: "Classic", ru: "Классический" }, itemID: null, offerID: null },
  {
    id: "halloween",
    name: { en: "Halloween", ru: "Хеллоуинский" },
    itemID: "skin_halloween",
    offerID: "skin_halloween",
  },
  { id: "lantern", name: { en: "Lantern", ru: "Фонарь" }, itemID: "skin_lantern", offerID: "skin_lantern" },
  { id: "idos", name: { en: "IDos games", ru: "IDos games" }, itemID: "skin_idos", offerID: "skin_idos" },
];

export const DEFAULT_SKIN = "default";
export const SKINS_STORE_ID = "skins";
/** Private user-custom-data key holding the equipped skin id. */
export const EQUIPPED_SKIN_KEY = "equipped_skin";

export function skinPreview(id: string, chef = false): string {
  return `skins/${id}${chef ? "_chef" : ""}.png`;
}

/** Ids of the skins the player owns (from the cached inventory). */
export function ownedSkins(client: IDosGamesClient): Set<string> {
  const instances = Object.values(client.data.user.state?.InventoryV2?.UnstackableItems ?? {});
  const items = new Set(instances.map((i) => i.ItemID));
  return new Set(SKINS.filter((s) => s.itemID === null || items.has(s.itemID)).map((s) => s.id));
}

/** The equipped skin, falling back to the default one if it is unknown or no longer owned. */
export function equippedSkin(client: IDosGamesClient): string {
  const saved = client.data.user.state?.CustomData?.Private?.[EQUIPPED_SKIN_KEY]?.Value;
  return saved && ownedSkins(client).has(saved) ? saved : DEFAULT_SKIN;
}
