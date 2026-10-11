import { defineModule, defineTopic, shape, type Module } from "@idosgames/module-sdk";
import { makeSkinsScreen } from "./SkinsScreen";
import { t } from "./i18n";

/** The player equipped a skin (id from src/shared/skins.ts). */
export const skinEquipped = defineTopic("skins:equipped@1", shape({ skinId: "string" }));

// The skins shop as a lobby tab: buy skins for BloodyCoin (store "skins"), equip one. The equipped
// skin is durable state in the player's custom data; the event only tells a running game to swap now.
export const skinsModule: Module = defineModule({
  id: "skins",
  meta: { name: "Skins", type: "app", engine: "dom" },
  setup(ctx) {
    ctx.features.register({
      id: "skins",
      label: t("title"),
      icon: "shop",
      group: "economy",
      order: 20,
      primary: true,
      Screen: makeSkinsScreen((skinId) => ctx.events.emit(skinEquipped, { skinId })),
    });
    ctx.exposeToAgent({
      state: () => ({ ui: "dom", feature: "skins" }),
      describeActions: {},
    });
  },
});
