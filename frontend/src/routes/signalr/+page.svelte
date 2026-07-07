<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { roomHub } from '$lib/stores/roomHub';

	// Return type/interface of a bunch of SignalR calls.
	interface RoomInformation {
		roomId: string;
		userIds: string[];
	}

	interface MessageResponse {
		userId: string;
		content: string;
		sentAt: string;
	}

	const SIGNALR_TEST_TOKEN = {
		userA: '',
		userB: ''
	} as const;

	type SignalRTestTokenName = keyof typeof SIGNALR_TEST_TOKEN;

	// Helper to get the appropriate SIGNALR_TEST_TOKEN
	const getSignalRTestToken = function () {
		const tokenName = new URLSearchParams(window.location.search).get('token');

		if (!tokenName || !(tokenName in SIGNALR_TEST_TOKEN))
			throw new Error(
				`Use one of: ${Object.keys(SIGNALR_TEST_TOKEN)
					.map((name) => `/signalr?token=${name}`)
					.join(', ')}`
			);

		const token = SIGNALR_TEST_TOKEN[tokenName as SignalRTestTokenName];

		if (!token) throw new Error(`Paste a token into SIGNALR_TEST_TOKEN.${tokenName} first.`);

		return { token, tokenName };
	};

	// Note that quite a few of these are Guid in the backend.
	let messages = $state<MessageResponse[]>([]);
	let roomId = $state('');
	let messageInput = $state('');
	let activeTokenName = $state('');
	let connectionError = $state('');
	let currentStatus = $state('');

	onMount(async () => {
		try {
			const { token, tokenName } = getSignalRTestToken();
			activeTokenName = tokenName;

			await roomHub.connect(token);

			$roomHub?.on('ReceiveMessage', (msg) => {
				console.log('Message received:', msg);
				messages = [...messages, msg];
			});
		} catch (error) {
			connectionError = error instanceof Error ? error.message : String(error);
			console.error(error);
		}

		currentStatus = 'Mounted';
	});

	onDestroy(async () => {
		await roomHub.disconnect();
	});

	// Some functions that is exposed by the backend signalR, not all are here
	const createRoom = async function () {
		if (!$roomHub) return;
		const information: RoomInformation = await $roomHub.invoke('CreateRoom');
		roomId = information.roomId;

		currentStatus = 'Created Room';
	};

	const joinRoom = async function () {
		if (!roomId) await createRoom();
		await $roomHub?.invoke('JoinRoom', roomId);

		currentStatus = 'Joined Room with id: ' + roomId.toString();
	};

	const sendMessage = async function () {
		if (!messageInput.trim()) return;

		await $roomHub?.invoke('SendMessageToRoom', messageInput);
		currentStatus = `Sent ${messageInput} to group`;
		messageInput = '';
	};

	const leaveRoom = async function () {
		if (!roomId) return;

		await $roomHub?.invoke('LeaveRoom', roomId);

		currentStatus = `Left room with id: ${roomId}`;
		roomId = '';
	};
</script>

{#if connectionError}
	<p>{connectionError}</p>
{:else}
	<p>Connected as: {activeTokenName}</p>
{/if}

<button onclick={createRoom}>Create Room</button>
<input bind:value={roomId} placeholder="Room ID" />

<p>
	Current status: {currentStatus}
</p>

<div>
	{#each messages as msg (msg.userId + msg.sentAt)}
		<p>{msg.userId}: {msg.content}</p>
	{/each}
</div>

<input bind:value={messageInput} placeholder="Type a message" />
<button onclick={sendMessage}>Send</button>
<button onclick={joinRoom}>Join</button>
<button onclick={createRoom}>Create</button>
<button onclick={leaveRoom}>Leave Room</button>
