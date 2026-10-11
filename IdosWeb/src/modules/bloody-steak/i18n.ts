import { makeT } from "@idosgames/react/ui";

const EN = {
  play: "Bloody Steak",
  scoreSent: "Score saved to the leaderboard",
  newRecord: "New record!",
  scoreFailed: "Could not save the score",
  victoryReward: "Shift won!",
  victoryLimit: "No victory reward this time: at most one per 5 minutes and 5 per day.",
} as const;

export const t = makeT<keyof typeof EN>(EN, {
  play: "Bloody Steak",
  scoreSent: "Очки записаны в лидерборд",
  newRecord: "Новый рекорд!",
  scoreFailed: "Не удалось записать очки",
  victoryReward: "Смена выиграна!",
  victoryLimit: "Награды за победу сейчас нет: не чаще раза в 5 минут и не больше 5 в сутки.",
});
