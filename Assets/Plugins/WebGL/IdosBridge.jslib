// JS-половина IdosBridge.cs: передаёт события игры веб-оболочке iDosGames (IdosWeb, модуль bloody-steak).
// Оболочка регистрирует window.BloodySteakHost; без неё (обычная страница билда) вызовы ничего не делают.
mergeInto(LibraryManager.library, {
  IdosBridge_SubmitScore: function (score) {
    var host = window.BloodySteakHost;
    if (host && typeof host.submitScore === "function") host.submitScore(score);
  },
  IdosBridge_ReportVictory: function () {
    var host = window.BloodySteakHost;
    if (host && typeof host.reportVictory === "function") host.reportVictory();
  },
});
