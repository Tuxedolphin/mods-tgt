import { writable } from 'svelte/store';
import * as signalR from '@microsoft/signalr';
import { currentUserInformation, token_information } from '$lib/shared/shared.svelte';
import { resolve } from '$app/paths';
import { goto } from '$app/navigation';
import { PUBLIC_DB_LINK } from '$env/static/public';

const createRoomHub = function () {
	const { subscribe, set } = writable<signalR.HubConnection | null>(null);

	let connection: signalR.HubConnection | null = null;

	const connect = async function (token: string) {
		connection = new signalR.HubConnectionBuilder()
			.withUrl(`${PUBLIC_DB_LINK}hubs/room`, {
				accessTokenFactory: () => token
			})
			.configureLogging(signalR.LogLevel.Error)
			.withAutomaticReconnect()
			.build();

		try {
			await connection.start();
		} catch {
			token_information.reset();
			currentUserInformation.reset();
			const message = 'Login expired, please login in again';
			goto(resolve(`/login#error_description=${message}`));
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
