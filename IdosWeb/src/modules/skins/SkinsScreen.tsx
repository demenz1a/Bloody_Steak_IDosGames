import { useCallback, useEffect, useState, type ReactNode } from "react";
import type { StorefrontOfferView, StorefrontView } from "@idosgames/core";
import { useIDosGamesClient, useUserState } from "@idosgames/react";
import {
  Button,
  Card,
  costLines,
  errorText,
  grantedBy,
  outlined,
  pickLanguage,
  ResourceList,
  Spinner,
  useCelebrate,
  useGameLayout,
  useStagger,
  useToast,
  v,
} from "@idosgames/react/ui";
import {
  EQUIPPED_SKIN_KEY,
  SKINS,
  SKINS_STORE_ID,
  equippedSkin,
  ownedSkins,
  skinPreview,
  type SkinInfo,
} from "../../shared/skins";
import { t } from "./i18n";

interface OfferAddress {
  offer: StorefrontOfferView;
  storeID: string;
  sectionID: string;
  slotID: string;
}

/** Offers of the "skins" storefront by OfferID, with the slot each one is bought through. */
function skinOffers(stores: StorefrontView[] | null | undefined): Map<string, OfferAddress> {
  const map = new Map<string, OfferAddress>();
  for (const store of stores ?? []) {
    if (store.StoreID !== SKINS_STORE_ID) continue;
    for (const section of store.Sections ?? [])
      for (const slot of section.Slots ?? [])
        if (slot.Offer)
          map.set(slot.Offer.OfferID, {
            offer: slot.Offer,
            storeID: store.StoreID,
            sectionID: section.SectionID,
            slotID: slot.SlotID,
          });
  }
  return map;
}

// The skins shop: every skin with its wolf and chef look, bought for BloodyCoin through the "skins"
// store and equipped here. The equipped skin is saved in the player's private custom data and sent
// to the game by `onEquipped` (the bloody-steak module listens and forwards it to Unity).
export function makeSkinsScreen(onEquipped: (skinId: string) => void) {
  return function SkinsScreen(): ReactNode {
    const client = useIDosGamesClient();
    useUserState(); // re-render on inventory / custom data changes
    const toast = useToast();
    const celebrate = useCelebrate();
    const stagger = useStagger();
    const layout = useGameLayout();
    const [offers, setOffers] = useState<Map<string, OfferAddress> | null>(null);
    const [busy, setBusy] = useState<string | null>(null);

    const load = useCallback(async () => {
      const res = await client.store.getStorefront();
      if (res.ok) setOffers(skinOffers(res.data.Stores));
      else {
        setOffers(new Map());
        toast(errorText(res.error), "error");
      }
    }, [client, toast]);

    useEffect(() => {
      void load();
    }, [load]);

    const owned = ownedSkins(client);
    const equipped = equippedSkin(client);

    const equip = async (skin: SkinInfo) => {
      setBusy(skin.id);
      const res = await client.userCustomData.setPrivateData(EQUIPPED_SKIN_KEY, skin.id);
      setBusy(null);
      if (!res.ok) return toast(errorText(res.error), "error");
      onEquipped(skin.id);
      toast(t("equippedToast"), "success");
    };

    const buy = async (skin: SkinInfo, address: OfferAddress) => {
      const option = address.offer.PriceOptions?.[0];
      setBusy(skin.id);
      const res = await client.store.purchase(address.offer.OfferID, 1, {
        selectedOptionID: option?.OptionID,
        slot: { storeID: address.storeID, sectionID: address.sectionID, slotID: address.slotID },
      });
      setBusy(null);
      // A crypto shortfall opens the wallet top-up by itself (the base's WalletTopUp).
      if (!res.ok) return toast(errorText(res.error), "error");
      celebrate(grantedBy(res.data));
      toast(t("bought"), "success");
      await load();
      await equip(skin);
    };

    if (!offers) return <Spinner />;

    const lang = pickLanguage();
    return (
      <div style={{ display: "grid", gap: 14 }}>
        <div style={{ ...outlined, fontSize: 13, color: v.textDim, textAlign: "center" }}>{t("hint")}</div>
        <div
          style={{
            display: "grid",
            gap: 14,
            gridTemplateColumns: layout === "phone" ? "1fr" : "repeat(auto-fill, minmax(300px, 1fr))",
          }}
        >
          {SKINS.map((skin, i) => {
            const isOwned = owned.has(skin.id);
            const isEquipped = equipped === skin.id;
            const address = skin.offerID ? offers.get(skin.offerID) : undefined;
            const price = costLines(address?.offer.PriceOptions?.[0]?.Cost);
            return (
              <Card key={skin.id} highlight={isEquipped} style={{ display: "grid", gap: 10, ...stagger(i) }}>
                <div style={{ ...outlined, fontSize: 18, textAlign: "center" }}>{skin.name[lang]}</div>
                <div style={{ display: "flex", justifyContent: "center", gap: 12 }}>
                  {([false, true] as const).map((chef) => (
                    <figure key={String(chef)} style={{ margin: 0, display: "grid", justifyItems: "center", gap: 4 }}>
                      <img src={skinPreview(skin.id, chef)} alt="" style={{ height: 150, objectFit: "contain" }} />
                      <figcaption style={{ ...outlined, fontSize: 11, color: v.textDim }}>
                        {chef ? t("chef") : t("wolf")}
                      </figcaption>
                    </figure>
                  ))}
                </div>
                {isEquipped ? (
                  <Button tone="grey" disabled>
                    {t("equipped")}
                  </Button>
                ) : isOwned ? (
                  <Button tone="green" busy={busy === skin.id} onClick={() => void equip(skin)}>
                    {t("equip")}
                  </Button>
                ) : address ? (
                  <Button tone="gold" busy={busy === skin.id} onClick={() => void buy(skin, address)}>
                    {t("buy")} <ResourceList lines={price} size={18} />
                  </Button>
                ) : (
                  <Button tone="grey" disabled>
                    {t("notForSale")}
                  </Button>
                )}
              </Card>
            );
          })}
        </div>
      </div>
    );
  };
}
