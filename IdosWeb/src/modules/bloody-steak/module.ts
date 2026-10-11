import { defineModule, defineTopic, shape, type Module } from "@idosgames/module-sdk";
import { equippedSkin } from "../../shared/skins";
import { createUnityScene } from "./game/unityScene";
import { t } from "./i18n";

/** Copy of the skins module's topic (modules never import each other). */
const skinEquipped = defineTopic("skins:equipped@1", shape({ skinId: "string" }));

/** Leaderboard of the best shift score (title config: Leaderboard.Definitions.best_score, 12-hour cycles; #1 gets 100 BloodyCoin). */
export const LEADERBOARD_ID = "best_score";
/**
 * BloodyCoin for a won shift. claimReward pays a fixed amount, so the game's number (0..60) is paid as a
 * sum of denominations: title config Reward.Claims.shift_coin_{1,2,4,8,16,32}, each with its own server
 * rate limit (once a minute, 5 per day) — so a day can never pay more than 5 × 63.
 */
export const COIN_DENOMINATIONS = [32, 16, 8, 4, 2, 1] as const;
export const MAX_COINS_PER_SHIFT = 60;
/** The SDK rejects the same endpoint fired again within ~600 ms ("throttled"). */
const CLAIM_SPACING_MS = 700;

declare global {
  interface Window {
    /** What the Unity build calls through Assets/Plugins/WebGL/IdosBridge.jslib. */
    BloodySteakHost?: { submitScore(score: number): void; reportVictory(coins: number): void };
  }
}

// Bloody Steak itself: the Unity WebGL build as a full-screen game behind the lobby's "Play". Sign-in
// and the leaderboard screen belong to the base and the leaderboard module; this module only runs
// the game and turns the score the game reports at the end of a shift into a leaderboard submission.
export const bloodySteakModule: Module = defineModule({
  id: "bloody-steak",
  meta: { name: "Bloody Steak", type: "game", genre: "simulation", engine: "dom" },
  setup(ctx) {
    const scene = createUnityScene(() => equippedSkin(ctx.client));
    // The equipped skin lives in the player's custom data; load it once so a skin bought in an
    // earlier session is on the player from the first shift.
    void ctx.client.userCustomData.getMyUserCustomData().then(() => scene.setSkin(equippedSkin(ctx.client)));
    ctx.events.on(skinEquipped, ({ skinId }) => scene.setSkin(skinId));
    let lastScore: number | null = null;
    let best: number | null = null;

    const submitScore = async (score: number) => {
      lastScore = score;
      // The server rejects a score <= 0 anyway; a shift with no points is simply not recorded.
      if (!(score > 0)) return;
      const res = await ctx.client.leaderboard.submitScore(LEADERBOARD_ID, Math.floor(score));
      if (!res.ok) {
        ctx.notifications.toast({ level: "error", message: t("scoreFailed"), source: "leaderboard" });
        return;
      }
      // best_score keeps the maximum (BestScore): NewScore equals this shift only when it is the record.
      const isRecord = res.data.NewScore === Math.floor(score);
      best = res.data.NewScore;
      ctx.notifications.toast({
        level: isRecord ? "reward" : "success",
        title: isRecord ? t("newRecord") : undefined,
        message: `${t("scoreSent")}: ${Math.floor(score)}`,
        icon: "trophy",
        source: "leaderboard",
      });
    };

    // The game reports how many BloodyCoin the shift earned (money / 10, at most 60); it is paid as
    // denominations, one claim each. The server limits every denomination; a refusal is normal.
    let paying = false;
    const reportVictory = async (coins: number) => {
      let left = Math.max(0, Math.min(MAX_COINS_PER_SHIFT, Math.floor(Number(coins) || 0)));
      if (left === 0 || paying) return;
      paying = true;
      let paid = 0;
      let refused = false;
      try {
        for (const value of COIN_DENOMINATIONS) {
          if (value > left) continue;
          if (paid > 0 || refused) await new Promise((r) => setTimeout(r, CLAIM_SPACING_MS));
          const res = await ctx.client.reward.claimReward(`shift_coin_${value}`);
          if (res.ok) paid += value;
          else refused = true;
          left -= value;
        }
      } finally {
        paying = false;
      }
      if (paid === 0) {
        ctx.notifications.toast({ level: "info", message: t("victoryLimit"), key: "victory-reward" });
        return;
      }
      ctx.notifications.toast({
        level: "reward",
        title: t("victoryReward"),
        message: `+${paid} BloodyCoin`,
        icon: "gift",
        key: "victory-reward",
      });
    };

    ctx.effect(() => {
      window.BloodySteakHost = {
        submitScore: (score) => void submitScore(score),
        reportVictory: (coins) => void reportVictory(coins),
      };
      return () => {
        delete window.BloodySteakHost;
      };
    });

    ctx.registerScene(scene);
    ctx.registerRoute({ id: "bloody-steak", label: t("play"), icon: "🥩" });

    ctx.exposeToAgent({
      state: () => ({
        unity: scene.state(),
        lastScore,
        best,
        leaderboard: LEADERBOARD_ID,
        skin: equippedSkin(ctx.client),
      }),
      actions: {
        submitScore: (args) => submitScore(Number(args?.score ?? 0)),
        reportVictory: (args) => reportVictory(Number(args?.coins ?? 0)),
      },
      describeActions: {
        submitScore:
          "submitScore({ score: number }) — submit a shift score exactly as the Unity build does at the end of a shift.",
        reportVictory:
          "reportVictory({ coins: number }) — pay a won shift's BloodyCoin (0..60) as shift_coin_* denominations, exactly as the Unity build does.",
      },
    });
  },
});
