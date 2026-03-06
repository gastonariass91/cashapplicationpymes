window.reconciliationHotkeys = (function () {
  let handler = null;
  let dotnet = null;

  function keydown(e) {
    const tag = (document.activeElement?.tagName || "").toLowerCase();
    const isTyping = tag === "input" || tag === "textarea";

    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "togglePalette");
      return;
    }

    if (e.key === "Escape") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "closeContext");
      return;
    }

    if (isTyping) return;

    const k = e.key.toLowerCase();

    if (k === "1") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "viewAuto");
      return;
    }
    if (k === "2") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "viewPending");
      return;
    }
    if (k === "3") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "viewExceptions");
      return;
    }
    if (k === "a") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "acceptCurrent");
      return;
    }
    if (k === "x") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "exceptionCurrent");
      return;
    }
    if (k === "v") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "acceptVisible");
      return;
    }
    if (k === "f") {
      e.preventDefault();
      dotnet?.invokeMethodAsync("HandleHotkey", "focusSearch");
      return;
    }
  }

  return {
    register: function (dotnetRef) {
      this.unregister();
      dotnet = dotnetRef;
      handler = keydown;
      window.addEventListener("keydown", handler);
    },
    unregister: function () {
      if (handler) {
        window.removeEventListener("keydown", handler);
        handler = null;
      }
      dotnet = null;
    },
    focusSelector: function (selector) {
      const el = document.querySelector(selector);
      if (el) el.focus();
    }
  };
})();
