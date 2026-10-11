import { makeT } from "@idosgames/react/ui";

const EN = {
  play: "Bloody Steak",
  scoreSent: "Score saved to the leaderboard",
  newRecord: "New record!",
  scoreFailed: "Could not save the score",
  victoryReward: "Shift won!",
  victoryLimit: "No BloodyCoin this time: shift rewards are limited to one per minute and 5 per day.",
} as const;

export const t = makeT<keyof typeof EN>(EN, {
  play: "Bloody Steak",
  scoreSent: "Очки записаны в лидерборд",
  newRecord: "Новый рекорд!",
  scoreFailed: "Не удалось записать очки",
  victoryReward: "Смена выиграна!",
  victoryLimit: "BloodyCoin сейчас не начислены: награды за смену — не чаще раза в минуту и не больше 5 в сутки.",
});
