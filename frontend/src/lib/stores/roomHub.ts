import { writable } from 'svelte/store';
import * as signalR from '@microsoft/signalr';

const createRoomHub = function () {
	const { subscribe, set } = writable<signalR.HubConnection | null>(null);

	let connection: signalR.HubConnection | null = null;

	const connect = async function (token: string) {
		connection = new signalR.HubConnectionBuilder()
			.withUrl('http://localhost:5233/hubs/room', {
				accessTokenFactory: () => token
			})
			.withAutomaticReconnect()
			.build();

		await connection.start();
		set(connection);
	};

	const disconnect = async function () {
		await connection?.stop();
		set(null);
	};

	return { subscribe, connect, disconnect };
};

export const roomHub = createRoomHub();
