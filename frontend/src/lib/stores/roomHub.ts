import * as signalR from "@microsoft/signalr";
import { FailedToNegotiateWithServerError } from "@microsoft/signalr/dist/esm/Errors";
import { get, writable } from "svelte/store";
import { goto } from "$app/navigation";
import { resolve } from "$app/paths";
import { PUBLIC_DB_LINK } from "$env/static/public";
import {
  currentUserInformation,
  currentWorkingTimetable,
  token_information,
} from "$lib/shared/shared.svelte";

const createRoomHub = function () {
  const { subscribe, set } = writable<signalR.HubConnection | null>(null);

  let connection: signalR.HubConnection | null = null;

  const connect = async function (token: string) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${PUBLIC_DB_LINK}hubs/room`, {
        accessTokenFactory: () => token,
      })
      .configureLogging(signalR.LogLevel.Error)
      .withAutomaticReconnect()
      .build();

    try {
      await connection.start();
    } catch (e: unknown) {
      // This is a 401:
      if (e instanceof FailedToNegotiateWithServerError) {
        token_information.reset();
        currentUserInformation.reset();
        const tt_id = get(currentWorkingTimetable).timetable_id;
        console.log(tt_id);
        const message = "Login expired, please login in again";
        goto(
          resolve(
            `/login?error_description=${message}&action=redirect&tt_id=${tt_id}`,
          ),
        );
      }

      throw e;
    }

    set(connection);
  };

  const disconnect = async function () {
    await connection?.stop();
    set(null);
  };

  return { subscribe, connect, disconnect };
};

export const roomHub = createRoomHub();
