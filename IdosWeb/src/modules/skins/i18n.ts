import { makeT } from "@idosgames/react/ui";

const EN = {
  title: "Skins",
  hint: "The skin changes the wolf and the chef. Animations stay the same.",
  wolf: "Wolf",
  chef: "Chef",
  buy: "Buy",
  equip: "Equip",
  equipped: "Equipped",
  owned: "Owned",
  notForSale: "Not for sale",
  bought: "Skin bought!",
  equippedToast: "Skin equipped",
} as const;

export const t = makeT<keyof typeof EN>(EN, {
  title: "Скины",
  hint: "Скин меняет волка и повара. Анимации остаются прежними.",
  wolf: "Волк",
  chef: "Повар",
  buy: "Купить",
  equip: "Надеть",
  equipped: "Надет",
  owned: "Куплен",
  notForSale: "Не продаётся",
  bought: "Скин куплен!",
  equippedToast: "Скин надет",
});
