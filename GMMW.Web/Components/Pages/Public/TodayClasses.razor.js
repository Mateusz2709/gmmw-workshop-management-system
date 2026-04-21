let connection = null;

export async function startClassUpdates(dotNetHelper) {
    if (connection) {
        return;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/class-updates")
        .withAutomaticReconnect()
        .build();

    connection.on("ClassesUpdated", async function () {
        await dotNetHelper.invokeMethodAsync("HandleClassesUpdated");
    });

    await connection.start();
}

export async function stopClassUpdates() {
    if (!connection) {
        return;
    }

    await connection.stop();
    connection = null;
}