window.moonlightServers = {
    loadAddons: function () {
        if(window.moonlightServers.consoleAddonsLoaded)
            return;

        window.moonlightServers.consoleAddonsLoaded = true;
        XtermBlazor.registerAddons({"addon-fit": new FitAddon.FitAddon()});
    }
}

window.moonlightServers.consoleAddonsLoaded = false;