import { makeT } from "@idosgames/react/ui";

const EN = {
  play: "Bloody Steak",
  scoreSent: "Score saved to the leaderboard",
  newRecord: "New record!",
  scoreFailed: "Could not save the score",
} as const;

export const t = makeT<keyof typeof EN>(EN, {
  play: "Bloody Steak",
  scoreSent: "Очки записаны в лидерборд",
  newRecord: "Новый рекорд!",
  scoreFailed: "Не удалось записать очки",
});
