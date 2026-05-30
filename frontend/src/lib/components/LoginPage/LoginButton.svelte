<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { currentUserInformation, access_token } from '$lib/shared/shared.svelte';
	import { login_to_db, get_timetables } from '$lib/utils/db_operations';
	import { onMount } from 'svelte';


	interface LoginButtonProps {
		email: string;
		password: string;
	}
	let loading = $state(false);
	let { email, password }: LoginButtonProps = $props();
	let errorMessage = $state('');
	onMount(() => {
		if ($currentUserInformation.displayName) {
			goto(resolve('/planner'));
		}
	});

	async function login() {
		loading = true;
		const result = await login_to_db(email, password);
		if (result.isOk()) {
			// Stores access token in localstorage (FOR NOW) -- Not secure:!
			$access_token.access_token = result.value.accessToken;
			$access_token.is_guest_login = false;

			const tt = await get_timetables($access_token.access_token);

			console.log(tt);
			// goto(resolve('/planner'));
		} else {
			errorMessage = result.error;
		}
		loading = false;
	}
</script>

{#if loading}
	<button class="disabled btn mt-4 btn-neutral" type="submit">Logging in...</button>
{:else}
	<button class="btn mt-4 btn-neutral" type="submit" onclick={() => login()}>Login</button>
{/if}

{#if errorMessage}
	{errorMessage}
{/if}
